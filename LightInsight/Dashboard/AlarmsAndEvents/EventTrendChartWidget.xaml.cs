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

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public class EventTrendData
    {
        public string Date { get; set; }
        public int Alarms { get; set; }
    }

    public partial class EventTrendChartWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        private SeriesCollection _chartSeries = new SeriesCollection();
        private List<string> _xAxisLabels = new List<string>();

        private SolidColorBrush _lineColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFA500");

        public EventTrendChartWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadChartData();

            TrendChart.Series = _chartSeries;
            TrendAxisX.Labels = _xAxisLabels;
        }

        private void LoadChartData()
        {
            // FAKE DATA NHÌN Y HỆT ẢNH BÁC GỬI
            var rawData = new List<EventTrendData>
            {
                new EventTrendData { Date = "Feb 25", Alarms = 45 },
                new EventTrendData { Date = "Feb 26", Alarms = 37 },
                new EventTrendData { Date = "Feb 27", Alarms = 52 },
                new EventTrendData { Date = "Feb 28", Alarms = 40 },
                new EventTrendData { Date = "Mar 1", Alarms = 34 },
                new EventTrendData { Date = "Mar 2", Alarms = 29 },
                new EventTrendData { Date = "Mar 3", Alarms = 44 }
            };

            var values = new ChartValues<int>();

            foreach (var item in rawData)
            {
                values.Add(item.Alarms);
                _xAxisLabels.Add(item.Date);
            }

            // DÙNG LINESERIES ĐỂ VẼ ĐƯỜNG UỐN LƯỢN
            _chartSeries.Add(new LineSeries
            {
                Values = values,
                Stroke = _lineColor,
                StrokeThickness = 3,         // Độ dày của đường dây
                Fill = Brushes.Transparent,  // KHÔNG TÔ MÀU BÊN DƯỚI DÂY
                PointGeometrySize = 12,      // Kích thước cục tròn tròn
                PointForeground = _lineColor // Màu của cục tròn
            });
        }

        // HÀM QUÉT CHUỘT LIÊN TỤC
        private void TrendChart_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var chart = (CartesianChart)sender;
            var mouseCoordinate = e.GetPosition(chart);
            var p = chart.ConvertToChartValues(mouseCoordinate);

            int index = (int)Math.Round(p.X);

            if (index >= 0 && index < _xAxisLabels.Count && index < _chartSeries[0].Values.Count)
            {
                // Bật sọc trắng
                HoverLine.Value = index;
                HoverLine.Visibility = Visibility.Visible;

                // Cập nhật chữ
                TooltipDate.Text = _xAxisLabels[index];
                int yValue = (int)_chartSeries[0].Values[index];
                TooltipValue.Text = $"alarms : {yValue}";

                // THẦN CHÚ LÀ ĐÂY: Lấy tọa độ X, Y của cái "nút" trên biểu đồ đổi ra pixel
                var nodePosition = chart.ConvertToPixels(new Point(index, yValue));

                // Neo bảng đen vào đúng tọa độ của nút đó
                HoverPopup.PlacementTarget = chart;
                HoverPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;

                // Cộng thêm 15px để cái bảng nằm xêch xuống góc dưới bên phải cái nút cho đẹp
                HoverPopup.HorizontalOffset = nodePosition.X + 15;
                HoverPopup.VerticalOffset = nodePosition.Y + 15;

                HoverPopup.IsOpen = true;
            }
        }

        // HÀM KHI RÚT CHUỘT RA KHỎI BIỂU ĐỒ
        private void TrendChart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HoverLine.Visibility = Visibility.Hidden;
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
