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

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    // 1. NHÚNG MODEL
    public class SourceCountData
    {
        public string Source { get; set; }
        public int Count { get; set; }
    }

    public partial class AlarmBySourceWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public event EventHandler DeleteRequested;

        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _yAxisLabels = new List<string>(); // Giờ là trục Y

        private SolidColorBrush _defaultColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#E87E14");
        private SolidColorBrush _hoverColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#F5A623");
        private System.Windows.Shapes.Rectangle _lastHoveredRect = null;

        public int MinCol => 4;

        public int MinRow => 4;

        public Thumb ResizeThumb => this.InternalResizeThumb;

        public AlarmBySourceWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            SourceChart.Series = _chartSeries;
            SourceAxisY.Labels = _yAxisLabels; // Ép nhãn vào trục Y
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
            // Mẹo: LiveCharts vẽ từ dưới lên trên, nên mình đưa Warehouse vào trước để nó chìm xuống đáy
            var rawData = new List<SourceCountData>
            {
                new SourceCountData { Source = "Warehouse", Count = 13 },
                new SourceCountData { Source = "Lobby", Count = 17 },
                new SourceCountData { Source = "Server Room", Count = 28 },
                new SourceCountData { Source = "Parking Lot", Count = 32 },
                new SourceCountData { Source = "Entrance Gate", Count = 45 }
            };

            var values = new ChartValues<int>();

            foreach (var item in rawData)
            {
                values.Add(item.Count);
                _yAxisLabels.Add(item.Source);
            }

            // ĐỔI THÀNH ROWSERIES ĐỂ VẼ THANH NGANG
            _chartSeries.Add(new RowSeries
            {
                Values = values,
                Fill = _defaultColor,

                // ÔNG TÁC GIẢ GÕ SAI CHÍNH TẢ, BÁC PHẢI GÕ THEO ỔNG:
                MaxRowHeigth = 40,

                RowPadding = 4,
                StrokeThickness = 0
            });
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            RestoreLastHoveredPoint();
            if (chartPoint == null) return;

            // Tuyệt chiêu Reflection vẫn hoạt động tốt với RowSeries
            var view = chartPoint.View;
            if (view != null)
            {
                var rectInfo = view.GetType().GetProperty("Rectangle");
                if (rectInfo != null)
                {
                    _lastHoveredRect = rectInfo.GetValue(view) as System.Windows.Shapes.Rectangle;
                    if (_lastHoveredRect != null)
                    {
                        _lastHoveredRect.Fill = _hoverColor;
                    }
                }
            }

            // Mở Popup: Chú ý bây giờ Index nằm ở trục Y, còn Value nằm ở trục X
            int index = (int)chartPoint.Y;
            if (index >= 0 && index < _yAxisLabels.Count)
            {
                TooltipSource.Text = _yAxisLabels[index];
                TooltipCount.Text = $"count : {chartPoint.X}"; // Lấy giá trị từ trục X
                HoverPopup.IsOpen = true;
            }
        }

        private void Chart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RestoreLastHoveredPoint();
            HoverPopup.IsOpen = false;
        }

        private void RestoreLastHoveredPoint()
        {
            if (_lastHoveredRect != null)
            {
                _lastHoveredRect.Fill = _defaultColor;
                _lastHoveredRect = null;
            }
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
    }
}