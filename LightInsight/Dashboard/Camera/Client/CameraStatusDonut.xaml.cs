using LightInsight.Dashboard.Dashboard;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Input;

namespace LightInsight.Dashboard.Camera.Client
{
	public partial class CameraStatusDonut : UserControl, IDashboardWidget
	{
		public event EventHandler DeleteRequested;

		public CameraStatusDonut()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;
			UpdateStatus(142, 8);
		}

		public void UpdateStatus(int online, int offline)
		{
			var series = new SeriesCollection();

			// Hàm định dạng: Chỉ hiện "Tên: Số" (Ví dụ: Online: 142)
			Func<ChartPoint, string> labelPoint = chartPoint =>
				string.Format("{0}: {1}", chartPoint.SeriesView.Title, chartPoint.Y);

			if (online == 0 && offline == 0)
			{
				series.Add(new PieSeries
				{
					Values = new ChartValues<double> { 1 },
					Fill = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
					StrokeThickness = 0,
					Title = "No Camera"
				});
			}
			else
			{
				if (online > 0)
				{
					series.Add(new PieSeries
					{
						Title = "Online",
						Values = new ChartValues<double> { online },
						Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
						Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
						StrokeThickness = 1,
						DataLabels = true,
						LabelPosition = PieLabelPosition.OutsideSlice, // Đẩy chữ ra ngoài vòng
						LabelPoint = labelPoint,
						Foreground = Brushes.White,
						FontSize = 12,
						FontWeight = FontWeights.SemiBold
					});
				}

				if (offline > 0)
				{
					series.Add(new PieSeries
					{
						Title = "Offline",
						Values = new ChartValues<double> { offline },
						Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
						Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
						StrokeThickness = 1,
						DataLabels = true,
						LabelPosition = PieLabelPosition.OutsideSlice, // Đẩy chữ ra ngoài vòng
						LabelPoint = labelPoint,
						Foreground = Brushes.White,
						FontSize = 12,
						FontWeight = FontWeights.SemiBold
					});
				}
			}

			StatusChart.Series = series;
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