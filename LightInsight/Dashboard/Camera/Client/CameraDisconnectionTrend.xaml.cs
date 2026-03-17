using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Controls.Primitives;
using LiveCharts; 
using LiveCharts.Wpf;
namespace LightInsight.Dashboard.Camera.Client
{
    public partial class CameraDisconnectionTrend : UserControl, IResizableWidget
    {
        public event EventHandler DeleteRequested;
		public int MinCol => 4;
		public int MinRow => 3;
		public Thumb ResizeThumb => this.InternalResizeThumb;
		public CameraDisconnectionTrend()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            // Đổ dữ liệu mẫu ngay khi khởi tạo
            LoadChartData();
        }

        private void LoadChartData()
        {
            // Dữ liệu mẫu tương ứng với các mốc: 00:00, 04:00, 08:00...
            var values = new ChartValues<double> { 2, 5, 12, 8, 3, 6, 1 };

            TrendChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Values = values,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")), // Màu đỏ
                    Fill = Brushes.Transparent, // Không đổ màu vùng dưới đường line
                    PointGeometrySize = 8, // Kích thước điểm chấm
                    PointForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    LineSmoothness = 1, // Tạo đường cong spline mượt mà
					DataLabels = true
				}
            };
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