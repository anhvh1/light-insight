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
using MahApps.Metro.IconPacks;
using VideoOS.Platform.Messaging;
using VideoOS.Platform;
using VideoOS.Platform.Client;

namespace LightInsight.Dashboard.Dashboard.Workspace
{
    /// <summary>
    /// Interaction logic for AddWorkspaceWindow.xaml
    /// </summary>
    public partial class AddWorkspaceWindow : Window
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        public string WorkspaceLabel { get;  set; }
        public PackIconMaterialKind WorkspaceIcon { get;  set; }
        public string WorkspaceType { get;  set; }
        List<PackIconMaterialKind> _icons;
        public AddWorkspaceWindow()
        {
            InitializeComponent();
            // nạp theme hiện tại ngay lúc mở
            ApplySmartClientTheme(ClientControl.Instance?.Theme);

            // đăng ký nghe sự kiện đổi theme
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            _icons = LightInsightDefinition.IconList.Take(200).ToList();
            // icon mặc định
            WorkspaceIcon = PackIconMaterialKind.None;
            IconList.ItemsSource = _icons;
        }

        //private void Create_Click(object sender, RoutedEventArgs e)
        //{
        //    WorkspaceLabel = LabelBox.Text;

        //    if (string.IsNullOrWhiteSpace(WorkspaceLabel))
        //    {
        //        MessageBox.Show("Please enter workspace name");
        //        return;
        //    }

        //    // ví dụ lấy từ combobox/icon picker
        //    WorkspaceIcon = IconList.SelectedItem?.ToString();
        //    WorkspaceType = TypeBox.SelectedItem?.ToString();

        //    DialogResult = true;
        //}
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceLabel = LabelBox.Text;

            WorkspaceType = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void SelectIconButton_Click(object sender, RoutedEventArgs e)
        {
            IconPopup.IsOpen = true;
        }
        private void IconList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconList.SelectedItem == null)
                return;

            var icon = (PackIconMaterialKind)IconList.SelectedItem;

            //SelectedIcon.Kind = icon;
            SelectedIconName.Text = icon.ToString();
            WorkspaceIcon = icon;
            SelectedIcon.Kind = icon;
            IconPopup.IsOpen = false;
        }
        
        private void IconSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            var text = IconSearchBox.Text.ToLower();

            IconList.ItemsSource = LightInsightDefinition.IconList
                .Where(x => x.ToString().ToLower().Contains(text))
                .ToList();
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
    }
}
