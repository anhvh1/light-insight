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

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public class DailyCountData
    {
        public string Day { get; set; }
        public int Count { get; set; }
    }

    // Đổi IDashboardWidget thành IResizableWidget để khớp chuẩn hệ thống
    public partial class AlarmDailyCountWidget : UserControl, IResizableWidget
    {
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
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            DailyChart.Series = _chartSeries;
            DailyAxisX.Labels = _xAxisLabels;
        }

        private void LoadChartData()
        {
            var rawData = new List<DailyCountData>
            {
                new DailyCountData { Day = "Mon", Count = 34 },
                new DailyCountData { Day = "Tue", Count = 28 },
                new DailyCountData { Day = "Wed", Count = 45 },
                new DailyCountData { Day = "Thu", Count = 53 },
                new DailyCountData { Day = "Fri", Count = 38 },
                new DailyCountData { Day = "Sat", Count = 15 },
                new DailyCountData { Day = "Sun", Count = 12 }
            };

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

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}