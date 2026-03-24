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
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            var mainBorder = FindName("MainBorder") as System.Windows.Controls.Border;
            if (mainBorder != null)
            {
                if (isEdit)
                {
                    if (mainBorder.Tag is System.Windows.Media.Brush originalBorderBrush)
                        mainBorder.BorderBrush = originalBorderBrush;
                    mainBorder.BorderThickness = new Thickness(1);
                }
                else
                {
                    if (!(mainBorder.Tag is System.Windows.Media.Brush))
                        mainBorder.Tag = mainBorder.BorderBrush;
                    mainBorder.BorderBrush = TryFindResource("CardBorder") as System.Windows.Media.Brush
                        ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                    mainBorder.BorderThickness = new Thickness(1);
                }
            }
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
            });
        }

        // ==============================================================================
  //      private MessageCommunication _messageCommunication;
  //      private object _registration;
		private async void TestApiButton_Click()
		{
			var myApi = new Api();

			// 1. Test lấy Token thô
			myApi.GetMilestoneAccessToken();

			// 2. Test gọi REST API (Asynchronous)
			await myApi.TestRestApiCall();

		}
	}
}