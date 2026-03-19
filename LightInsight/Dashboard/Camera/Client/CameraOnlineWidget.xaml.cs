using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using LightInsight.Dashboard.Dashboard;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;

namespace LightInsight.Dashboard.Camera.Client
{
    public partial class CameraOnlineWidget : System.Windows.Controls.UserControl, IResizableWidget 
	//, IDisposable
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public event EventHandler DeleteRequested;
        public int MinCol => 2;
        public int MinRow => 2;
        public Thumb ResizeThumb => this.InternalResizeThumb;
		private CameraServices _cServices;
        public CameraOnlineWidget()
        {
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;
			//this.Loaded += (s, e) => {
			//	GetCameraStatus();
			//};
			_cServices = new CameraServices();
			_cServices.StatusUpdated += (online, offline) => {
					TxtOnlineCount.Text = online.ToString();
			};
            _cServices.Start();
		}

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            this.Cursor = isEdit ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.Arrow;
        }
        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
		}

        private object OnThemeChanged(Message message, FQID dest, FQID sender)
        {
            var theme = message?.Data as Theme;
            ApplySmartClientTheme(theme);
            return null;
        }

        private void ApplySmartClientTheme(Theme scTheme)
        {
            Dispatcher.Invoke(() =>
            {
                var themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Dark.xaml";
                var crTheme = ClientControl.Instance.Theme.ThemeType;
                if (crTheme == ThemeType.Light)
                    themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";

                var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };

                if (_currentThemeDictionary != null)
                    Resources.MergedDictionaries.Remove(_currentThemeDictionary);

                Resources.MergedDictionaries.Insert(0, newDict);
                _currentThemeDictionary = newDict;
            });
        }

        // ==============================================================================
  //      private MessageCommunication _messageCommunication;
  //      private object _registration;

  //      public void GetCameraStatus()
  //      {
  //          // 1. Khởi tạo giao tiếp tin nhắn với Server hiện tại
  //          MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
  //          _messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);
  //          // 2. Đăng ký nhận phản hồi trạng thái
  //          _registration = _messageCommunication.RegisterCommunicationFilter(CurrentStateResponseHandler,
  //              new CommunicationIdFilter(MessageCommunication.ProvideCurrentStateResponse));
  //          // 3. Gửi yêu cầu lấy trạng thái của TẤT CẢ các item
  //          _messageCommunication.TransmitMessage(
  //              new VideoOS.Platform.Messaging.Message(MessageCommunication.ProvideCurrentStateRequest),
  //              null, null, null);
  //      }

  //      private object CurrentStateResponseHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
  //      {
		//	// Nhận danh sách trạng thái từ Server
		//	if (message.Data is Collection<ItemState> states)
		//	{
		//		int onlineCount = 0;
		//		int offlineCount = 0;

		//		foreach (ItemState itemState in states)
		//		{
		//			// Chỉ lọc những item là Camera
		//			if (itemState.FQID.Kind == Kind.Camera)
		//			{
		//				// Trạng thái thường là "Responding", "Not Responding", "Disabled"
		//				if (itemState.State == "Responding")
		//				{
		//					onlineCount++;
		//				}
		//				else if (itemState.State == "Not Responding")
		//				{
		//					offlineCount++;
		//				}
		//			}
		//		}

		//		// Ở đây bạn có thể cập nhật lên UI của Smart Client
		//		//System.Diagnostics.Debug.WriteLine($"Online: {onlineCount}, Offline: {offlineCount}");
		//		Dispatcher.BeginInvoke(new Action(() =>
		//		{
		//			// Cập nhật con số Online
		//			TxtOnlineCount.Text = onlineCount.ToString();
		//		}));
		//	}
		//	return null;
  //      }

		//public void Dispose()
		//{
  //          if (_messageCommunication != null && _registration != null)
  //              _messageCommunication.UnRegisterCommunicationFilter(_registration);
		//	    _registration = null;
		//	    _messageCommunication = null;
		//}
	}
}