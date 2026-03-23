using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using VideoOS.Platform;
using VideoOS.Platform.Data;
using VideoOS.Platform.Messaging;

namespace LightInsight.Dashboard.RecordingServer
{
    internal class ServerServices : IDisposable
    {
        private MessageCommunication _messageCommunication;

        // Từ điển này đóng vai trò là "Whitelist": Chỉ chứa ID của đúng Recording Server
        private readonly ConcurrentDictionary<Guid, string> _rsCache = new ConcurrentDictionary<Guid, string>();

        private object _registration1, _registration2;
        private bool _disposed = false;

        // Trả về đúng 3 thông số bác cần: (Online, Offline, Total)
        public event Action<int, int, int> StatusUpdated;

        public void Start()
        {
            // 1. CHỐT DANH SÁCH RECORDING SERVER (WHITELIST)
            // Quét hệ thống lấy các item thuộc nhóm Server (Ở cấp cao nhất, đây chính là các Recording Server)
            var servers = Configuration.Instance.GetItemsByKind(Kind.Server, ItemHierarchy.SystemDefined);
            if (servers != null)
            {
                foreach (var server in servers)
                {
                    // Lưu ID vào Whitelist với trạng thái ban đầu là Unknown (Chờ event báo về)
                    _rsCache[server.FQID.ObjectId] = "Unknown";
                }
            }

            // 2. KHỞI TẠO LẮNG NGHE SỰ KIỆN
            MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
            _messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

            _registration1 = _messageCommunication.RegisterCommunicationFilter(CurrentStateResponseHandler,
                new CommunicationIdFilter(MessageCommunication.ProvideCurrentStateResponse));

            _registration2 = _messageCommunication.RegisterCommunicationFilter(RealTimeEventHandler,
                new CommunicationIdFilter(MessageId.Server.NewEventIndication));

            // Yêu cầu Milestone báo cáo trạng thái hiện tại cho toàn bộ hệ thống ngay lập tức
            _messageCommunication.TransmitMessage(
                new VideoOS.Platform.Messaging.Message(MessageCommunication.ProvideCurrentStateRequest),
                null, null, null);

            // Bắn event cập nhật UI lần đầu (Lúc này Total đã chuẩn, On/Off có thể đang chờ data)
            NotifyStatus();
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_messageCommunication != null)
            {
                if (_registration1 != null) _messageCommunication.UnRegisterCommunicationFilter(_registration1);
                if (_registration2 != null) _messageCommunication.UnRegisterCommunicationFilter(_registration2);
            }
            _rsCache.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private object CurrentStateResponseHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
        {
            if (message.Data is Collection<ItemState> result)
            {
                bool isChanged = false;
                foreach (var itemState in result)
                {
                    // LỌC NGHIÊM NGẶT: Chỉ khi ID nằm trong Whitelist (Đúng là Recording Server) thì mới cập nhật
                    if (_rsCache.ContainsKey(itemState.FQID.ObjectId))
                    {
                        _rsCache[itemState.FQID.ObjectId] = itemState.State;
                        isChanged = true;
                    }
                }

                // Tránh gọi UI liên tục nếu event gửi về không liên quan đến Recording Server
                if (isChanged) NotifyStatus();
            }
            return null;
        }

        private object RealTimeEventHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
        {
            var eventData = message.Data as EventData;
            if (eventData != null)
            {
                Guid incomingId = eventData.EventHeader.Source.FQID.ObjectId;

                // LỌC NGHIÊM NGẶT: Có ID trong Whitelist mới cho qua
                if (_rsCache.ContainsKey(incomingId))
                {
                    string msg = eventData.EventHeader.Message;
                    _rsCache[incomingId] = msg;
                    NotifyStatus();
                }
            }
            return null;
        }

        private void NotifyStatus()
        {
            int online = 0;
            int offline = 0;
            int total = _rsCache.Count; // Tổng số đếm chuẩn từ Whitelist

            foreach (var status in _rsCache.Values)
            {
                // Ưu tiên check Offline trước
                if (status.IndexOf("Not Responding", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    status.IndexOf("broken", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    status.IndexOf("Offline", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    offline++;
                }
                // Nếu không Offline thì check Online
                else if (status.IndexOf("Responding", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         status.IndexOf("restored", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    online++;
                }
                // Nếu trạng thái là 'Unknown' hoặc từ lạ, nó sẽ không được cộng vào Online/Offline,
                // nhưng Total thì luôn đúng.
            }

            // Trả số liệu về cho Widget (UI)
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusUpdated?.Invoke(online, offline, total);
            }));
        }
    }
}