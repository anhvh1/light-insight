using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LightInsight.Dashboard.Dashboard;
using LiveCharts.Wpf;
using LiveCharts;
using System.Windows.Controls.Primitives;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public class TypeData
    {
        public string Day { get; set; }
        public int Count { get; set; }
    }

    public partial class AlarmByTypeWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public event EventHandler DeleteRequested;
        public int MinCol => 3;
        public int MinRow => 2;

        public Thumb ResizeThumb => this.InternalResizeThumb;
        // Chuyển sang biến nội bộ cho gọn, không cần Public nữa
        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _xAxisLabels = new List<string>();

        private SolidColorBrush _defaultColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#E87E14");
        private SolidColorBrush _hoverColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#F5A623");
        private System.Windows.Shapes.Rectangle _lastHoveredRect = null;

        public AlarmByTypeWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            // THẦN CHÚ LÀ ĐÂY: Gán thẳng tay data vào biểu đồ, không trượt đi đâu được!
            DailyChart.Series = _chartSeries;
            DailyAxisX.Labels = _xAxisLabels;
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
            var rawData = new List<TypeData>
            {
                new TypeData { Day = "Mon", Count = 34 },
                new TypeData { Day = "Tue", Count = 28 },
                new TypeData { Day = "Wed", Count = 45 },
                new TypeData { Day = "Thu", Count = 53 },
                new TypeData { Day = "Fri", Count = 38 },
                new TypeData { Day = "Sat", Count = 15 },
                new TypeData { Day = "Sun", Count = 12 }
            };

            // Kiểm tra xem DataProvider có thực sự trả về data không
            if (rawData != null && rawData.Count > 0)
            {
                var values = new ChartValues<int>();

                foreach (var item in rawData)
                {
                    values.Add(item.Count);
                    _xAxisLabels.Add(item.Day);
                }

                _chartSeries.Add(new ColumnSeries
                {
                    Values = values,
                    Fill = _defaultColor,
                    MaxColumnWidth = 55,
                    ColumnPadding = 2,
                    StrokeThickness = 0
                });
            }
            else
            {
                // Dòng này để phòng hờ bác truyền lộn Enum, nó sẽ tạo Data giả để hiện cột luôn
                _chartSeries.Add(new ColumnSeries { Values = new ChartValues<int> { 10, 20, 30 }, Fill = _defaultColor });
                _xAxisLabels.AddRange(new[] { "Lỗi Data", "Không có", "Dữ liệu" });
            }
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            RestoreLastHoveredPoint();
            if (chartPoint == null) return;

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

            // Mở Popup
            int index = (int)chartPoint.X;
            if (index >= 0 && index < _xAxisLabels.Count)
            {
                TooltipDay.Text = _xAxisLabels[index];
                TooltipCount.Text = $"count : {chartPoint.Y}";
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
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}