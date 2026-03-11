using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LightInsight.Dashboard.Data.AlarmsAndEvents;
using LightInsight.Dashboard.Data;
using LiveCharts.Wpf;
using LiveCharts;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public partial class AlarmDailyCountWidget : UserControl
    {
        public event EventHandler DeleteRequested;

        // Chuyển sang biến nội bộ cho gọn, không cần Public nữa
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

            // THẦN CHÚ LÀ ĐÂY: Gán thẳng tay data vào biểu đồ, không trượt đi đâu được!
            DailyChart.Series = _chartSeries;
            DailyAxisX.Labels = _xAxisLabels;
        }

        private void LoadChartData()
        {
            var rawData = AlarmDataProvider.GetData(WigetType.AlarmsDailyCountWidget) as List<DailyCountData>;

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
                    ColumnPadding = 10,
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

        public void SetEditMode(bool isEdit)
        {
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}