using LightInsight.Dashboard.Dashboard;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;
namespace LightInsight.Dashboard.Camera.Client
{
    /// <summary>
    /// Interaction logic for CameraOnlineOffline.xaml
    /// </summary>
    public partial class CameraOnlineNOffline : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public event EventHandler DeleteRequested;
        public int MinCol => 2;
        public int MinRow => 2;
        public Thumb ResizeThumb
        {
            get
            {
                if (InternalResizeThumb == null)
                    return null;

                return InternalResizeThumb;
            }
        }
        private readonly CameraServices _cServices;
        public CameraOnlineNOffline()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    InvalidateMeasure();
                    InvalidateArrange();
                }));
            };

            ApplySmartClientTheme(ClientControl.Instance?.Theme);

            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));

            DeleteButton.Visibility = Visibility.Collapsed;
            _cServices = new CameraServices();
            _cServices.StatusUpdated += (online, offline, totalCount) =>
            {
                TxtOnlineCount.Text = online.ToString();
                TxtOfflineCount.Text = offline.ToString();
            };
            _cServices.Start();

            this.Unloaded += (s, e) =>
            {
                _cServices?.Dispose();
            };
        

		}
		

        private void ApplySmartClientLanguage(string name)
        {
            var uri = name == "vi-VN"
                       ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml"
                       : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml";
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            var dict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Relative)
            };

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
        }

        /// <summary>
        /// Cập nhật số lượng camera từ bên ngoài
        /// </summary>
        public void UpdateCounts(int online, int offline)
        {
            TxtOnlineCount.Text = online.ToString();
            TxtOfflineCount.Text = offline.ToString();
        }

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
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
    }
}