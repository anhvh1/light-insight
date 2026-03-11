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
using LightInsight.Dashboard.Data;
using LightInsight.Dashboard.Data.AlarmsAndEvents;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    /// <summary>
    /// Interaction logic for LiveAlarmsFeed.xaml
    /// </summary>
    public partial class LiveAlarmsFeedWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        public LiveAlarmsFeedWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            this.DataContext = AlarmDataProvider.GetData(WigetType.LiveAlarmsFeedWidget);
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
