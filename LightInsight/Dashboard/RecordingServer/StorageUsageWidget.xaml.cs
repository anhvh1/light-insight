using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts.Wpf;
using LiveCharts;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;

namespace LightInsight.Dashboard.RecordingServer
{
    public class StorageData
    {
        public string ServerName { get; set; }
        public int UsedPercentage { get; set; }
    }

    public partial class StorageUsageWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public event EventHandler DeleteRequested;

        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _xAxisLabels = new List<string>();

        private SolidColorBrush _barColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#2ECC71");

        public int MinCol => 4;

        public int MinRow => 3;

        public Thumb ResizeThumb => this.InternalResizeThumb;

        public StorageUsageWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            UsageChart.Series = _chartSeries;
            UsageAxisX.Labels = _xAxisLabels;
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
                //if (scTheme != null && scTheme.ThemeType == ThemeType.Light)
                if (crTheme == ThemeType.Light)
                    themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";

                var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };

                if (_currentThemeDictionary != null)
                    Resources.MergedDictionaries.Remove(_currentThemeDictionary);

                Resources.MergedDictionaries.Insert(0, newDict);
                _currentThemeDictionary = newDict;

                //_vm?.SetThemeResources(Resources);
                //_vm?.RefreshChartTheme();
            });
        }

        private void LoadChartData()
        {
            var rawData = new List<StorageData>
            {
                new StorageData { ServerName = "RS-01", UsedPercentage = 78 },
                new StorageData { ServerName = "RS-02", UsedPercentage = 45 },
                new StorageData { ServerName = "RS-03", UsedPercentage = 92 },
                new StorageData { ServerName = "RS-04", UsedPercentage = 34 }
            };

            var values = new ChartValues<int>();

            foreach (var item in rawData)
            {
                values.Add(item.UsedPercentage);
                _xAxisLabels.Add(item.ServerName);
            }

            _chartSeries.Add(new ColumnSeries
            {
                Values = values,
                Fill = _barColor,
                MaxColumnWidth = 500,
                ColumnPadding = 5,
                StrokeThickness = 0
            });
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            if (chartPoint == null) return;

            int index = (int)chartPoint.X;

            HoverSection.Value = index - 0.5;
            HoverSection.Visibility = Visibility.Visible;

            if (index >= 0 && index < _xAxisLabels.Count)
            {
                TooltipServer.Text = _xAxisLabels[index];
                TooltipPercent.Text = $"Used % : {chartPoint.Y}";

                // ĐÃ SỬA: Lấy trực tiếp từ UsageChart, không ép kiểu từ sender nữa
                var nodePosition = UsageChart.ConvertToPixels(new Point(chartPoint.X, chartPoint.Y));

                HoverPopup.PlacementTarget = UsageChart;
                HoverPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;

                HoverPopup.HorizontalOffset = nodePosition.X + 20;
                HoverPopup.VerticalOffset = nodePosition.Y + 10;

                HoverPopup.IsOpen = false;
                HoverPopup.IsOpen = true;
            }
        }

        private void Chart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HoverSection.Visibility = Visibility.Hidden;
            HoverPopup.IsOpen = false;
        }

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            var mainBorder = FindName("MainBorder") as Border;
            if (mainBorder != null)
            {
                if (isEdit)
                {
                    mainBorder.ClearValue(Border.BorderBrushProperty);
                    mainBorder.BorderThickness = new Thickness(1);
                }
                else
                {
                    mainBorder.BorderBrush = TryFindResource("CardBorder") as System.Windows.Media.Brush
                        ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
                    mainBorder.BorderThickness = new Thickness(1);
                }
            }
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}