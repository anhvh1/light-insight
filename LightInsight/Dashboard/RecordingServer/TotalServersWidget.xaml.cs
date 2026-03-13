using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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

namespace LightInsight.Dashboard.RecordingServer
{
    public class TotalServersData
    {
        public int Count { get; set; }
    }

    public partial class TotalServersWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        public TotalServersWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadData();
        }

        private void LoadData()
        {
            // FAKE DATA
            var data = new TotalServersData
            {
                Count = 13
            };

            // Đẩy thẳng số lên UI
            CountText.Text = data.Count.ToString();
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