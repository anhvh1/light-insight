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

		// Cache trạng thái realtime
		// Cache danh sách camera (CONFIG)
		private readonly ConcurrentDictionary<Guid, string> _cameraStatusCache = new ConcurrentDictionary<Guid, string>();
		private List<Item> _cameraItems = new List<Item>();

		private object _registration1, _registration2;
		private bool _disposed = false;

		public event Action<int, int, int> StatusUpdated;

		#region START / INIT

		public void Start()
		{
			if (EnvironmentManager.Instance.MasterSite == null) return;

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
			return _cameraItems.Select(item =>
			{
				_cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);

				return new CameraInfo
				{
					ID = item.FQID.ObjectId.ToString().Substring(0, 8).ToUpper(),
					Name = item.Name,
					Status = MapStatus(status),
					IP = GetCameraIp(item),
					Recording = "Yes",
					Uptime = "N/A"
				};
			}).ToList();
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

		#endregion

		#region IP

		private string GetCameraIp(Item cameraItem)
		{
			try
			{
				var hardware = Configuration.Instance.GetItem(cameraItem.FQID.ParentId, Kind.Hardware);
                //foreach (var kv in hardware.Properties)
                //{
                //    System.Diagnostics.Debug.WriteLine($"{kv.Key} = {kv.Value}");
                //}
                if (hardware == null) return "N/A";

				if (hardware.Properties != null)
				{
					string[] keys = { "Address", "IpAddress", "Host", "DeviceAddress" };

					foreach (var key in keys)
					{
						if (hardware.Properties.ContainsKey(key))
						{
							var value = hardware.Properties[key];
							if (!string.IsNullOrEmpty(value))
								return value;
						}
					}

					// fallback scan
					foreach (var kv in hardware.Properties)
					{
						var k = kv.Key.ToLower();
						if (k.Contains("ip") || k.Contains("address"))
							return kv.Value;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("GetCameraIp error: " + ex.Message);
			}

			return "N/A";
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

			foreach (var item in _cameraItems)
			{
				_cameraStatusCache.TryGetValue(item.FQID.ObjectId, out string status);

				if (status == "Responding")
					online++;
				else if (status == "Not Responding")
					offline++;
				else
					unknown++;
			}

			int total = _cameraItems.Count;

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
	}
}