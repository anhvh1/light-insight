using LightInsight.Dashboard.Camera.Client;
using LightInsight.Dashboard.RecordingServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Path = System.IO.Path;

namespace LightInsight.Dashboard.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        bool editMode = false;
        private Point startPoint;
        Button currentMenu = null;
        string currentDashboard = "Operations";
        bool sidebarCollapsed = false;
        FrameworkElement selectedWidget = null;
        bool isDraggingWidget = false;
        bool isDark = true;
        string currentFilter = null;

        // khai báo widget mẫu để hiển thị trong thư viện widget library
        List<WidgetItem> allWidgets = new List<WidgetItem>()
        {
            new WidgetItem{ Name="Camera Online Count", Category="KPI", WidgetType = typeof(CameraOnlineWidget)},
            new WidgetItem{ Name="Camera Offline Count", Category="KPI",WidgetType = typeof(CameraOfflineWidget)},
            new WidgetItem{ Name="Camera Total Count", Category="KPI",WidgetType = typeof(TotalCameraCount)},
            new WidgetItem{ Name="Camera Status Donut", Category="Charts",WidgetType = typeof(CameraStatusDonut)},
            new WidgetItem{ Name="Camera Duration top 10", Category="Tables",WidgetType = typeof(CameraOfflineDurationTop10)},

            new WidgetItem{ Name="Servers Online Count", Category="KPI", WidgetType = typeof(ServersOnlineCountWidget)},
            new WidgetItem{ Name="Servers Offline Count", Category="KPI",WidgetType = typeof(ServersOfflineCountWidget)},
            new WidgetItem{ Name="Servers Total", Category="KPI",WidgetType = typeof(TotalServersWidget)},
            new WidgetItem{ Name="Storage Usage by Server", Category="KPI",WidgetType = typeof(StorageUsageWidget)},


        };
        public DashboardView()
        {
            InitializeComponent();
            SelectMenu(OperationsBtn);
            TimeRangeCombo.SelectedIndex = 0;
            LanguageCombo.SelectedIndex = 0;
            ThemeBtn.Content = "🌙";
            WidgetList.ItemsSource = allWidgets;
            LoadLayout();
        }
        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedBtn = sender as ToggleButton;

            // lấy tất cả ToggleButton trong container (StackPanel chứa filter)
            var buttons = FilterPanel.Children.OfType<ToggleButton>();

            // nếu button vừa click đang bật -> tắt các button khác
            if (clickedBtn.IsChecked == true)
            {
                foreach (var btn in buttons)
                {
                    if (btn != clickedBtn)
                        btn.IsChecked = false;
                }
            }

            // tìm button đang được chọn
            var activeBtn = buttons.FirstOrDefault(b => b.IsChecked == true);

            if (activeBtn == null)
            {
                // không có filter nào -> show tất cả
                WidgetList.ItemsSource = allWidgets;
            }
            else
            {
                string filter = activeBtn.Tag.ToString();

                var result = allWidgets
                    .Where(x => x.Category == filter)
                    .ToList();

                WidgetList.ItemsSource = result;
            }
        }
        private void AddWidget_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            WidgetItem widget = btn.Tag as WidgetItem;

            if (widget == null)
                return;

            FrameworkElement newWidget =
                Activator.CreateInstance(widget.WidgetType) as FrameworkElement;

            if (newWidget == null)
                return;

            SetupWidget(newWidget);

            Point pos = GetGridPosition();

            Canvas.SetLeft(newWidget, pos.X);
            Canvas.SetTop(newWidget, pos.Y);

            DashboardGrid.Children.Add(newWidget);
        }
        private Point GetGridPosition()
        {
            int count = DashboardGrid.Children.Count;

            int columns = 3;

            double widgetWidth = 320;
            double widgetHeight = 200;

            double x = (count % columns) * widgetWidth;
            double y = (count / columns) * widgetHeight;

            return new Point(x, y);
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = SearchBox.Text.ToLower();

            var result = allWidgets
                .Where(x => x.Name.ToLower().Contains(keyword))
                .ToList();

            WidgetList.ItemsSource = result;
        }
        void ThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            isDark = !isDark;

            if (!isDark)
            {
                ThemeBtn.Content = "🌙";
                //this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            }
            else
            {
                ThemeBtn.Content = "☀";
                //this.Background = Brushes.White;
            }
        }
        void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Refreshing dashboard...");
        }
        void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem)LanguageCombo.SelectedItem;

            if (item.Content.ToString() == "VI")
            {
                OperationsText.Text = " Vận hành";
                AlarmMonitorText.Text = " Giám sát cảnh báo";
            }
            else
            {
                OperationsText.Text = " Operations";
                AlarmMonitorText.Text = " Alarm Monitor";
            }
        }
        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            SelectMenu(btn);

            currentDashboard = btn.Tag.ToString();

            string parent = DashboardExpander.Header.ToString();
            string child = btn.Tag.ToString().Trim();
            BreadcrumbText.Text = $"{parent} > {child}";
            LoadLayout();
        }
        void SelectMenu(Button btn)
        {
            if (currentMenu != null)
                currentMenu.Background = Brushes.Transparent;

            btn.Background = Brushes.DodgerBlue;

            currentMenu = btn;
        }
        private void Widget_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }


        private void EditLayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            editMode = true;
            // mở widget library
            WidgetLibraryColumn.Width = new GridLength(280);
            WidgetLibrary.Visibility = Visibility.Visible;

            EditLayoutBtn.Visibility = Visibility.Collapsed;
            SaveBtn.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;

            foreach (var widget in DashboardGrid.Children.OfType<IDashboardWidget>())
            {
                widget.SetEditMode(true);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            List<WidgetLayout> layouts = new List<WidgetLayout>();

            foreach (UIElement child in DashboardGrid.Children)
            {
                if (child is FrameworkElement widget)
                {
                    WidgetLayout layout = new WidgetLayout
                    {
                        Dashboard = currentDashboard,
                        Type = widget.GetType().Name,
                        X = Canvas.GetLeft(widget),
                        Y = Canvas.GetTop(widget)
                    };

                    layouts.Add(layout);
                }
            }

            SaveLayout(layouts);
            ExitEditMode();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            // đóng widget library
            WidgetLibraryColumn.Width = new GridLength(0);
            ExitEditMode();
        }

        private void ExitEditMode()
        {
            editMode = false;

            WidgetLibrary.Visibility = Visibility.Collapsed;

            EditLayoutBtn.Visibility = Visibility.Visible;
            SaveBtn.Visibility = Visibility.Collapsed;
            CancelBtn.Visibility = Visibility.Collapsed;

            foreach (var widget in DashboardGrid.Children.OfType<IDashboardWidget>())
            {
                widget.SetEditMode(false);
            }
        }
        private void DashboardGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        
        void Widget_MouseMove(object sender, MouseEventArgs e)
        {
            if (!editMode) return;

            if (!isDraggingWidget || selectedWidget == null)
                return;

            Point currentPoint = e.GetPosition(DashboardGrid);

            double dx = currentPoint.X - startPoint.X;
            double dy = currentPoint.Y - startPoint.Y;

            double left = Canvas.GetLeft(selectedWidget);
            double top = Canvas.GetTop(selectedWidget);

            double newLeft = left + dx;
            double newTop = top + dy;

            // ===== GIỚI HẠN TRONG CANVAS =====

            double maxX = DashboardGrid.ActualWidth - selectedWidget.ActualWidth;
            double maxY = DashboardGrid.ActualHeight - selectedWidget.ActualHeight;

            newLeft = Math.Max(0, Math.Min(newLeft, maxX));
            newTop = Math.Max(0, Math.Min(newTop, maxY));

            Canvas.SetLeft(selectedWidget, newLeft);
            Canvas.SetTop(selectedWidget, newTop);

            startPoint = currentPoint;
        }
        //private void DashboardGrid_Drop(object sender, DragEventArgs e)
        //{
        //    if (!editMode)
        //        return;

        //    if (!e.Data.GetDataPresent(DataFormats.StringFormat))
        //        return;

        //    string widgetName = e.Data.GetData(DataFormats.StringFormat) as string;

        //    if (widgetName == "Camera Online Count")
        //    {
        //        bool exists = DashboardGrid.Children
        //            .OfType<CameraOnlineWidget>()
        //            .Any();

        //        if (exists)
        //        {
        //            MessageBox.Show("Widget này đã tồn tại trên dashboard!");
        //            return;
        //        }

        //        var widget = new CameraOnlineWidget();

        //        // cho phép drag widget sau khi thả
        //        widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown;
        //        widget.MouseMove += Widget_MouseMove;
        //        widget.MouseLeftButtonUp += Widget_MouseLeftButtonUp;

        //        // delete widget
        //        widget.DeleteRequested += Widget_DeleteRequested;
        //        widget.SetEditMode(true);

        //        Point position = e.GetPosition(DashboardGrid);

        //        Canvas.SetLeft(widget, position.X);
        //        Canvas.SetTop(widget, position.Y);

        //        Panel.SetZIndex(widget, DashboardGrid.Children.Count);

        //        DashboardGrid.Children.Add(widget);
        //    }
        //}
        private void DashboardGrid_Drop(object sender, DragEventArgs e)
        {
            if (!editMode)
                return;

            if (!e.Data.GetDataPresent(typeof(WidgetItem)))
                return;

            WidgetItem widgetItem = e.Data.GetData(typeof(WidgetItem)) as WidgetItem;

            if (widgetItem == null)
                return;
            // ===== KIỂM TRA WIDGET ĐÃ TỒN TẠI =====
            bool exists = DashboardGrid.Children
                .OfType<FrameworkElement>()
                .Any(x => x.GetType() == widgetItem.WidgetType);

            if (exists)
            {
                MessageBox.Show("Widget này đã tồn tại trên dashboard!");
                return;
            }


            FrameworkElement widget =
                Activator.CreateInstance(widgetItem.WidgetType) as FrameworkElement;

            if (widget == null)
                return;

            // giữ nguyên logic drag
            widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown;
            widget.MouseMove += Widget_MouseMove;
            widget.MouseLeftButtonUp += Widget_MouseLeftButtonUp;

            // delete
            if (widget is IDashboardWidget dashboardWidget)
            {
                dashboardWidget.DeleteRequested += Widget_DeleteRequested;
                dashboardWidget.SetEditMode(true);
            }

            Point position = e.GetPosition(DashboardGrid);

            Canvas.SetLeft(widget, position.X);
            Canvas.SetTop(widget, position.Y);

            Panel.SetZIndex(widget, DashboardGrid.Children.Count);

            DashboardGrid.Children.Add(widget);
        }
        private void WidgetLibrary_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Border border = sender as Border;

            if (border == null)
                return;

            WidgetItem widget = border.DataContext as WidgetItem;

            if (widget == null)
                return;

            DragDrop.DoDragDrop(border, widget, DragDropEffects.Copy);
        }
        void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!editMode) return;

            selectedWidget = sender as FrameworkElement;

            startPoint = e.GetPosition(DashboardGrid);

            isDraggingWidget = true;

            selectedWidget.CaptureMouse();
        }



        void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedWidget != null)
            {
                selectedWidget.ReleaseMouseCapture();
            }

            isDraggingWidget = false;
            selectedWidget = null;
        }
        void Widget_DeleteRequested(object sender, EventArgs e)
        {
            if (sender is FrameworkElement widget)
            {
                DashboardGrid.Children.Remove(widget);
            }
        }
        /// <summary>
        /// Lưu layout của dashboard vào file JSON để có thể load lại sau này
        /// </summary>
        /// <param name="newLayouts"></param>
        void SaveLayout(List<WidgetLayout> newLayouts)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "LightInsight");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, "dashboard_layout.json");

            List<WidgetLayout> allLayouts = new List<WidgetLayout>();

            if (File.Exists(filePath))
            {
                string oldJson = File.ReadAllText(filePath);

                if (!string.IsNullOrWhiteSpace(oldJson))
                {
                    try
                    {
                        allLayouts = JsonSerializer.Deserialize<List<WidgetLayout>>(oldJson);
                    }
                    catch
                    {
                        allLayouts = new List<WidgetLayout>();
                    }
                }
            }

            allLayouts.RemoveAll(x => x.Dashboard == currentDashboard);

            allLayouts.AddRange(newLayouts);

            string json = JsonSerializer.Serialize(allLayouts, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
        /// <summary>
        /// Load layout từ file JSON và hiển thị trên dashboard
        /// </summary>
        void LoadLayout()
        {
            DashboardGrid.Children.Clear();

            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "LightInsight");

            string filePath = Path.Combine(folder, "dashboard_layout.json");

            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return;

            List<WidgetLayout> layouts;

            try
            {
                layouts = JsonSerializer.Deserialize<List<WidgetLayout>>(json);
            }
            catch
            {
                return;
            }

            if (layouts == null)
                return;

            foreach (var layout in layouts)
            {
                if (layout.Dashboard != currentDashboard)
                    continue;

                FrameworkElement widget = CreateWidget(layout.Type);

                if (widget == null)
                    continue;

                SetupWidget(widget);

                Canvas.SetLeft(widget, layout.X);
                Canvas.SetTop(widget, layout.Y);

                Panel.SetZIndex(widget, DashboardGrid.Children.Count);

                DashboardGrid.Children.Add(widget);
            }
        }
        FrameworkElement CreateWidget(string typeName)
        {
            var widgetType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName);

            if (widgetType == null)
                return null;

            return Activator.CreateInstance(widgetType) as FrameworkElement;
        }
        void SetupWidget(FrameworkElement widget)
        {
            widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown;
            widget.MouseMove += Widget_MouseMove;
            widget.MouseLeftButtonUp += Widget_MouseLeftButtonUp;

            if (widget is IDashboardWidget dashboardWidget)
            {
                dashboardWidget.DeleteRequested += Widget_DeleteRequested;
                dashboardWidget.SetEditMode(editMode);
            }
        }
        /// <summary>
        /// Xử lý sự kiện click để thu gọn/hiện sidebar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (sidebarCollapsed)
            {
                SidebarColumn.Width = new GridLength(220);

                OperationsText.Visibility = Visibility.Visible;
                AlarmMonitorText.Visibility = Visibility.Visible;
                DashboardExpander.Header = "Dashboard";
            }
            else
            {
                SidebarColumn.Width = new GridLength(80);

                OperationsText.Visibility = Visibility.Collapsed;
                AlarmMonitorText.Visibility = Visibility.Collapsed;
                DashboardExpander.Header = "";
            }

            sidebarCollapsed = !sidebarCollapsed;
        }
    }
    
}
