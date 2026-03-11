using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LightInsight.Dashboard.Data.AlarmsAndEvents;
using LightInsight.Dashboard.Data;
using LiveCharts;
using LiveCharts.Wpf;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public partial class AlarmBySeverityWidget : UserControl
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
            var rawData = AlarmDataProvider.GetData(WigetType.AlarmsBySeverityWidget) as List<SeverityData>;

            if (rawData != null)
            {
                foreach (var item in rawData)
                {
                    var sliceColor = (SolidColorBrush)new BrushConverter().ConvertFrom(item.ColorHex);

                    ChartSeries.Add(new PieSeries
                    {
                        Title = item.Title,
                        Values = new ChartValues<int> { item.Count },

                        DataLabels = true,
                        LabelPosition = PieLabelPosition.OutsideSlice,
                        LabelPoint = chartPoint => $"\n\n {item.Title} {chartPoint.Participation:P0} \n\n",

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
        }

        // HÀM XỬ LÝ KHI RÊ CHUỘT VÀO VẠCH
        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            // Lấy Title và Value ra (Ví dụ: "Minor : 45")
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