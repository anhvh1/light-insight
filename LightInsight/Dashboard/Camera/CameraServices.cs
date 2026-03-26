using LightInsight.Dashboard.Camera.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Windows;
using VideoOS.Platform;
using VideoOS.Platform.ConfigurationItems;
using VideoOS.Platform.Data;
using VideoOS.Platform.Messaging;
//using LightInsight.DAL;


namespace LightInsight.Dashboard.Camera
{
    internal class CameraServices : IDisposable
    {
        private MessageCommunication _messageCommunication;

        // Cache trạng thái realtime
        // Cache danh sách camera (CONFIG)
        private readonly ConcurrentDictionary<Guid, string> _cameraStatusCache = new ConcurrentDictionary<Guid, string>();
        private List<Item> _cameraItems = new List<Item>();

        private object _registration1, _registration2;
        private bool _disposed = false;
        Dictionary<string, string> _uris = new Dictionary<string, string>();
        public event Action<int, int, int> StatusUpdated;
        #region START / INIT

        public void Start()
        {
            if (EnvironmentManager.Instance.MasterSite == null) return;

            //0.LoadURICameraMap();
            //Database db = new Database();
            _uris = LoadCameraUriMap();

            // 1. Load configuration (chỉ 1 lần)
            LoadCameraConfiguration();

            // 2. Start messaging
            MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
            _messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

            _registration1 = _messageCommunication.RegisterCommunicationFilter(
                CurrentStateResponseHandler,
                new CommunicationIdFilter(MessageCommunication.ProvideCurrentStateResponse));

            _registration2 = _messageCommunication.RegisterCommunicationFilter(
                RealTimeEventHandler,
                new CommunicationIdFilter(MessageId.Server.NewEventIndication));

            // Request initial state
            _messageCommunication.TransmitMessage(
                new Message(MessageCommunication.ProvideCurrentStateRequest),
                null, null, null);
        }

        #endregion

        #region CONFIGURATION

        private void LoadCameraConfiguration()
        {
            _cameraItems.Clear();

            if (Configuration.Instance == null) return;

            try
            {
                var topItems = Configuration.Instance.GetItems(ItemHierarchy.Both);

                foreach (var item in topItems)
                {
                    FindCamerasRecursive(item);
                }

                System.Diagnostics.Debug.WriteLine($"[CameraServices] Loaded {_cameraItems.Count} cameras");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CameraServices] Load config error: {ex.Message}");
            }
        }

        private void FindCamerasRecursive(Item parent)
        {
            if (parent.FQID.Kind == Kind.Camera)
            {
                _cameraItems.Add(parent);
            }

            var children = parent.GetChildren();
            if (children == null) return;

            foreach (var child in children)
            {
                FindCamerasRecursive(child);
            }
        }

        #endregion

        #region BUILD VIEW (IMPORTANT)

        public List<CameraInfo> GetCameraList()
        {
            if (_cameraItems == null || _cameraItems.Count == 0) return new List<CameraInfo>();

            // BƯỚC 1: Lọc trùng dựa trên ObjectId
            var uniqueCameras = _cameraItems
                .GroupBy(c => c.FQID.ObjectId) // Nhóm theo ID duy nhất
                .Select(group => group.First()) // Chỉ lấy 1 đại diện trong mỗi nhóm
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Tổng số item: {_cameraItems.Count} | Sau khi lọc trùng: {uniqueCameras.Count}");

            // BƯỚC 2: Chuyển đổi sang CameraInfo (vòng lặp dễ debug)
            var resultList = new List<CameraInfo>();
            foreach (var item in uniqueCameras)
            {
                try
                {
                    _cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);
                    var key = item.FQID.ObjectId.ToString();

                    _uris.TryGetValue(key, out var uri);
                    var info = new CameraInfo
                    {
                        // Dùng 8 ký tự đầu của ID để hiển thị, nhưng lọc trùng phải dùng cả GUID
                        ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(),
                        Name = item.Name,
                        Status = MapStatus(status),
                        IP = uri,
                        Recording = MapStatus(status) == "Online" ? "Yes" : "No",
                        Uptime = "N/A"
                    };

                    resultList.Add(info);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi xử lý camera {item.Name}: {ex.Message}");
                }
            }

            return resultList.Where(x => x.Status != "Unknown").ToList();
        }

        private string MapStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return "Unknown";

            if (status.Equals("Responding", StringComparison.OrdinalIgnoreCase))
                return "Online";

            if (status.Equals("Not Responding", StringComparison.OrdinalIgnoreCase))
                return "Offline";

            return "Unknown";
        }



        public void CheckRecordingStatus(Guid cameraId)
        {
            // Tạo 1 FQID cho Camera
            FQID cameraFqid = new FQID(EnvironmentManager.Instance.MasterSite.ServerId, Guid.Empty, cameraId, FolderType.No, Kind.Camera);
            // Gửi yêu cầu lấy trạng thái CHI TIẾT
            // Server sẽ trả về một tin nhắn có tên là MessageId.Server.GetItemStatusResponse
            var msg = new VideoOS.Platform.Messaging.Message(MessageId.Server.GetIPAddressResponse);
            _messageCommunication.TransmitMessage(msg, cameraFqid, null, null);
        }
        // Trong Handler nhận tin nhắn:
        private object StatusResponseHandler(Message message, FQID dest, FQID source)
        {
            if (message.MessageId == MessageId.Server.GetIPAddressResponse)
            {
                // Data trả về là một đối tượng ItemStatus
                var status = message.Data;
                if (status != null)
                {
                    // Kiểm tra xem trong danh sách status có chữ "Recording" không
                    string statusString = status.ToString();
                    System.Diagnostics.Debug.WriteLine($"Camera {source.ObjectId} ip {statusString}");
                }
            }
            return null;
        }

        #endregion

        #region MESSAGING (STATE)

        private object CurrentStateResponseHandler(Message message, FQID dest, FQID source)
        {
            if (message.Data is IEnumerable<ItemState> result)
            {
                foreach (var itemState in result.Where(i => i.FQID.Kind == Kind.Camera))
                {
                    _cameraStatusCache[itemState.FQID.ObjectId] = itemState.State;
                }

                NotifyStatus();
            }
            return null;
        }

        private object RealTimeEventHandler(Message message, FQID dest, FQID source)
        {
            var eventData = message.Data as EventData;

            if (eventData?.EventHeader.Source.FQID.Kind == Kind.Camera)
            {
                string msg = eventData.EventHeader.Message;

                string[] validStates =
                {
                    "Responding",
                    "Not Responding",
                    "Disabled",
                    "Enabled",
                    "Communication error"
                };

                if (validStates.Any(s => s.Equals(msg, StringComparison.OrdinalIgnoreCase)))
                {
                    _cameraStatusCache[eventData.EventHeader.Source.FQID.ObjectId] = msg;
                    NotifyStatus();
                }
            }

            return null;
        }

        #endregion

        #region COUNT (FIXED)

        private void NotifyStatus()
        {
            int online = 0;
            int offline = 0;
            int unknown = 0;
            var uniqueCameras = _cameraItems
                .GroupBy(c => c.FQID.ObjectId) // Nhóm theo ID duy nhất
                .Select(group => group.First()) // Chỉ lấy 1 đại diện trong mỗi nhóm
                .ToList();
            foreach (var item in uniqueCameras)
            {
                _cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);

                if (status == "Responding")
                    online++;
                else if (status == "Not Responding")
                    offline++;
                else
                    unknown++;
            }

            int total = online +offline;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusUpdated?.Invoke(online, offline, total);
            }));
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (_disposed) return;

            if (_messageCommunication != null)
            {
                if (_registration1 != null)
                    _messageCommunication.UnRegisterCommunicationFilter(_registration1);

                if (_registration2 != null)
                    _messageCommunication.UnRegisterCommunicationFilter(_registration2);
            }

            _cameraStatusCache.Clear();
            _cameraItems.Clear();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion

        public static Dictionary<string, string> LoadCameraUriMap()
        {
            var dict = new Dictionary<string, string>();

            string query = @"
            SELECT d.IDDevice, h.URI
            FROM Devices d
            JOIN Hardware h ON d.IDHardware = h.IDHardware
            WHERE d.DeviceType = 'Camera'";

            using (SqlConnection conn = new SqlConnection("Data Source= 192.168.100.10 ;Initial Catalog=Surveillance;Persist Security Info=True;User ID=dev;Password=gosol@123;TrustServerCertificate=True;Max Pool Size=400"))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string id = reader["IDDevice"].ToString();
                        string uri = reader["URI"]?.ToString();

                        if (!dict.ContainsKey(id))
                        {
                            dict.Add(id, uri);
                        }
                    }
                }
            }

            return dict;
        }
    }
}