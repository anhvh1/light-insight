using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Controls.Primitives;
using LiveCharts; 
using LiveCharts.Wpf;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;
namespace LightInsight.Dashboard.Camera.Client
{
    public partial class CameraDisconnectionTrend : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public event EventHandler DeleteRequested;
		public int MinCol => 4;
		public int MinRow => 3;
		public Thumb ResizeThumb => this.InternalResizeThumb;
		public CameraDisconnectionTrend()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            // Đổ dữ liệu mẫu ngay khi khởi tạo
            LoadChartData();
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

        private void LoadChartData()
        {
            // Dữ liệu mẫu tương ứng với các mốc: 00:00, 04:00, 08:00...
            var values = new ChartValues<double> { 2, 5, 12, 8, 3, 6, 1 };

            TrendChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Values = values,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")), // Màu đỏ
                    Fill = Brushes.Transparent, // Không đổ màu vùng dưới đường line
                    PointGeometrySize = 8, // Kích thước điểm chấm
                    PointForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    LineSmoothness = 1, // Tạo đường cong spline mượt mà
					DataLabels = true
				}
            };
        }

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            var mainBorder = FindName("MainBorder") as Border;
            if (mainBorder != null)
            {
                mainBorder.BorderThickness = isEdit ? new Thickness(1) : new Thickness(0.8);
            }
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
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
    }
}