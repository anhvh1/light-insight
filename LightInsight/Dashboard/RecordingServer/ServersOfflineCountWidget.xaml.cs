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
    public class ServersOfflineData
    {
        public int Count { get; set; }
        public int TrendPercentage { get; set; }
        public bool IsTrendUp { get; set; }
    }

    public partial class ServersOfflineCountWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        public ServersOfflineCountWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadData();
        }

        private void LoadData()
        {
            // FAKE DATA NHƯ ẢNH MẪU
            var data = new ServersOfflineData
            {
                Count = 1,
                TrendPercentage = 50,
                IsTrendUp = false // Trend giảm
            };

            // Gán dữ liệu lên UI
            CountText.Text = data.Count.ToString();

            // Text hiển thị
            TrendText.Text = $"{data.TrendPercentage}% vs last period";
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