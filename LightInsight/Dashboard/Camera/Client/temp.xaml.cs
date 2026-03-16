using LightInsight.Dashboard.Dashboard;
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

namespace LightInsight.Dashboard.Camera.Client
{
	/// <summary>
	/// Interaction logic for CameraOnlineWidget.xaml
	/// </summary>
	public partial class Temp : UserControl, IResizableWidget
	{
		public event EventHandler DeleteRequested;
		// Set minimum size of the Widget on the Dashboard Grid
		// default size can be set by the Tag attribute of the UserControl
		public int MinCol => 2;
		public int MinRow => 2;
		public Temp()
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
