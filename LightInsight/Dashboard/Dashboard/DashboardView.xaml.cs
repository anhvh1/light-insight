using LightInsight.Dashboard.Camera.Client;
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
        };
        public DashboardView()
        {
            InitializeComponent();
            OpenDashboard(OperationsBtn);
            TimeRangeCombo.SelectedIndex = 0;
            LanguageCombo.SelectedIndex = 0;
            ThemeBtn.Content = "🌙";
            WidgetList.ItemsSource = allWidgets;
            LoadLayout();
        }
        private (int colSpan, int rowSpan) CalculateWidgetSpan(FrameworkElement widget)
        {
            double cellWidth = DashboardGrid.ActualWidth / 12;
            double cellHeight = 80;

            double widgetWidth = widget.Width;
            double widgetHeight = widget.Height;

            int colSpan = (int)Math.Round(widgetWidth / cellWidth);
            int rowSpan = (int)Math.Ceiling(widgetHeight / cellHeight);

            if (colSpan < 1) colSpan = 1;
            if (rowSpan < 1) rowSpan = 1;

            return (colSpan, rowSpan);
        }

        //private (int colSpan, int rowSpan) CalculateWidgetSpan(FrameworkElement widget)
        //{
        //	// Đọc cấu hình từ Tag (ví dụ "2x2", "4x3")
        //	string config = widget.Tag as string;

        //	if (!string.IsNullOrEmpty(config) && config.Contains("x"))
        //	{
        //		var parts = config.Split('x');
        //		if (parts.Length == 2)
        //		{
        //			int cols = int.Parse(parts[0]);
        //			int rows = int.Parse(parts[1]);
        //			return (cols, rows);
        //		}
        //	}

        //	return (2, 2); // Mặc định nếu không có cấu hình
        //}
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
            bool exists = DashboardGrid.Children
                .OfType<FrameworkElement>()
                .Any(x => x.GetType() == widget.WidgetType);

            if (exists)
            {
                MessageBox.Show("Widget này đã tồn tại trên dashboard!");
                return;
            }

            SetupWidget(newWidget);


            var span = CalculateWidgetSpan(newWidget);
            var pos = FindFreePosition(span.rowSpan, span.colSpan);

            EnsureRow(pos.Row + span.rowSpan);

            Grid.SetRow(newWidget, pos.Row);
            Grid.SetColumn(newWidget, pos.Column);

            Grid.SetColumnSpan(newWidget, span.colSpan);
            Grid.SetRowSpan(newWidget, span.rowSpan);

            DashboardGrid.Children.Add(newWidget);
        }
        private (int Row, int Column) FindFreePosition(int rowSpan, int colSpan)
        {
            int maxCols = 12;

            HashSet<string> used = new HashSet<string>();

            foreach (UIElement child in DashboardGrid.Children)
            {
                int r = Grid.GetRow(child);
                int c = Grid.GetColumn(child);
                int rs = Grid.GetRowSpan(child);
                int cs = Grid.GetColumnSpan(child);

                for (int i = r; i < r + rs; i++)
                {
                    for (int j = c; j < c + cs; j++)
                    {
                        used.Add($"{i}-{j}");
                    }
                }
            }

            for (int row = 0; row < 100; row++)
            {
                for (int col = 0; col <= maxCols - colSpan; col++)
                {
                    bool free = true;

                    for (int r = row; r < row + rowSpan; r++)
                    {
                        for (int c = col; c < col + colSpan; c++)
                        {
                            if (used.Contains($"{r}-{c}"))
                            {
                                free = false;
                                break;
                            }
                        }

                        if (!free) break;
                    }

                    if (free)
                        return (row, col);
                }
            }

            return (0, 0);
        }
        private (int Row, int Column) GetGridPosition()
        {
            int count = DashboardGrid.Children.Count;

            int columns = DashboardGrid.ColumnDefinitions.Count;

            int row = count / columns;
            int column = count % columns;

            return (row, column);
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
            OpenDashboard(sender as Button);
        }
        void OpenDashboard(Button btn)
        {
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
            InitGrid();
            CreateGrid();
            GridOverlay.Visibility = Visibility.Visible;
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
                        Row = Grid.GetRow(widget),
                        Column = Grid.GetColumn(widget),
                        RowSpan = Grid.GetRowSpan(widget),
                        ColumnSpan = Grid.GetColumnSpan(widget)
                    };

                    layouts.Add(layout);
                }
            }

            SaveLayout(layouts);
            ExitEditMode();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadLayout();
            // đóng widget library
            ExitEditMode();
        }
        private void EnsureRow(int rowIndex)
        {
            while (DashboardGrid.RowDefinitions.Count <= rowIndex)
            {
                DashboardGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = new GridLength(80)   // chiều cao 1 ô
                    });
            }
        }
        private void ExitEditMode()
        {
            WidgetLibraryColumn.Width = new GridLength(0);

            editMode = false;
            GridOverlay.Visibility = Visibility.Collapsed;
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

        private void Widget_MouseMove(object sender, MouseEventArgs e)
        {
            if (!editMode) return;

            if (!isDraggingWidget || selectedWidget == null)
                return;

            FrameworkElement widget = sender as FrameworkElement;

            Point pos = e.GetPosition(DashboardGrid);

            int columnCount = 12;

            double cellWidth = DashboardGrid.ActualWidth / columnCount;
            double cellHeight = 80;

            int column = (int)(pos.X / cellWidth);
            int row = (int)(pos.Y / cellHeight);

            int colSpan = Grid.GetColumnSpan(widget);
            int rowSpan = Grid.GetRowSpan(widget);

            column = Math.Max(0, Math.Min(column, columnCount - colSpan));
            row = Math.Max(0, row);

            Grid.SetColumn(widget, column);
            Grid.SetRow(widget, row);
        }

        private void DashboardGrid_Drop(object sender, DragEventArgs e)
        {
            if (!editMode)
                return;

            if (!e.Data.GetDataPresent(typeof(WidgetItem)))
                return;

            WidgetItem widgetItem = e.Data.GetData(typeof(WidgetItem)) as WidgetItem;

            if (widgetItem == null)
                return;

            bool exists = DashboardGrid.Children
                .OfType<FrameworkElement>()
                .Any(x => x.GetType() == widgetItem.WidgetType);

            if (exists)
            {
                MessageBox.Show("Widget này đã tồn tại trên dashboard!");
                return;
            }

            FrameworkElement newWidget =
                Activator.CreateInstance(widgetItem.WidgetType) as FrameworkElement;

            if (newWidget == null)
                return;

            SetupWidget(newWidget);

            // vị trí drop
            Point position = e.GetPosition(DashboardGrid);

            double cellWidth = DashboardGrid.ActualWidth / 12;
            double cellHeight = 80;

            int column = (int)(position.X / cellWidth);
            int row = (int)(position.Y / cellHeight);

            // gọi hàm tính span
            var span = CalculateWidgetSpan(newWidget);

            int colSpan = span.colSpan;
            int rowSpan = span.rowSpan;

            // tránh vượt quá 12 column
            if (column + colSpan > 12)
                column = 12 - colSpan;

            // đảm bảo đủ row
            EnsureRow(row + rowSpan);

            Grid.SetColumn(newWidget, column);
            Grid.SetRow(newWidget, row);

            Grid.SetColumnSpan(newWidget, colSpan);
            Grid.SetRowSpan(newWidget, rowSpan);

            DashboardGrid.Children.Add(newWidget);
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
                SmartCascadePush(selectedWidget);
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

            // lưu tất cả cell đã dùng
            HashSet<string> usedCells = new HashSet<string>();

            foreach (var layout in layouts)
            {
                if (layout.Dashboard != currentDashboard)
                    continue;

                FrameworkElement widget = CreateWidget(layout.Type);

                if (widget == null)
                    continue;

                int row = layout.Row;
                int col = layout.Column;

                int rowSpan = layout.RowSpan <= 0 ? 1 : layout.RowSpan;
                int colSpan = layout.ColumnSpan <= 0 ? 1 : layout.ColumnSpan;

                // đảm bảo grid đủ hàng
                EnsureRow(row + rowSpan);

                // kiểm tra toàn bộ vùng widget chiếm
                bool isOverlap = false;

                for (int r = row; r < row + rowSpan; r++)
                {
                    for (int c = col; c < col + colSpan; c++)
                    {
                        string key = $"{r}-{c}";
                        if (usedCells.Contains(key))
                        {
                            isOverlap = true;
                            break;
                        }
                    }
                    if (isOverlap)
                        break;
                }

                if (isOverlap)
                    continue;

                SetupWidget(widget);

                Grid.SetRow(widget, row);
                Grid.SetColumn(widget, col);
                Grid.SetRowSpan(widget, rowSpan);
                Grid.SetColumnSpan(widget, colSpan);

                Panel.SetZIndex(widget, DashboardGrid.Children.Count);

                DashboardGrid.Children.Add(widget);

                // đánh dấu tất cả cell đã dùng
                for (int r = row; r < row + rowSpan; r++)
                {
                    for (int c = col; c < col + colSpan; c++)
                    {
                        usedCells.Add($"{r}-{c}");
                    }
                }
            }
        }
        void InitGrid()
        {
            GridOverlay.RowDefinitions.Clear();
            GridOverlay.ColumnDefinitions.Clear();

            DashboardGrid.RowDefinitions.Clear();
            DashboardGrid.ColumnDefinitions.Clear();

            // 12 column
            for (int i = 0; i < 12; i++)
            {
                GridOverlay.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                DashboardGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // 10 row
            for (int i = 0; i < 10; i++)
            {
                GridOverlay.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(80) });

                DashboardGrid.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(80) });
            }

            CreateGrid();
        }
        void CreateGrid()
        {
            GridOverlay.Children.Clear();

            int rowCount = GridOverlay.RowDefinitions.Count;
            int colCount = GridOverlay.ColumnDefinitions.Count;

            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    Border cell = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                        BorderThickness = new Thickness(0.5),
                        Background = Brushes.Transparent
                    };

                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);

                    GridOverlay.Children.Add(cell);
                }
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
            widget.Margin = new Thickness(1, 1, 5, 5);

            widget.HorizontalAlignment = HorizontalAlignment.Stretch;
            widget.VerticalAlignment = VerticalAlignment.Stretch;
            widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown;
            widget.MouseMove += Widget_MouseMove;
            widget.MouseLeftButtonUp += Widget_MouseLeftButtonUp;

            if (widget is IDashboardWidget dashboardWidget)
            {
                dashboardWidget.DeleteRequested += Widget_DeleteRequested;
                dashboardWidget.SetEditMode(editMode);
            }
            // Tìm nút ResizeThumb trong Widget
            var thumb = FindVisualChild<Thumb>(widget, "ResizeThumb");
            if (thumb != null)
            {
                thumb.Visibility = editMode ? Visibility.Visible : Visibility.Collapsed;

                thumb.DragDelta += (s, e) =>
                {
                    if (!editMode) return;

                    double cellWidth = DashboardGrid.ActualWidth / 12;
                    double cellHeight = 80;

                    // --- PHẦN DEBUG ---
                    System.Diagnostics.Debug.WriteLine($"--- RESIZING {widget.GetType().Name} ---");
                    System.Diagnostics.Debug.WriteLine($"Delta X: {e.HorizontalChange:F2}, Delta Y: {e.VerticalChange:F2}");
                    System.Diagnostics.Debug.WriteLine($"Actual Size: {widget.ActualWidth:F2}x{widget.ActualHeight:F2}");
                    System.Diagnostics.Debug.WriteLine($"Cell Size: {cellWidth:F2}x{cellHeight:F2}");

                    // Tính toán Span mới
                    int newColSpan = (int)Math.Max(1, Math.Round((widget.ActualWidth + e.HorizontalChange) / cellWidth));
                    int newRowSpan = (int)Math.Max(1, Math.Round((widget.ActualHeight + e.VerticalChange) / cellHeight));

                    System.Diagnostics.Debug.WriteLine($"Calculated Span: {newColSpan}x{newRowSpan}");

                    // Cập nhật giao diện
                    Grid.SetColumnSpan(widget, newColSpan);
                    Grid.SetRowSpan(widget, newRowSpan);

                    widget.Tag = $"{newColSpan}x{newRowSpan}";
                };
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
        private T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t && (child as FrameworkElement).Name == name)
                    return t;

                T childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        bool IsOverlap(FrameworkElement a, FrameworkElement b)
        {
            int r1 = Grid.GetRow(a);
            int c1 = Grid.GetColumn(a);
            int rs1 = Grid.GetRowSpan(a);
            int cs1 = Grid.GetColumnSpan(a);

            int r2 = Grid.GetRow(b);
            int c2 = Grid.GetColumn(b);
            int rs2 = Grid.GetRowSpan(b);
            int cs2 = Grid.GetColumnSpan(b);

            return r1 < r2 + rs2 &&
                   r1 + rs1 > r2 &&
                   c1 < c2 + cs2 &&
                   c1 + cs1 > c2;
        }
        // hàm xử lý việc đè widget khi kéo thả, sẽ tự động tìm vị trí mới cho widget bị đè
        (bool found, int row, int col) FindNearestPosition(int startRow, int startCol, int rowSpan, int colSpan)
        {
            int maxCols = 12;

            // 1. thử sang phải
            for (int col = startCol + 1; col <= maxCols - colSpan; col++)
            {
                if (IsAreaFree(startRow, col, rowSpan, colSpan))
                    return (true, startRow, col);
            }

            // 2. thử sang trái
            for (int col = startCol - 1; col >= 0; col--)
            {
                if (IsAreaFree(startRow, col, rowSpan, colSpan))
                    return (true, startRow, col);
            }

            // 3. tìm xuống dưới nhiều row
            for (int row = startRow + 1; row < 200; row++)
            {
                for (int col = 0; col <= maxCols - colSpan; col++)
                {
                    if (IsAreaFree(row, col, rowSpan, colSpan))
                        return (true, row, col);
                }
            }

            return (false, startRow, startCol);
        }
        bool IsAreaFree(int row, int col, int rowSpan, int colSpan)
        {
            foreach (FrameworkElement widget in DashboardGrid.Children.OfType<FrameworkElement>())
            {
                int r = Grid.GetRow(widget);
                int c = Grid.GetColumn(widget);
                int rs = Grid.GetRowSpan(widget);
                int cs = Grid.GetColumnSpan(widget);

                bool overlap =
                    r < row + rowSpan &&
                    r + rs > row &&
                    c < col + colSpan &&
                    c + cs > col;

                if (overlap)
                    return false;
            }

            return true;
        }
        void SmartCascadePush(FrameworkElement movedWidget)
        {
            Queue<FrameworkElement> queue = new Queue<FrameworkElement>();
            queue.Enqueue(movedWidget);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (FrameworkElement widget in DashboardGrid.Children.OfType<FrameworkElement>().OrderBy(x => Grid.GetRow(x)))
                {
                    if (widget == current)
                        continue;

                    if (IsOverlap(current, widget))
                    {
                        int r = Grid.GetRow(widget);
                        int c = Grid.GetColumn(widget);
                        int rs = Grid.GetRowSpan(widget);
                        int cs = Grid.GetColumnSpan(widget);

                        var pos = FindNearestPosition(r, c, rs, cs);

                        if (pos.found)
                        {
                            EnsureRow(pos.row + rs);

                            Grid.SetRow(widget, pos.row);
                            Grid.SetColumn(widget, pos.col);

                            queue.Enqueue(widget);
                        }
                    }
                }
            }
        }
        void AddMoreRows(int count)
        {
            for (int i = 0; i < count; i++)
            {
                DashboardGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = new GridLength(80)
                    });

                GridOverlay.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = new GridLength(80)
                    });
            }

            CreateGrid();
        }
        private void DashboardScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 5)
            {
                AddMoreRows(10);
            }
        }

    }

}
