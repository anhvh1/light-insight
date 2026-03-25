using System;
using System.Threading;
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
        private bool _widgetEditMode;
        public event EventHandler DeleteRequested;
        public int MinCol => 2;
        public int MinRow => 2;
        public Thumb ResizeThumb => this.InternalResizeThumb;
		private readonly CameraServices _cServices;
        public CameraOnlineWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;
			_cServices = new CameraServices();
			_cServices.StatusUpdated += (online, offline, totalCount) => {
				TxtOnlineCount.Text = online.ToString();
			};
            _cServices.Start();

			TestApiButton_Click();

			this.Unloaded += (s, e) => {
				_cServices?.Dispose();
			};
		}

        private void ApplySmartClientLanguage(string name)
        {
            var uri = name == "vi-VN"
                       ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml"
                       : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml";

            var dict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Relative)
            };

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
        }

        public void SetEditMode(bool isEdit)
        {
            _widgetEditMode = isEdit;
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            this.Cursor = isEdit ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.Arrow;
        }
        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
			_cServices?.Dispose();
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
				DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
			});
		}

        // ==============================================================================
  //      private MessageCommunication _messageCommunication;
  //      private object _registration;
		private async void TestApiButton_Click()
		{
			var myApi = new Api();

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
}