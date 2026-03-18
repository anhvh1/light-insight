using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    public class ServersOnlineData
    {
        public int Count { get; set; }
        public int TrendPercentage { get; set; }
        public bool IsTrendUp { get; set; }
    }

    public partial class ServersOnlineCountWidget : UserControl, IResizableWidget
    {
        public int MinCol => 2;

        public int MinRow => 2;

        public Thumb ResizeThumb => this.InternalResizeThumb;

        public event EventHandler DeleteRequested;

        public ServersOnlineCountWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadData();
        }

        private void LoadData()
        {
            // FAKE DATA
            var data = new ServersOnlineData
            {
                Count = 12,
                TrendPercentage = 0,
                IsTrendUp = true // Màu xanh
            };

            // Đẩy data lên Giao diện
            CountText.Text = data.Count.ToString();

            // Xử lý logic Text Trend
            string trendDirection = data.IsTrendUp ? "vs last period" : "down vs last period";
            TrendText.Text = $"{data.TrendPercentage}% {trendDirection}";
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