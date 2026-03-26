using LightInsight.Dashboard.Camera.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VideoOS.Platform;
using VideoOS.Platform.Data;
using VideoOS.Platform.Messaging;

namespace LightInsight.Dashboard.Camera
{
    internal class CameraServices : IDisposable
    {
        private MessageCommunication _messageCommunication;

        private readonly ConcurrentDictionary<Guid, string> _cameraStatusCache = new ConcurrentDictionary<Guid, string>();
        private readonly ConcurrentDictionary<Guid, bool> _recordingCache = new ConcurrentDictionary<Guid, bool>();
        private readonly ConcurrentDictionary<Guid, DateTime> _onlineSinceCache = new ConcurrentDictionary<Guid, DateTime>();

        private List<Item> _cameraItems = new List<Item>();
        private object _registration1, _registration2;
        private bool _disposed = false;

        public event Action<int, int, int> StatusUpdated;

        #region START / INIT

        public void Start()
        {
            if (EnvironmentManager.Instance.MasterSite == null) return;

            LoadCameraConfiguration();

            MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
            _messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

            _registration1 = _messageCommunication.RegisterCommunicationFilter(
                CurrentStateResponseHandler,
                new CommunicationIdFilter(MessageCommunication.ProvideCurrentStateResponse));

            _registration2 = _messageCommunication.RegisterCommunicationFilter(
                RealTimeEventHandler,
                new CommunicationIdFilter(MessageId.Server.NewEventIndication));

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

        #region BUILD VIEW

        public List<CameraInfo> GetCameraList()
        {
            return _cameraItems.Select(item =>
            {
                _cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);

                return new CameraInfo
                {
                    ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(),
                    Name = item.Name,
                    Status = MapStatus(status),
                    IP = GetCameraIp(item),
                    Recording = GetRecordingStatus(item),
                    Uptime = GetUptime(item)
                };
            }).ToList();
        }

        private string MapStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Unknown";
            if (status.Equals("Responding", StringComparison.OrdinalIgnoreCase)) return "Online";
            if (status.Equals("Not Responding", StringComparison.OrdinalIgnoreCase)) return "Offline";
            return "Unknown";
        }

        #endregion

        #region FEATURES: IP, RECORDING, UPTIME (FIXED VERSION)

        private string GetCameraIp(Item cameraItem)
        {
            try
            {
                // Cách lấy IP an toàn nhất qua Hardware Property
                var hardware = Configuration.Instance.GetItem(cameraItem.FQID.ParentId, Kind.Hardware);
                if (hardware != null && hardware.Properties != null)
                {
                    if (hardware.Properties.ContainsKey("Address")) return hardware.Properties["Address"];
                    if (hardware.Properties.ContainsKey("IpAddress")) return hardware.Properties["IpAddress"];
                }
            }
            catch { }
            return "N/A";
        }

        private string GetRecordingStatus(Item cameraItem)
        {
            if (_recordingCache.TryGetValue(cameraItem.FQID.ObjectId, out bool isRecording))
            {
                return isRecording ? "Recording" : "Stopped";
            }
            return "Unknown";
        }

        private string GetUptime(Item cameraItem)
        {
            if (_onlineSinceCache.TryGetValue(cameraItem.FQID.ObjectId, out DateTime onlineTime))
            {
                TimeSpan uptime = DateTime.Now - onlineTime;
                if (uptime.TotalDays >= 1)
                    return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
                return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            }
            return "N/A";
        }

        public void TestLogAllCameraDetails()
        {
            System.Diagnostics.Debug.WriteLine("\n--- [CameraServices TEST] ---");
            foreach (var item in _cameraItems)
            {
                string ip = GetCameraIp(item);
                string rec = GetRecordingStatus(item);
                string uptime = GetUptime(item);
                _cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);

                System.Diagnostics.Debug.WriteLine($"CAM: {item.Name} | IP: {ip} | Status: {status} | Rec: {rec} | Uptime: {uptime}");
            }
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

                    if (itemState.State == "Responding" && !_onlineSinceCache.ContainsKey(itemState.FQID.ObjectId))
                    {
                        _onlineSinceCache[itemState.FQID.ObjectId] = DateTime.Now;
                    }

                    // Một số hệ thống state có thể chứa chữ Recording
                    if (itemState.State.Contains("Recording"))
                    {
                        _recordingCache[itemState.FQID.ObjectId] = true;
                    }
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
                Guid camId = eventData.EventHeader.Source.FQID.ObjectId;
                string msg = eventData.EventHeader.Message;

                if (msg.Equals("Responding", StringComparison.OrdinalIgnoreCase))
                {
                    _cameraStatusCache[camId] = msg;
                    if (!_onlineSinceCache.ContainsKey(camId)) _onlineSinceCache[camId] = DateTime.Now;
                    NotifyStatus();
                }
                else if (msg.Equals("Not Responding", StringComparison.OrdinalIgnoreCase))
                {
                    _cameraStatusCache[camId] = msg;
                    _onlineSinceCache.TryRemove(camId, out _);
                    NotifyStatus();
                }

                // Bắt sự kiện Recording bằng Message String trực tiếp
                if (msg.IndexOf("recording started", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _recordingCache[camId] = true;
                }
                else if (msg.IndexOf("recording stopped", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _recordingCache[camId] = false;
                }
            }
            return null;
        }

        #endregion

        #region HELPERS
        private void NotifyStatus()
        {
            int online = _cameraItems.Count(i => _cameraStatusCache.TryGetValue(i.FQID.ObjectId, out var s) && s == "Responding");
            int total = _cameraItems.Count;
            int offline = total - online;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                StatusUpdated?.Invoke(online, offline, total);
            }));
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_messageCommunication != null)
            {
                if (_registration1 != null) _messageCommunication.UnRegisterCommunicationFilter(_registration1);
                if (_registration2 != null) _messageCommunication.UnRegisterCommunicationFilter(_registration2);
            }
            _cameraStatusCache.Clear();
            _recordingCache.Clear();
            _onlineSinceCache.Clear();
            _cameraItems.Clear();
            _disposed = true;
        }
        #endregion
    }
}
