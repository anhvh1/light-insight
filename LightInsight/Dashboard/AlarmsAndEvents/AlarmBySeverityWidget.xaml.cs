using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LightInsight.Dashboard.Dashboard;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
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

    public partial class AlarmBySeverityWidget : UserControl, IResizableWidget
    {
        public int MinCol => 3;
        public int MinRow => 3;
        public Thumb ResizeThumb => this.InternalResizeThumb;

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
            var rawData = new List<SeverityData>
            {
                new SeverityData { Title = "Minor", Count = 45 },
                new SeverityData { Title = "Major", Count = 27 },
                new SeverityData { Title = "Warning", Count = 66 },
                new SeverityData { Title = "Critical", Count = 22 }
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
                    PushOut = 0,
                    StrokeThickness = 1,
                    Stroke = Brushes.White
                });
            }
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            HoverText.Text = $"{chartPoint.SeriesView.Title} : {chartPoint.Instance}";
            HoverPopup.IsOpen = true;
        }

        private void Chart_MouseLeave(object sender, MouseEventArgs e)
        {
            HoverPopup.IsOpen = false;
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