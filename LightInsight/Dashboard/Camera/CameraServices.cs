using LightInsight.Dashboard.Camera.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
		private readonly ConcurrentDictionary<Guid, bool> _cameraRecordingCache = new ConcurrentDictionary<Guid, bool>();
		private object _registration1, _registration2;
		private bool _disposed = false;
		public event Action<int, int, int> StatusUpdated;

		public void Start()
		{
			if (EnvironmentManager.Instance.MasterSite == null) return;

			MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
			_messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

			_registration1 = _messageCommunication.RegisterCommunicationFilter(CurrentStateResponseHandler,
			new CommunicationIdFilter(MessageId.Server.ProvideCurrentStateResponse));

			_registration2 = _messageCommunication.RegisterCommunicationFilter(RealTimeEventHandler,
			new CommunicationIdFilter(MessageId.Server.NewEventIndication));

			_messageCommunication.TransmitMessage(
			new VideoOS.Platform.Messaging.Message(MessageId.Server.ProvideCurrentStateRequest),
			null, null, null);
		}

		public List<CameraInfo> GetCameraList()
		{
			var cameras = new List<CameraInfo>();
			if (Configuration.Instance == null) return cameras;

			try
			{
				var sites = Configuration.Instance.GetItems(ItemHierarchy.SystemDefined);
				if (sites == null) return cameras;

				foreach (var site in sites)
				{
					var cameraItems = Configuration.Instance.GetItemsByKind(Kind.Camera, site);
					if (cameraItems == null) continue;

					foreach (var item in cameraItems)
					{
						_cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);
						if (string.IsNullOrEmpty(status)) status = "Responding";

						_cameraRecordingCache.TryGetValue(item.FQID.ObjectId, out bool isRecording);

						cameras.Add(new CameraInfo
						{
							ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(),
							Name = item.Name,
							Status = status.Equals("Responding", StringComparison.OrdinalIgnoreCase) ? "Online" : "Offline",
							IP = GetCameraIp(item.FQID),
							Recording = isRecording ? "Yes" : "No",
							Uptime = "99.9%" // SDK không cung cấp trực tiếp, cần DB để tính toán chính xác
						});
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[CameraServices] Error: {ex.Message}");
			}
			return cameras;
		}

		private string GetCameraIp(FQID cameraFqid)
		{
			try
			{
				var result = EnvironmentManager.Instance.SendMessage(
					new VideoOS.Platform.Messaging.Message(MessageId.Server.GetIPAddressRequest),
					cameraFqid,
					null
				);

				if (result is string ip && !string.IsNullOrEmpty(ip))
				{
					ip = ip.Replace("http://", "").Replace("https://", "");
					int colonIndex = ip.IndexOf(':');
					if (colonIndex > 0) ip = ip.Substring(0, colonIndex);
					return ip.TrimEnd('/');
				}
			}
			catch { }
			return "N/A";
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
			_cameraRecordingCache.Clear();
			_disposed = true;
			GC.SuppressFinalize(this);
		}

		private object CurrentStateResponseHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
		{
			if (message.Data is Collection<ItemState> result)
			{
				foreach (var itemState in result.Where(i => i.FQID.Kind == Kind.Camera))
				{
					_cameraStatusCache[itemState.FQID.ObjectId] = itemState.State;
					
					// Kiểm tra xem trong danh sách trạng thái có trạng thái "Recording" hay không
					// Thường Milestone trả về chuỗi các trạng thái phân cách bằng dấu phẩy
					if (!string.IsNullOrEmpty(itemState.State))
					{
						_cameraRecordingCache[itemState.FQID.ObjectId] = 
							itemState.State.IndexOf("Recording", StringComparison.OrdinalIgnoreCase) >= 0;
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
				string msg = eventData.EventHeader.Message;
				Guid cameraGuid = eventData.EventHeader.Source.FQID.ObjectId;

				// 1. Cập nhật trạng thái kết nối
				string[] connectionStatus = { "Responding", "Not Responding", "Disabled", "Enabled", "Communication error" };
				if (connectionStatus.Any(s => msg.Equals(s, StringComparison.OrdinalIgnoreCase)))
				{
					_cameraStatusCache[cameraGuid] = msg;
					NotifyStatus();
				}

				// 2. Cập nhật trạng thái ghi hình (Theo mẫu StatusViewer)
				if (msg.Equals("Recording Started", StringComparison.OrdinalIgnoreCase))
				{
					_cameraRecordingCache[cameraGuid] = true;
				}
				else if (msg.Equals("Recording Stopped", StringComparison.OrdinalIgnoreCase))
				{
					_cameraRecordingCache[cameraGuid] = false;
				}
			}
			return null;
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
			GC.SuppressFinalize(this);
		}

		private object CurrentStateResponseHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
		{
			if (message.Data is Collection<ItemState> result)
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
				string msg = eventData.EventHeader.Message;
				string[] statusMessages = { "Responding", "Not Responding", "Disabled", "Enabled", "Communication error" };

				bool isStatusMessage = statusMessages.Any(s => msg.Equals(s, StringComparison.OrdinalIgnoreCase));

				if (isStatusMessage)
				{
					_cameraStatusCache[eventData.EventHeader.Source.FQID.ObjectId] = msg;
					NotifyStatus();
				}
			}
			return null;
		}

		private void NotifyStatus()
		{
			int online = 0;
			int offline = 0;
			int totalCount = 0;

			foreach (var entry in _cameraStatusCache)
			{
				var item = Configuration.Instance.GetItem(entry.Key, Kind.Camera);
				if (item != null)
				{
					totalCount++;
					if (entry.Value.Equals("Responding", StringComparison.OrdinalIgnoreCase))
						online++;
					else if (entry.Value.Equals("Not Responding", StringComparison.OrdinalIgnoreCase))
						offline++;
				}
			}

			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				StatusUpdated?.Invoke(online, offline, totalCount);
			}));
		}
	}
}
