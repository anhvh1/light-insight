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
				// 1. Lấy toàn bộ Item trong hệ thống (bao gồm cả phân cấp hệ thống và người dùng định nghĩa)
				var allItems = Configuration.Instance.GetItems(ItemHierarchy.Both);

				if (allItems != null)
				{
					// 2. Lọc ra các Item là Camera và dùng GroupBy để đảm bảo mỗi Camera vật lý chỉ xuất hiện 1 lần
					var uniqueCameras = allItems
						.Where(i => i.FQID.Kind == Kind.Camera)
						.GroupBy(i => i.FQID.ObjectId)
						.Select(g => g.First())
						.ToList();

					foreach (var item in uniqueCameras)
					{
						// Lấy trạng thái từ cache
						_cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);
						if (string.IsNullOrEmpty(status)) status = "Responding";

						cameras.Add(new CameraInfo
						{
							ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(),
							Name = item.Name,
							Status = status.Equals("Responding", StringComparison.OrdinalIgnoreCase) ? "Online" : "Offline",
							IP = GetCameraIp(item),
							Recording = "Yes", 
							Uptime = "99.9%"
						});
					}
				}

				System.Diagnostics.Debug.WriteLine($"[CameraServices] Total unique cameras found: {cameras.Count}");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[CameraServices] Error: {ex.Message}");
			}
			return cameras;
		}

		private string GetCameraIp(Item cameraItem)
		{
			try
			{
				// Trong Milestone, Camera con của Hardware. Hardware chứa IP trong thuộc tính Address.
				var hardwareItem = Configuration.Instance.GetItem(cameraItem.FQID.ParentId, Kind.Hardware);
				if (hardwareItem != null)
				{
					string addr = string.Empty;

					if (hardwareItem.Properties.ContainsKey("Address"))
					{
						addr = hardwareItem.Properties["Address"];
					}

					if (string.IsNullOrEmpty(addr))
					{
						addr = hardwareItem.Name; // Đôi khi tên Hardware là IP
					}

					// Làm sạch chuỗi để lấy IPv4/Host (bỏ http, port, ...)
					if (!string.IsNullOrEmpty(addr))
					{
						addr = addr.Replace("http://", "").Replace("https://", "");
						int colonIndex = addr.IndexOf(':');
						if (colonIndex > 0) addr = addr.Substring(0, colonIndex);
						addr = addr.TrimEnd('/');
						return addr;
					}
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
