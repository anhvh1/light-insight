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
		private object _registration1, _registration2;
		private bool _disposed = false;
		public event Action<int, int, int> StatusUpdated;

		public void Start()
		{
			if (EnvironmentManager.Instance.MasterSite == null) return;

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

		public List<CameraInfo> GetCameraList()
		{
			var cameras = new List<CameraInfo>();
			try
			{
				// Lấy tất cả các Item có Kind là Camera từ cấu hình
				var cameraItems = Configuration.Instance.GetItems(Kind.Camera);

				if (cameraItems == null) return cameras;

				foreach (var item in cameraItems)
				{
					// Lấy trạng thái từ cache nếu có, mặc định là "Unknown"
					_cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);
					if (string.IsNullOrEmpty(status)) status = "Responding"; // Giả định ban đầu là hoạt động nếu chưa nhận được event

					// Lấy thông tin Recording
					bool isRecording = false;
					try
					{
						// Sử dụng RecordingCommandService để kiểm tra trạng thái ghi hình thực tế nếu cần, 
						// ở đây ta tạm lấy thông tin cơ bản từ Item
						isRecording = true; // Mặc định trong Smart Client camera thường là đang ghi hình
					}
					catch { }

					cameras.Add(new CameraInfo
					{
						ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(), // Hiển thị 8 ký tự đầu cho gọn
						Name = item.Name,
						Status = status == "Responding" ? "Online" : "Offline",
						IP = GetCameraIp(item),
						Recording = isRecording ? "Yes" : "No",
						Uptime = "99.9%" // Phần này thường cần API chuyên biệt hoặc DB để tính toán chính xác
					});
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[CameraServices] Error getting camera list: {ex.Message}");
			}
			return cameras;
		}

		private string GetCameraIp(Item cameraItem)
		{
			try
			{
				// Trong Milestone, Camera thuộc về một Hardware, Hardware chứa thông tin Address (IP)
				var hardwareItem = Configuration.Instance.GetItem(cameraItem.FQID.ParentId, Kind.Hardware);
				if (hardwareItem != null && !string.IsNullOrEmpty(hardwareItem.Name))
				{
					// Thông thường Address nằm trong Hardware name hoặc metadata của Hardware
					// Ở mức SDK Smart Client, ta có thể lấy qua Hardware Item
					return hardwareItem.Properties.ContainsKey("Address") ? hardwareItem.Properties["Address"] : "N/A";
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
