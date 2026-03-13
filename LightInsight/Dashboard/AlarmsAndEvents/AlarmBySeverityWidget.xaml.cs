using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LightInsight.Dashboard.Dashboard;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    // 1. NHÚNG MODEL VÀO ĐÂY
    public class SeverityData
    {
        public string Title { get; set; }
        public int Count { get; set; }

        public string ColorHex
        {
            get
            {
                switch (Title)
                {
                    case "Minor": return "#2ECC71";
                    case "Warning": return "#F39C12";
                    case "Major": return "#E74C3C";
                    case "Critical": return "#E67E22";
                    default: return "#888888";
                }
            }
        }
    }

    public partial class AlarmBySeverityWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;
        public SeriesCollection ChartSeries { get; set; } = new SeriesCollection();

        public AlarmBySeverityWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;
            this.Loaded += AlarmBySeverityWidget_Loaded;
            this.DataContext = this;
        }

        private void AlarmBySeverityWidget_Loaded(object sender, RoutedEventArgs e)
        {
            if (ChartSeries.Count == 0)
            {
                LoadChartData();
            }
        }

        private void LoadChartData()
        {
            // 2. NHÚNG FAKE DATA VÀO ĐÂY (Thay vì gọi AlarmDataProvider)
            var rawData = new List<SeverityData>
            {
                new SeverityData { Title = "Minor", Count = 45 },
                new SeverityData { Title = "Warning", Count = 66 },
                new SeverityData { Title = "Major", Count = 27 },
                new SeverityData { Title = "Critical", Count = 12 }
            };

            foreach (var item in rawData)
            {
                var sliceColor = (SolidColorBrush)new BrushConverter().ConvertFrom(item.ColorHex);

                ChartSeries.Add(new PieSeries
                {
                    Title = item.Title,
                    Values = new ChartValues<int> { item.Count },

                    DataLabels = true,
                    LabelPosition = PieLabelPosition.OutsideSlice,
                    LabelPoint = chartPoint => $" {item.Title} {chartPoint.Participation:P0} ",

                    Fill = sliceColor,
                    Foreground = sliceColor,
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,

                    PushOut = 2,
                    StrokeThickness = 1,
                    Stroke = Brushes.White
                });
            }
        }

        // HÀM XỬ LÝ KHI RÊ CHUỘT VÀO VẠCH
        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            HoverText.Text = $"{chartPoint.SeriesView.Title} : {chartPoint.Instance}";
            HoverPopup.IsOpen = true; // Mở bảng đen
        }

        // HÀM XỬ LÝ KHI RÚT CHUỘT RA NGOÀI
        private void Chart_MouseLeave(object sender, MouseEventArgs e)
        {
            HoverPopup.IsOpen = false; // Tắt bảng đen
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