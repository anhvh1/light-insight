using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts.Wpf;
using LiveCharts;
using LightInsight.Dashboard.Dashboard;

namespace LightInsight.Dashboard.RecordingServer
{
    public class StorageData
    {
        public string ServerName { get; set; }
        public int UsedPercentage { get; set; }
    }

    public partial class StorageUsageWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _xAxisLabels = new List<string>();

        private SolidColorBrush _barColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#2ECC71");

        public StorageUsageWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            UsageChart.Series = _chartSeries;
            UsageAxisX.Labels = _xAxisLabels;
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
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}