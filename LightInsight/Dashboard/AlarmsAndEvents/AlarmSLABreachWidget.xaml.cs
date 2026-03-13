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
using LightInsight.Dashboard.Dashboard;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public class SLABreachData
    {
        public int Count { get; set; }
        public int TrendPercentage { get; set; }
        public bool IsTrendUp { get; set; }
    }
    public partial class AlarmSLABreachWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        public AlarmSLABreachWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadData();
        }

        private void LoadData()
        {
            // FAKE DATA TRỰC TIẾP
            var data = new SLABreachData
            {
                Count = 3,
                TrendPercentage = 25,
                IsTrendUp = true
            };

            // Đẩy data lên giao diện
            CountText.Text = data.Count.ToString();

            // Xử lý text hiển thị cho phần Trend
            string trendDirection = data.IsTrendUp ? "vs last period" : "down vs last period";
            TrendText.Text = $"{data.TrendPercentage}% {trendDirection}";

            // Nếu trend giảm (IsTrendUp = false) thì bác có thể viết thêm vài dòng đổi màu Text/Icon sang đỏ ở đây. 
            // Hiện tại tôi làm đúng như ảnh mẫu là màu xanh nhé.
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