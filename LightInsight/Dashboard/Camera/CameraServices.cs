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

		//public List<CameraInfo> GetCameraList()
		//{
		//	var cameras = new List<CameraInfo>();
		//	if (Configuration.Instance == null) return cameras;
		//	try
		//	{
		//		// Sửa lỗi: Thêm ItemHierarchy.Both để xác định phạm vi tìm kiếm
		//		var cameraItems = Configuration.Instance.GetItems(ItemHierarchy.Both);

		//		if (cameraItems == null || cameraItems.Count == 0)
		//		{
		//			System.Diagnostics.Debug.WriteLine("[CameraServices] Configuration.Instance returned no items.");

		//			if (cameraItems == null) return cameras;
		//		}
		//		else
		//		{
		//			System.Diagnostics.Debug.WriteLine("[CameraServices] Configuration.Instance returned some items:.");
		//			System.Diagnostics.Debug.WriteLine(cameraItems);
		//		}
		//		// Lọc các Item có Kind là Camera
		//		foreach (var item in cameraItems.Where(i => i.FQID.Kind == Kind.Camera))
		//		{
		//			// Lấy trạng thái từ cache nếu có, mặc định là "Online" (Responding)
		//			_cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);
		//			if (string.IsNullOrEmpty(status)) status = "Responding"; 

		//			cameras.Add(new CameraInfo
		//			{
		//				ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(), 
		//				Name = item.Name,
		//				Status = status.Equals("Responding", StringComparison.OrdinalIgnoreCase) ? "Online" : "Offline",
		//				IP = GetCameraIp(item),
		//				Recording = "Yes", // Mặc định Yes, Milestone ghi hình hầu hết thời gian
		//				Uptime = "99.9%"
		//			});
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		System.Diagnostics.Debug.WriteLine($"[CameraServices] Error getting camera list: {ex.Message}");
		//	}
		//	return cameras;
		//}


		public List<CameraInfo> GetCameraList()
		{
			var cameras = new List<CameraInfo>();
			if (Configuration.Instance == null) return cameras;

			try
			{
				// 1. Sử dụng HashSet để đảm bảo không trùng lặp camera giữa các Site/Folder
				var processedIds = new HashSet<Guid>();
				
				// 2. Lấy các Site/Server ở mức cao nhất từ SystemDefined hierarchy
				var topLevelItems = Configuration.Instance.GetItems(ItemHierarchy.SystemDefined);

				if (topLevelItems != null)
				{
					foreach (var item in topLevelItems)
					{
						FindCamerasInHierarchy(item, cameras, processedIds);
					}
				}

				System.Diagnostics.Debug.WriteLine($"[CameraServices] Final unique cameras discovered: {cameras.Count}");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[CameraServices] Error in GetCameraList: {ex.Message}");
			}
			return cameras;
		}

		private void FindCamerasInHierarchy(Item parentItem, List<CameraInfo> resultList, HashSet<Guid> processedIds)
		{
			// Nếu là Camera và chưa được xử lý
			if (parentItem.FQID.Kind == Kind.Camera && !processedIds.Contains(parentItem.FQID.ObjectId))
			{
				processedIds.Add(parentItem.FQID.ObjectId);

				// Lấy trạng thái từ cache
				_cameraStatusCache.TryGetValue(parentItem.FQID.ObjectId, out string status);
				if (string.IsNullOrEmpty(status)) status = "Responding";

				resultList.Add(new CameraInfo
				{
					ID = parentItem.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(),
					Name = parentItem.Name,
					Status = status.Equals("Responding", StringComparison.OrdinalIgnoreCase) ? "Online" : "Offline",
					IP = GetCameraIpUsingMessage(parentItem.FQID),
					Recording = "Yes",
					Uptime = "99.9%"
				});
			}

			// Đệ quy tìm con (Milestone tự động tải cấu hình Site con khi gọi GetChildren)
			var children = parentItem.GetChildren();
			if (children != null)
			{
				foreach (var child in children)
				{
					FindCamerasInHierarchy(child, resultList, processedIds);
				}
			}
		}

		private string GetCameraIpUsingMessage(FQID cameraFqid)
		{
			try
			{
				// Cách chuẩn nhất trong MIP SDK để lấy IP của Camera trong Smart Client
				var result = VideoOS.Platform.SDK.Environment.SendMessage(
					new VideoOS.Platform.Messaging.Message(MessageId.Server.GetIPAddressRequest),
					cameraFqid,
					null
				);

				if (result is string ip && !string.IsNullOrEmpty(ip))
				{
					// Làm sạch chuỗi (loại bỏ http, port...)
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
