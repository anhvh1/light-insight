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
    // Đã xóa class TypeData dư thừa vì ta đã khai báo TypeCountData ở AlarmServices

    public partial class AlarmByTypeWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        private bool _widgetEditMode;
        public event EventHandler DeleteRequested;
        public int MinCol => 3;
        public int MinRow => 3;

        public Thumb ResizeThumb => this.InternalResizeThumb;

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
            try
            {
                // 1. Kéo data thật từ hệ thống
                List<TypeCountData> rawData = AlarmServices.GetAlarmCountsByType();

                var values = new ChartValues<int>();

                if (rawData != null && rawData.Count > 0)
                {
                    foreach (var item in rawData)
                    {
                        values.Add(item.Count);

                        string typeName = item.TypeName;

                        // Logic xuống dòng nếu tên Type quá dài (Ví dụ: "Motion Detection Alarm" -> Xuống dòng ở chữ Detection)
                        if (typeName.Length > 15)
                        {
                            int spaceIndex = typeName.IndexOf(' ', typeName.Length / 2);
                            if (spaceIndex < 0) spaceIndex = typeName.LastIndexOf(' ', typeName.Length / 2);

                            if (spaceIndex > 0)
                            {
                                typeName = typeName.Substring(0, spaceIndex) + "\n" + typeName.Substring(spaceIndex + 1);
                            }
                        }

                        _xAxisLabels.Add(typeName);
                    }
                }
                else
                {
                    // Fallback an toàn
                    values.Add(0);
                    _xAxisLabels.Add("No Data");
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
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "LoadChart Type Error: " + ex.Message);
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
                // Bỏ đi ký tự xuống dòng (\n) trong XLabel để lúc hiển thị trên Tooltip được dính liền 1 dòng cho đẹp
                TooltipDay.Text = _xAxisLabels[index].Replace("\n", " ");
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
        }
    }
}