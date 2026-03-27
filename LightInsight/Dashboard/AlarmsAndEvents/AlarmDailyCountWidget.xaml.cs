using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // Bắt buộc để dùng Thumb
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts.Wpf;
using LiveCharts;
using LightInsight.Dashboard.Dashboard;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using System.Threading;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public partial class AlarmDailyCountWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        private bool _widgetEditMode;
        // Khai báo các thuộc tính bắt buộc của IResizableWidget
        public int MinCol => 5;
        public int MinRow => 3;
        public Thumb ResizeThumb => this.InternalResizeThumb;

        public event EventHandler DeleteRequested;

        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _xAxisLabels = new List<string>();

        private SolidColorBrush _defaultColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#E87E14");
        private SolidColorBrush _hoverColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#F5A623");
        private System.Windows.Shapes.Rectangle _lastHoveredRect = null;

        public AlarmDailyCountWidget()
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
                // 1. Gọi trực tiếp Service lấy số liệu thống kê 7 ngày từ Milestone
                // (Đảm bảo bạn đã dán hàm GetWeeklyAlarmCounts vào AlarmServices)
                List<DailyCountData> rawData = AlarmServices.GetWeeklyAlarmCounts();

                var values = new ChartValues<int>();

                if (rawData != null && rawData.Count > 0)
                {
                    foreach (var item in rawData)
                    {
                        values.Add(item.Count);     // Lấy số lượng báo động
                        _xAxisLabels.Add(item.Day); // Lấy tên thứ (Mon, Tue...)
                    }
                }
                else
                {
                    // Fallback an toàn nếu Server trả về null
                    for (int i = 0; i < 7; i++)
                    {
                        values.Add(0);
                        _xAxisLabels.Add("N/A");
                    }
                }

                // 2. Add cột vào Series Collection để vẽ biểu đồ
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
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "LoadChart UI Error: " + ex.Message);
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