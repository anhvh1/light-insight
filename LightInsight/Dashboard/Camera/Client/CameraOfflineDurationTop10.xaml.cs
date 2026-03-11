using LightInsight.Dashboard.Dashboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	/// Interaction logic for CameraOfflineDurationTop10.xaml
	/// </summary>
	public partial class CameraOfflineDurationTop10 : UserControl, IDashboardWidget
    {
        //public event Action<object> DeleteRequested;
        public event EventHandler DeleteRequested;

        public CameraOfflineDurationTop10()
		{
			InitializeComponent();
			DeleteButton.Visibility = Visibility.Collapsed;
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
