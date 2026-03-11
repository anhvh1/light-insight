using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LightInsight.Dashboard.Camera.Client
{
	public partial class CameraStatusDonut : UserControl
	{
		public event EventHandler DeleteRequested;

		public CameraStatusDonut()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;

			// Tự động cập nhật khi UI đã sẵn sàng
			this.Loaded += (s, e) => UpdateChart(0, 1);
		}

		public void SetEditMode(bool isEdit)
		{
			DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
		}

		private void DeleteWidget_Click(object sender, RoutedEventArgs e)
		{
			DeleteRequested?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Cập nhật biểu đồ Donut dựa trên số lượng Online/Offline
		/// </summary>
		public void UpdateChart(int online, int offline)
		{
			TxtOnline.Text = online.ToString();
			TxtOffline.Text = offline.ToString();

			// Đổi màu text Offline nếu bằng 0
			if (offline == 0)
			{
				var grayBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
				LblOffline.Foreground = grayBrush;
				TxtOffline.Foreground = grayBrush;
			}
			else
			{
				var redBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
				LblOffline.Foreground = redBrush;
				TxtOffline.Foreground = redBrush;
			}

			int total = online + offline;

			if (total == 0)
			{
				BackgroundCircle.Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80));
				OnlineSlice.StrokeDashArray = new DoubleCollection(new double[] { 0, 1000 });
				return;
			}

			BackgroundCircle.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));

			// Chu vi tính theo đường kính tâm (70-12=58)
			double circumference = Math.PI * 58;
			double ratio = (double)online / total;
			double dashLength = (ratio * circumference) / OnlineSlice.StrokeThickness;

			if (online == 0)
			{
				OnlineSlice.StrokeDashArray = new DoubleCollection(new double[] { 0, 1000 });
			}
			else if (offline == 0)
			{
				double fullDash = circumference / OnlineSlice.StrokeThickness;
				OnlineSlice.StrokeDashArray = new DoubleCollection(new double[] { fullDash, 1000 });
			}
			else
			{
				OnlineSlice.StrokeDashArray = new DoubleCollection(new double[] { dashLength, 1000 });
			}
		}
	}
}