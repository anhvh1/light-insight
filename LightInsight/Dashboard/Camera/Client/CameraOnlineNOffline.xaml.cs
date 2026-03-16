using LightInsight.Dashboard.Dashboard;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
namespace LightInsight.Dashboard.Camera.Client
{
	/// <summary>
	/// Interaction logic for CameraOnlineOffline.xaml
	/// </summary>
	public partial class CameraOnlineNOffline : UserControl, IResizableWidget
	{
		public event EventHandler DeleteRequested;
		public int MinCol => 2;
		public int MinRow => 2;
		public Thumb ResizeThumb => this.InternalResizeThumb;
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
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

		private void DeleteWidget_Click(object sender, RoutedEventArgs e)
		{
			DeleteRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}