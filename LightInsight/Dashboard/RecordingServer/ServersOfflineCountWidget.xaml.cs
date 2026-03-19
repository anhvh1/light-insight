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
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.ConfigurationItems;
using VideoOS.Platform.Messaging;

namespace LightInsight.Dashboard.RecordingServer
{
    public class ServersOfflineData
    {
        public int Count { get; set; }
        public int TrendPercentage { get; set; }
        public bool IsTrendUp { get; set; }
    }

    public partial class ServersOfflineCountWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public int MinCol => 2;

        public int MinRow => 2;

        public Thumb ResizeThumb => this.InternalResizeThumb;

        public event EventHandler DeleteRequested;

        public ServersOfflineCountWidget()
        {
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            LoadData();
        }

        private object OnThemeChanged(Message message, FQID dest, FQID sender)
        {
            var theme = message?.Data as Theme;
            ApplySmartClientTheme(theme);
            return null;
        }

        private void ApplySmartClientTheme(Theme scTheme)
        {
            Dispatcher.Invoke(() =>
            {
                var themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Dark.xaml";
                var crTheme = ClientControl.Instance.Theme.ThemeType;
                //if (scTheme != null && scTheme.ThemeType == ThemeType.Light)
                if (crTheme == ThemeType.Light)
                    themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";

                var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };

                if (_currentThemeDictionary != null)
                    Resources.MergedDictionaries.Remove(_currentThemeDictionary);

                Resources.MergedDictionaries.Insert(0, newDict);
                _currentThemeDictionary = newDict;

                //_vm?.SetThemeResources(Resources);
                //_vm?.RefreshChartTheme();
            });
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
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        // lấy dữ liệu recording server offline từ server và cập nhật UI
        //public int GetTotalRecordingServers()
        //{
        //    int count = 0;

        //    var managementServer = Configuration.Instance.GetItem(
        //        Kind.ManagementServer,
        //        ItemHierarchy.SystemDefined
        //    );

        //    if (managementServer != null)
        //    {
        //        var recordingServers = managementServer.GetChildren();

        //        count = recordingServers
        //            .Where(x => x.FQID.Kind == Kind.RecordingServer)
        //            .Count();
        //    }

        //    return count;
        //}
    }
}