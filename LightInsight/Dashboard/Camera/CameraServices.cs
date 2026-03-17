using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IdentityModel.Protocols.WSTrust;
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
		private object _registration1, _registration2;
		private bool _disposed = false;
		// Sử dụng Action để báo về UI
		public event Action<int, int, int> StatusUpdated;

		public void Start()
		{
			MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
			_messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

			_registration1 = _messageCommunication.RegisterCommunicationFilter(CurrentStateResponseHandler,
			new CommunicationIdFilter(MessageCommunication.ProvideCurrentStateResponse));

			_registration2 = _messageCommunication.RegisterCommunicationFilter(RealTimeEventHandler,
			new CommunicationIdFilter(MessageId.Server.NewEventIndication));

			_messageCommunication.TransmitMessage(
			new VideoOS.Platform.Messaging.Message(MessageCommunication.ProvideCurrentStateRequest),
			null, null, null);
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
            _disposed = true;
   
            // Báo cho Garbage Collector biết là đối tượng này đã được dọn dẹp xong
            GC.SuppressFinalize(this);
        }

	private object CurrentStateResponseHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
		{
			if (message.Data is Collection<ItemState> result)
			{
				foreach (var itemState in result.Where(i => i.FQID.Kind == Kind.Camera))
				{
					var item = Configuration.Instance.GetItem(itemState.FQID);
					if (item != null)
					{
						_cameraStatusCache[itemState.FQID.ObjectId] = itemState.State;
					}
				}
				NotifyStatus();
			}
			return null;
		}

		private object RealTimeEventHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
		{
			var eventData = message.Data as EventData;
			if (eventData?.EventHeader.Source.FQID.Kind == Kind.Camera)
			{
				_cameraStatusCache[eventData.EventHeader.Source.FQID.ObjectId] = eventData.EventHeader.Message;
				NotifyStatus();
			}
			return null;
		}

		//private void NotifyStatus()
		//{
		//	 //var allStates = _cameraStatusCache.Values.ToList();
		//	 //System.Diagnostics.Debug.WriteLine($"All States: {string.Join(", ", allStates)}");
		//	 //var summary = allStates.GroupBy(s => s).Select(g => $"{g.Key}: {g.Count()}");
		//	 //System.Diagnostics.Debug.WriteLine($"Summary: {string.Join(" | ", summary)}");
		//		//	foreach (var entry in _cameraStatusCache) {
		//		//		var item = Configuration.Instance.GetItem(entry.Key, Kind.Camera);
		//		//		if (item != null) {
		//		//				System.Diagnostics.Debug.WriteLine($"Name: {item.Name} | ID: {entry.Key} | State: {entry.Value}");
		//		//		}
		//		//		else {
		//		//				System.Diagnostics.Debug.WriteLine($"Unknown Camera | ID: {entry.Key} | State: {entry.Value}");
		//		//		}
		//		//	}

		//	int online = _cameraStatusCache.Values.Count(s => s == "Responding" || s == "Enabled");
		//	int offline = _cameraStatusCache.Values.Count(s => s == "Not Responding" || s == "Disabled");
			
		//	Application.Current.Dispatcher.BeginInvoke(new Action(() =>
		//	{
		//		StatusUpdated?.Invoke(online, offline, 0);
		//	}));
		//}
private void NotifyStatus()
 {
     int online = 0;
     int offline = 0;
     int totalCount = 0;

     // Duyệt qua tất cả các ID đang có trong bộ nhớ đệm
     foreach (var entry in _cameraStatusCache)
     {
         // Kiểm tra xem ID này có thực sự là một Camera đang tồn tại trong cấu hình không     
         var item = Configuration.Instance.GetItem(entry.Key, Kind.Camera);

         if (item != null)
         {
             // Đây là camera thực tế (Không phải camera ma)
             totalCount++;

             // Kiểm tra trạng thái
             if (entry.Value.Equals("Responding", StringComparison.OrdinalIgnoreCase))
                 online++;
             else if (entry.Value.Equals("Not Responding", StringComparison.OrdinalIgnoreCase))
                 offline++;
         }
     }

     // Gửi dữ liệu về UI
     Application.Current.Dispatcher.BeginInvoke(new Action(() =>
     {
         // Bây giờ số lượng sẽ là: Online: 9, Offline: 3, Total: 12
         StatusUpdated?.Invoke(online, offline, totalCount);
     }));
 }
	}
}
