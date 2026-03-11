using System;
using System.Windows;
using System.Windows.Controls;

namespace LightInsight.Dashboard.Camera.Client
{
	/// <summary>
	/// Interaction logic for CameraOnlineOffline.xaml
	/// </summary>
	public partial class CameraOnlineNOffline : UserControl
	{
		public event EventHandler DeleteRequested;

		public CameraOnlineNOffline()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Cập nhật số lượng camera từ bên ngoài
		/// </summary>
		public void UpdateCounts(int online, int offline)
		{
			TxtOnlineCount.Text = online.ToString();
			TxtOfflineCount.Text = offline.ToString();
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