using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
		public event Action<int, int> StatusUpdated;

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
			var result = message.Data as Collection<ItemState>;
			if (result != null)
			{
				foreach (var itemState in result.Where(i => i.FQID.Kind == Kind.Camera))
				{
					_cameraStatusCache[itemState.FQID.ObjectId] = itemState.State;
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

		private void NotifyStatus()
		{
			int online = _cameraStatusCache.Values.Count(s => s == "Responding" || s == "Enabled");
			int offline = _cameraStatusCache.Values.Count(s => s == "Not Responding" || s == "Disabled");

			// Đảm bảo bắn sự kiện về UI Thread của WPF
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				StatusUpdated?.Invoke(online, offline);
			}));
		}
	}
}
