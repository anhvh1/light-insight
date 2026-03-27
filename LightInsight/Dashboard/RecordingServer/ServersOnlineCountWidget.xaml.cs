using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using LightInsight.Dashboard.Dashboard;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;

namespace LightInsight.Dashboard.RecordingServer
{
    public partial class ServersOnlineCountWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        private bool _widgetEditMode;
        public int MinCol => 2;
        public int MinRow => 2;
        public Thumb ResizeThumb => this.InternalResizeThumb;
        public event EventHandler DeleteRequested;

        // Khai báo Service
        private readonly ServerServices _sServices;

        public ServersOnlineCountWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            // Lấy dữ liệu Online
            _sServices = new ServerServices();
            _sServices.StatusUpdated += (online, offline, totalCount) =>
            {
                CountText.Text = online.ToString(); // Chỉ lấy số Online
            };
            _sServices.Start();

            // Dọn dẹp
            this.Unloaded += (s, e) => {
                _sServices?.Dispose();
            };
        }

        private void ApplySmartClientLanguage(string name)
        {
            var uri = name == "vi-VN" ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml" : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml";
            var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
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
                if (crTheme == ThemeType.Light) themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";

                var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };
                if (_currentThemeDictionary != null) Resources.MergedDictionaries.Remove(_currentThemeDictionary);
                Resources.MergedDictionaries.Insert(0, newDict);
                _currentThemeDictionary = newDict;
                DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            });
        }

        public void SetEditMode(bool isEdit)
        {
            _widgetEditMode = isEdit;
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
            _sServices?.Dispose();
        }
    }
}