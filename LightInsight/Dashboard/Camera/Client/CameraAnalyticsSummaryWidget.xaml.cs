using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LightInsight.Dashboard.Dashboard;
using System.Windows.Controls.Primitives;
namespace LightInsight.Dashboard.Camera.Client
{
	/// <summary>
	/// Interaction logic for CameraAnalyticsSummaryWidget.xaml
	/// </summary>
	public partial class CameraAnalyticsSummaryWidget : UserControl, IResizableWidget
	{
		public event EventHandler DeleteRequested;
		public int MinCol => 3;
		public int MinRow => 3;
		public Thumb ResizeThumb => this.InternalResizeThumb;

		public CameraAnalyticsSummaryWidget()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;
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
