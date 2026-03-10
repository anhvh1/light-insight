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
    public partial class CameraOnlineWidget : UserControl
    {
        public event EventHandler DeleteRequested;

        public CameraOnlineWidget()
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
