using LightInsight.Dashboard.AlarmsAndEvents;
using LightInsight.Dashboard.Camera.Client;
using LightInsight.Dashboard.Dashboard.Workspace;
using LightInsight.Dashboard.RecordingServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using VideoOS.Platform.Client;
using VideoOS.Platform;
using Path = System.IO.Path;
using VideoOS.Platform.Messaging;
using System.Threading;
using VideoOS.Platform.OAuth;
using Microsoft.Identity.Client;
using System.Configuration;
using System.Security.AccessControl;
using System.Security.Principal;


namespace LightInsight.Dashboard.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl, INotifyPropertyChanged
    {
        private ResourceDictionary _currentThemeDictionary;
        private ResourceDictionary _currentLanguageDictionary;
        private object _themeChangedRegistration;
        bool editMode = false;
        private bool _isDirty = false;
        private Point startPoint;

        public static readonly DependencyProperty CurrentDashboardProperty =
            DependencyProperty.Register("CurrentDashboard", typeof(string), typeof(DashboardView), new PropertyMetadata("Default Workspace"));

        public string CurrentDashboard
        {
            get { return (string)GetValue(CurrentDashboardProperty); }
            set { SetValue(CurrentDashboardProperty, value); }
        }

        private bool _isSidebarCollapsed = false;
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                _isSidebarCollapsed = value;
                OnPropertyChanged(nameof(IsSidebarCollapsed));
            }
        }

        private CancellationTokenSource _popupCloseCancellation;

        private async void Expander_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsSidebarCollapsed) return;
            _popupCloseCancellation?.Cancel();
            SubMenuPopup.IsOpen = true;
        }

        private async void Expander_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsSidebarCollapsed) return;
            _popupCloseCancellation?.Cancel();
            _popupCloseCancellation = new CancellationTokenSource();
            var token = _popupCloseCancellation.Token;
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    if (!SubMenuPopup.IsMouseOver && !DashboardExpander.IsMouseOver)
                    {
                        SubMenuPopup.IsOpen = false;
                    }
                }
            }
            catch (TaskCanceledException) { }
        }

        private void Popup_MouseEnter(object sender, MouseEventArgs e)
        {
            _popupCloseCancellation?.Cancel();
        }

        private void Popup_MouseLeave(object sender, MouseEventArgs e)
        {
            Expander_MouseLeave(sender, e);
        }

        FrameworkElement selectedWidget = null;
        bool isDraggingWidget = false;
        private Point _clickOffset;
        public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		private ObservableCollection<WorkspaceModel> _dashboardMenus;
		public ObservableCollection<WorkspaceModel> DashboardMenus
		{
			get => _dashboardMenus;
			set
			{
				_dashboardMenus = value;
				OnPropertyChanged(nameof(DashboardMenus));
			}
		}

		private ObservableCollection<WidgetGroup> _widgetGroups = new ObservableCollection<WidgetGroup>();
		public ObservableCollection<WidgetGroup> WidgetGroups
		{
			get => _widgetGroups;
			set
			{
				_widgetGroups = value;
				OnPropertyChanged(nameof(WidgetGroups));
			}
		}

		List<WidgetItem> allWidgets = new List<WidgetItem>()
		{
			new WidgetItem{ Name="Camera Online Count", Category="Camera", Description="Cameras online", WidgetType = typeof(CameraOnlineWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.Camera},
			new WidgetItem{ Name="Camera Offline Count", Category="Camera", Description="Cameras offline", WidgetType = typeof(CameraOfflineWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.CameraOff},
			new WidgetItem{ Name="Camera Total Count", Category="Camera", Description="Total cameras", WidgetType = typeof(TotalCameraCount), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.Video},
			new WidgetItem{ Name="Camera Online + Offline", Category="Camera", Description="Online and offline summary", WidgetType = typeof(CameraOnlineNOffline), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.VideoOutline},
			new WidgetItem{ Name="Camera Status Donut", Category="Camera", Description="Camera status distribution", WidgetType = typeof(CameraStatusDonut), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ChartDonut},
			new WidgetItem{ Name="Camera Offline Duration top 10", Category="Camera", Description="Top 10 longest disconnections", WidgetType = typeof(CameraOfflineDurationTop10), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.Table},
			new WidgetItem{ Name="Camera Disconnection Trend", Category="Camera", Description="Trend of disconnected cameras", WidgetType = typeof(CameraDisconnectionTrend), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ChartLine},
			new WidgetItem{ Name="Camera Analytics Summary", Category="Camera", Description="Analytics events summary", WidgetType = typeof(CameraAnalyticsSummaryWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.GoogleAnalytics},
			new WidgetItem{ Name="Camera List", Category="Camera", Description="Detailed camera inventory", WidgetType = typeof(CameraListWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.FormatListBulleted},
			new WidgetItem{ Name="Camera Health Score", Category="Camera", Description="Health metrics overview", WidgetType = typeof(CameraHealthScoreWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.HeartPulse},
			new WidgetItem{ Name="Live Alarm Feed", Category="Alarms & Events", Description="Recent alarm activity", WidgetType = typeof(LiveAlarmsFeedWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.BellRing},
			new WidgetItem{ Name="Alarm by Severity", Category="Alarms & Events", Description="Alarms grouped by severity", WidgetType = typeof(AlarmBySeverityWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ChartPie},
			new WidgetItem{ Name="Alarm Daily Count", Category="Alarms & Events", Description="Alarm trend by day", WidgetType = typeof(AlarmDailyCountWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ChartBar},
			new WidgetItem{ Name="Alarm by Source", Category="Alarms & Events", Description="Alarms by source device", WidgetType = typeof(AlarmBySourceWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.SourceBranch},
			new WidgetItem{ Name="Alarm by Type", Category="Alarms & Events", Description="Alarms by category", WidgetType = typeof(AlarmByTypeWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.Shape},
			new WidgetItem{ Name="Alarm SLA Breach", Category="Alarms & Events", Description="SLA compliance status", WidgetType = typeof(AlarmSLABreachWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ClockAlert},
			new WidgetItem{ Name="Event Trend Chart", Category="Alarms & Events", Description="Events frequency trend", WidgetType = typeof(EventTrendChartWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ChartTimelineVariant},
            new WidgetItem{ Name="Servers Online Count", Category="Recording Server", Description="Servers online", WidgetType = typeof(ServersOnlineCountWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.Server},
            new WidgetItem{ Name="Servers Offline Count", Category="Recording Server", Description="Servers offline", WidgetType = typeof(ServersOfflineCountWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ServerOff},
            new WidgetItem{ Name="Servers Total", Category="Recording Server", Description="Total recording servers", WidgetType = typeof(TotalServersWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.ServerNetwork},
            new WidgetItem{ Name="Storage Usage by Server", Category="Recording Server", Description="Storage utilization", WidgetType = typeof(StorageUsageWidget), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.Database},
            new WidgetItem{ Name="Template", Category="Recording Server", Description="Standard template", WidgetType = typeof(Temp), Icon = MahApps.Metro.IconPacks.PackIconMaterialKind.FileOutline},
        };

        public DashboardView()
        {
            InitializeComponent();
            TimeRangeCombo.SelectedIndex = 0;
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            LocalizeWidgetNames();
            LoadSidebar();
            WorkspaceService.Instance.OnWorkspaceChanged += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadSidebar();
                });
            };
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            
            UpdateWidgetGroups();
            EnsureDashboardFile();

            if (DashboardMenus != null && DashboardMenus.Any())
            {
                var first = DashboardMenus[0];
                first.IsSelected = true;
                CurrentDashboard = first.Name;
                BreadcrumbText.Text = $"Dashboard > {CurrentDashboard}";
            }
            LoadLayout();
        }

        private void LocalizeWidgetNames()
        {
            string GetText(string key, string fallback)
            {
                return TryFindResource(key) as string ?? fallback;
            }

			foreach (var w in allWidgets)
			{
				if (w.WidgetType == typeof(CameraOnlineWidget)) w.Name = GetText("WidgetName_CameraOnlineCount", w.Name);
				else if (w.WidgetType == typeof(CameraOfflineWidget)) w.Name = GetText("WidgetName_CameraOfflineCount", w.Name);
				else if (w.WidgetType == typeof(TotalCameraCount)) w.Name = GetText("WidgetName_CameraTotalCount", w.Name);
				else if (w.WidgetType == typeof(CameraOnlineNOffline)) w.Name = GetText("WidgetName_CameraOnlinePlusOffline", w.Name);
				else if (w.WidgetType == typeof(CameraStatusDonut)) w.Name = GetText("WidgetName_CameraStatusDonut", w.Name);
				else if (w.WidgetType == typeof(CameraOfflineDurationTop10)) w.Name = GetText("WidgetName_CameraOfflineDurationTop10", w.Name);
				else if (w.WidgetType == typeof(CameraDisconnectionTrend)) w.Name = GetText("WidgetName_CameraDisconnectionTrend", w.Name);
				else if (w.WidgetType == typeof(CameraAnalyticsSummaryWidget)) w.Name = GetText("WidgetName_CameraAnalyticsSummary", w.Name);
				else if (w.WidgetType == typeof(CameraListWidget)) w.Name = GetText("WidgetName_CameraList", w.Name);
				else if (w.WidgetType == typeof(CameraHealthScoreWidget)) w.Name = GetText("WidgetName_CameraHealthScore", w.Name);
				else if (w.WidgetType == typeof(LiveAlarmsFeedWidget)) w.Name = GetText("WidgetName_LiveAlarmFeed", w.Name);
				else if (w.WidgetType == typeof(AlarmBySeverityWidget)) w.Name = GetText("WidgetName_AlarmBySeverity", w.Name);
				else if (w.WidgetType == typeof(AlarmDailyCountWidget)) w.Name = GetText("WidgetName_AlarmDailyCount", w.Name);
				else if (w.WidgetType == typeof(AlarmBySourceWidget)) w.Name = GetText("WidgetName_AlarmBySource", w.Name);
				else if (w.WidgetType == typeof(AlarmByTypeWidget)) w.Name = GetText("WidgetName_AlarmByType", w.Name);
				else if (w.WidgetType == typeof(AlarmSLABreachWidget)) w.Name = GetText("WidgetName_AlarmSLABreach", w.Name);
				else if (w.WidgetType == typeof(EventTrendChartWidget)) w.Name = GetText("WidgetName_EventTrendChart", w.Name);
				else if (w.WidgetType == typeof(ServersOnlineCountWidget)) w.Name = GetText("WidgetName_ServersOnlineCount", w.Name);
				else if (w.WidgetType == typeof(ServersOfflineCountWidget)) w.Name = GetText("WidgetName_ServersOfflineCount", w.Name);
				else if (w.WidgetType == typeof(TotalServersWidget)) w.Name = GetText("WidgetName_ServersTotal", w.Name);
				else if (w.WidgetType == typeof(StorageUsageWidget)) w.Name = GetText("WidgetName_StorageUsageByServer", w.Name);
				else if (w.WidgetType == typeof(Temp)) w.Name = GetText("WidgetName_Template", w.Name);
			}
		}

		private (int colSpan, int rowSpan) CalculateWidgetSpan(FrameworkElement widget)
		{
			string config = widget.Tag as string;
			if (!string.IsNullOrEmpty(config) && config.Contains("x"))
			{
				var parts = config.Split('x');
				if (parts.Length == 2)
				{
					int cols = int.Parse(parts[0]);
					int rows = int.Parse(parts[1]);
					return (cols, rows);
				}
			}
			return (2, 2);
		}

		private void Filter_Click(object sender, RoutedEventArgs e)
		{
			ToggleButton clickedBtn = sender as ToggleButton;
			if (clickedBtn == null) return;

			if (clickedBtn.IsChecked == true)
			{
				string filter = clickedBtn.Tag?.ToString();
				UpdateWidgetGroups(filter);
			}
			else
			{
				UpdateWidgetGroups();
			}
		}

		private void AddWidget_Click(object sender, RoutedEventArgs e)
		{
			Button btn = sender as Button;
			WidgetItem widget = btn.Tag as WidgetItem;
			if (widget == null) return;

			FrameworkElement newWidget = Activator.CreateInstance(widget.WidgetType) as FrameworkElement;
			if (newWidget == null) return;

			bool exists = DashboardGrid.Children.OfType<FrameworkElement>().Any(x => x.GetType() == widget.WidgetType);
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
            _isDirty = true; // Ghi nhận thay đổi
		}

		private void ExitEditModeBtn_Click(object sender, RoutedEventArgs e)
		{
			ExitEditMode();
		}

		private void ExitEditMode()
		{
			WidgetLibraryColumn.Width = new GridLength(0);
			GridOverlay.Visibility = Visibility.Collapsed;
			WidgetLibrary.Visibility = Visibility.Collapsed;
			EditLayoutBtn.Visibility = Visibility.Visible;
			SaveBtn.Visibility = Visibility.Collapsed;
			CancelBtn.Visibility = Visibility.Collapsed;

			foreach (var widget in DashboardGrid.Children.OfType<IDashboardWidget>())
			{
				widget.SetEditMode(false);
			}

			TrimEmptyRows();
            editMode = false; // Tắt trạng thái edit sau cùng để TrimEmptyRows dọn dẹp grid triệt để
            GridOverlay.Children.Clear(); // Xóa sạch các đường lưới
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
					for (int j = c; j < c + cs; j++) used.Add($"{i}-{j}");
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
							if (used.Contains($"{r}-{c}")) { free = false; break; }
						}
						if (!free) break;
					}
					if (free) return (row, col);
				}
			}
			return (0, 0);
		}

        private void UpdateWidgetGroups(string filter = "")
        {
            var query = allWidgets.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(x => x.Name.ToLower().Contains(filter.ToLower()) || 
                                         x.Category.ToLower().Contains(filter.ToLower()));
            }

            var grouped = query.GroupBy(x => x.Category)
                               .Select(g => new WidgetGroup 
                               { 
                                   Title = g.Key, 
                                   Items = new ObservableCollection<WidgetItem>(g.ToList()) 
                               })
                               .OrderBy(x => x.Title)
                               .ToList();

            WidgetGroups = new ObservableCollection<WidgetGroup>(grouped);
        }

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateWidgetGroups(SearchBox.Text);
		}

		void RefreshBtn_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Refreshing dashboard...");
		}

		private void Menu_Click(object sender, RoutedEventArgs e)
		{
			var item = (sender as FrameworkElement)?.DataContext as WorkspaceModel;
			if (item == null) return;
            OpenDashboard(item);
            ExitEditMode();
            if (IsSidebarCollapsed) SubMenuPopup.IsOpen = false;
        }

		private void OpenDashboard(WorkspaceModel item)
		{
			if (!ConfirmBeforeLeave()) return;
            
            if (DashboardMenus != null)
            {
                foreach (var m in DashboardMenus) m.IsSelected = (m == item);
            }

			CurrentDashboard = item.Name;
			BreadcrumbText.Text = $"Dashboard > {item.Name}";
			LoadLayout();
		}

		private void Widget_MouseDown(object sender, MouseButtonEventArgs e)
		{
			startPoint = e.GetPosition(null);
		}

		private void EditLayoutBtn_Click(object sender, RoutedEventArgs e)
		{
			editMode = true;
			_isDirty = false;
			InitGrid();
			CreateGrid();
			GridOverlay.Visibility = Visibility.Visible;
			WidgetLibraryColumn.Width = new GridLength(280);
			WidgetLibrary.Visibility = Visibility.Visible;
			EditLayoutBtn.Visibility = Visibility.Collapsed;
			SaveBtn.Visibility = Visibility.Visible;
			CancelBtn.Visibility = Visibility.Visible;
			foreach (var widget in DashboardGrid.Children.OfType<FrameworkElement>()) 
            {
                SetupWidget(widget);
                if (widget is IDashboardWidget dw) dw.SetEditMode(true);
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
						Dashboard = CurrentDashboard,
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
			ExitEditMode();
		}

		private void EnsureRow(int rowIndex)
		{
			while (DashboardGrid.RowDefinitions.Count <= rowIndex)
			{
				DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
			}
		}

		private void TrimEmptyRows()
		{
		    int maxRowUsed = 0;
		    foreach (FrameworkElement child in DashboardGrid.Children)
		    {
		        int row = Grid.GetRow(child);
		        int rowSpan = Grid.GetRowSpan(child);
		        if (row + rowSpan > maxRowUsed) maxRowUsed = row + rowSpan;
		    }
		    int minRows = 10;
		    int targetRows = Math.Max(maxRowUsed, minRows);
		    while (DashboardGrid.RowDefinitions.Count > targetRows) DashboardGrid.RowDefinitions.RemoveAt(DashboardGrid.RowDefinitions.Count - 1);
		    while (GridOverlay.RowDefinitions.Count > targetRows) GridOverlay.RowDefinitions.RemoveAt(GridOverlay.RowDefinitions.Count - 1);
		    if (editMode) CreateGrid();
		}

        private void HandleAutoScroll(DragEventArgs e)
        {
            Point posInScroll = e.GetPosition(DashboardScroll);
            double scrollMargin = 50; double scrollSpeed = 15;
            if (posInScroll.Y < scrollMargin) DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset - scrollSpeed);
            else if (posInScroll.Y > DashboardScroll.ViewportHeight - scrollMargin) DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset + scrollSpeed);
        }

        private void HandleAutoScroll(MouseEventArgs e)
        {
            Point posInScroll = e.GetPosition(DashboardScroll);
            double scrollMargin = 50; double scrollSpeed = 15;
            if (posInScroll.Y < scrollMargin) DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset - scrollSpeed);
            else if (posInScroll.Y > DashboardScroll.ViewportHeight - scrollMargin)
            {
                DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset + scrollSpeed);
                if (DashboardScroll.VerticalOffset + DashboardScroll.ViewportHeight >= DashboardScroll.ExtentHeight - 20) AddMoreRows(5);
            }
        }

        private void DashboardGrid_DragOver(object sender, DragEventArgs e)
        {
            if (!editMode) 
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Kiểm tra xem dữ liệu có phải là WidgetItem không
            if (e.Data.GetDataPresent(typeof(WidgetItem)))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true; // Rất quan trọng để Windows biết chúng ta đã xử lý việc kéo này

                // Auto scroll và nới hàng
                HandleAutoScroll(e);
                Point posInGrid = e.GetPosition(DashboardGrid);
                if (posInGrid.Y > DashboardGrid.ActualHeight - 50) AddMoreRows(5);
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void Widget_MouseMove(object sender, MouseEventArgs e)
        {
            if (!editMode || !isDraggingWidget || selectedWidget == null) return;

            Point currentPoint = e.GetPosition(DashboardGrid);
            Vector diff = startPoint - currentPoint;

            // Chỉ bắt đầu di chuyển nếu chuột đã di chuyển một khoảng nhất định (tránh click vô tình)
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance && 
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            HandleAutoScroll(e);
            FrameworkElement widget = selectedWidget;
            int columnCount = 12; double cellWidth = DashboardGrid.ActualWidth / columnCount; double cellHeight = 80;
            
            // Tính toán vị trí mới dựa trên điểm click ban đầu (trừ đi offset để giữ nguyên vị trí chuột trên widget)
            double adjustedX = currentPoint.X - _clickOffset.X;
            double adjustedY = currentPoint.Y - _clickOffset.Y;
            
            int column = (int)Math.Round(adjustedX / cellWidth); 
            int row = (int)Math.Round(adjustedY / cellHeight);
            
            int colSpan = Grid.GetColumnSpan(widget); 
            int rowSpan = Grid.GetRowSpan(widget);
            
            column = Math.Max(0, Math.Min(column, columnCount - colSpan)); 
            row = Math.Max(0, row);

            if (Grid.GetColumn(widget) != column || Grid.GetRow(widget) != row)
            {
                Grid.SetColumn(widget, column); Grid.SetRow(widget, row);
                _isDirty = true;
            }
        }

		private void DashboardGrid_Drop(object sender, DragEventArgs e)
		{
			_isDirty = true;
			if (!editMode || !e.Data.GetDataPresent(typeof(WidgetItem))) return;
			WidgetItem widgetItem = e.Data.GetData(typeof(WidgetItem)) as WidgetItem;
			if (widgetItem == null) return;
			if (IsWidgetAdded(widgetItem.WidgetType)) { MessageBox.Show("Widget này đã tồn tại trên dashboard!"); return; }
			FrameworkElement newWidget = Activator.CreateInstance(widgetItem.WidgetType) as FrameworkElement;
			if (newWidget == null) return;
			SetupWidget(newWidget);
			Point position = e.GetPosition(DashboardGrid);
			double cellWidth = DashboardGrid.ActualWidth / 12; double cellHeight = 80;
			int column = (int)(position.X / cellWidth); int row = (int)(position.Y / cellHeight);
			var span = CalculateWidgetSpan(newWidget);
			if (column + span.colSpan > 12) column = 12 - span.colSpan;
			EnsureRow(row + span.rowSpan);
			Grid.SetColumn(newWidget, column); Grid.SetRow(newWidget, row);
			Grid.SetColumnSpan(newWidget, span.colSpan); Grid.SetRowSpan(newWidget, span.rowSpan);
			DashboardGrid.Children.Add(newWidget);
            SmartCascadePush(newWidget); // Đảm bảo không đè lên widget khác khi drop
		}

        private Point _dragStartPoint;

        private void WidgetLibrary_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void WidgetLibrary_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            // Kiểm tra khoảng cách kéo chuẩn WPF
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || 
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                FrameworkElement element = sender as FrameworkElement;
                if (element == null) return;
                
                WidgetItem widget = element.DataContext as WidgetItem;
                if (widget == null) return;

                // Sử dụng DataObject giống Plugin để đảm bảo tương thích dữ liệu khi kéo
                DataObject dragData = new DataObject(typeof(WidgetItem), widget);
                DragDrop.DoDragDrop(element, dragData, DragDropEffects.Copy);
            }
        }

        // Thêm sự kiện Click cho Border (thay thế Button.Click cũ)
        private void WidgetLibrary_Item_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            // Nếu không phải là hành động kéo (di chuyển ít hơn 3px) thì coi là Click
            if (Math.Abs(diff.X) <= 3 && Math.Abs(diff.Y) <= 3)
            {
                FrameworkElement element = sender as FrameworkElement;
                WidgetItem widget = element?.DataContext as WidgetItem;
                if (widget != null)
                {
                    // Tạo một sender giả lập Button để tái sử dụng hàm AddWidget_Click
                    Button pseudoButton = new Button { Tag = widget };
                    AddWidget_Click(pseudoButton, null);
                }
            }
        }

		void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!editMode) return;
			selectedWidget = sender as FrameworkElement;
			startPoint = e.GetPosition(DashboardGrid);
            _clickOffset = e.GetPosition(selectedWidget); // Lưu vị trí click tương đối trong widget
			isDraggingWidget = true;
			selectedWidget.CaptureMouse();

            // Đưa widget đang kéo lên trên cùng về mặt hiển thị
            Panel.SetZIndex(selectedWidget, 999);
		}

		void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (selectedWidget != null) { SmartCascadePush(selectedWidget); selectedWidget.ReleaseMouseCapture(); }
			isDraggingWidget = false;
			selectedWidget = null;
		}

		void Widget_DeleteRequested(object sender, EventArgs e)
		{
			if (sender is FrameworkElement widget) 
            {
                DashboardGrid.Children.Remove(widget);
                _isDirty = true;
            }
		}

		void SaveLayout(List<WidgetLayout> newLayouts)
		{
			_isDirty = false;
			string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LightInsight");
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
			string filePath = Path.Combine(folder, "dashboard_layout.json");
			List<WidgetLayout> allLayouts = new List<WidgetLayout>();
			if (File.Exists(filePath))
			{
				string oldJson = File.ReadAllText(filePath);
				if (!string.IsNullOrWhiteSpace(oldJson))
				{
					try { allLayouts = JsonSerializer.Deserialize<List<WidgetLayout>>(oldJson); }
					catch { allLayouts = new List<WidgetLayout>(); }
				}
			}
			allLayouts.RemoveAll(x => x.Dashboard == CurrentDashboard);
			allLayouts.AddRange(newLayouts);
            string json = JsonSerializer.Serialize(allLayouts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            SetFilePermissionForAllUsers(filePath);
        }

        void LoadLayout()
        {
            if (DashboardGrid.Children.Count > 0) DashboardGrid.Children.Clear();
			string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LightInsight");
			string filePath = Path.Combine(folder, "dashboard_layout.json");
			if (!File.Exists(filePath)) return;
			string json = File.ReadAllText(filePath);
			if (string.IsNullOrWhiteSpace(json)) return;
			List<WidgetLayout> layouts;
			try { layouts = JsonSerializer.Deserialize<List<WidgetLayout>>(json); }
			catch { return; }
			if (layouts == null) return;
			HashSet<string> usedCells = new HashSet<string>();
			foreach (var layout in layouts)
			{
				if (layout.Dashboard != CurrentDashboard) continue;
				FrameworkElement widget = CreateWidget(layout.Type);
				if (widget == null) continue;
				int row = layout.Row; int col = layout.Column;
				int rowSpan = layout.RowSpan <= 0 ? 1 : layout.RowSpan;
				int colSpan = layout.ColumnSpan <= 0 ? 1 : layout.ColumnSpan;
				EnsureRow(row + rowSpan);
				bool isOverlap = false;
				for (int r = row; r < row + rowSpan; r++)
				{
					for (int c = col; c < col + colSpan; c++)
					{
						if (usedCells.Contains($"{r}-{c}")) { isOverlap = true; break; }
					}
					if (isOverlap) break;
				}
				if (isOverlap) continue;
				SetupWidget(widget);
				Grid.SetRow(widget, row); Grid.SetColumn(widget, col);
				Grid.SetRowSpan(widget, rowSpan); Grid.SetColumnSpan(widget, colSpan);
				Panel.SetZIndex(widget, DashboardGrid.Children.Count);
				DashboardGrid.Children.Add(widget);
				for (int r = row; r < row + rowSpan; r++)
				{
					for (int c = col; c < col + colSpan; c++) usedCells.Add($"{r}-{c}");
				}
			}
            _isDirty = false; // Reset sau khi load layout mới
		}

		void InitGrid()
		{
			GridOverlay.RowDefinitions.Clear(); GridOverlay.ColumnDefinitions.Clear();
			DashboardGrid.RowDefinitions.Clear(); DashboardGrid.ColumnDefinitions.Clear();
			for (int i = 0; i < 12; i++)
			{
				GridOverlay.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				DashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			}
			for (int i = 0; i < 10; i++)
			{
				GridOverlay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
				DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
			}
			CreateGrid();
		}

		void CreateGrid()
		{
			GridOverlay.Children.Clear();
			int rowCount = GridOverlay.RowDefinitions.Count; int colCount = GridOverlay.ColumnDefinitions.Count;
			for (int r = 0; r < rowCount; r++)
			{
				for (int c = 0; c < colCount; c++)
				{
					Border cell = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)), BorderThickness = new Thickness(0.5), Background = Brushes.Transparent };
					Grid.SetRow(cell, r); Grid.SetColumn(cell, c);
					GridOverlay.Children.Add(cell);
				}
			}
		}

		FrameworkElement CreateWidget(string typeName)
		{
			var widgetType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == typeName);
			if (widgetType == null) return null;
			return Activator.CreateInstance(widgetType) as FrameworkElement;
		}

		void SetupWidget(FrameworkElement widget)
		{
			widget.Margin = new Thickness(1, 1, 5, 5);
			widget.HorizontalAlignment = HorizontalAlignment.Stretch;
			widget.VerticalAlignment = VerticalAlignment.Stretch;
            
            // Đảm bảo widget có Background để bắt sự kiện chuột (WPF yêu cầu Background != null)
            if (widget is Control ctrl && ctrl.Background == null) ctrl.Background = Brushes.Transparent;
            else if (widget is Panel panel && panel.Background == null) panel.Background = Brushes.Transparent;

			widget.Loaded += (s, e) =>
			{
				Thumb thumb = null;
				if (widget is IResizableWidget resizable) thumb = resizable.ResizeThumb;
				if (thumb != null)
				{
					if (widget is IDashboardWidget dw) dw.SetEditMode(editMode);
					thumb.DragDelta -= Thumb_DragDelta; thumb.DragDelta += Thumb_DragDelta;
				}
			};

            // Gỡ bỏ sự kiện cũ trước khi gán mới để tránh trùng lặp
			widget.MouseLeftButtonDown -= Widget_MouseLeftButtonDown;
			widget.MouseMove -= Widget_MouseMove;
			widget.MouseLeftButtonUp -= Widget_MouseLeftButtonUp;

			widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown;
			widget.MouseMove += Widget_MouseMove;
			widget.MouseLeftButtonUp += Widget_MouseLeftButtonUp;

            if (widget is IDashboardWidget dashboardWidget)
            {
                dashboardWidget.DeleteRequested -= Widget_DeleteRequested;
                dashboardWidget.DeleteRequested += Widget_DeleteRequested;
                dashboardWidget.SetEditMode(editMode);
            }
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (IsSidebarCollapsed) SidebarColumn.Width = new GridLength(220);
            else SidebarColumn.Width = new GridLength(40);
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!editMode) return;
			Thumb thumb = sender as Thumb;
			FrameworkElement widget = FindParentWidget(thumb);
			if (widget != null)
			{
				double cellWidth = DashboardGrid.ActualWidth / 12; double cellHeight = 80;
				int targetColSpan = (int)Math.Max(1, Math.Round((widget.ActualWidth + e.HorizontalChange) / cellWidth));
				int targetRowSpan = (int)Math.Max(1, Math.Round((widget.ActualHeight + e.VerticalChange) / cellHeight));
				int finalColSpan = targetColSpan; int finalRowSpan = targetRowSpan;
				int minCol = 1; int minRow = 1;
				if (widget is IResizableWidget resizable) { minCol = resizable.MinCol; minRow = resizable.MinRow; finalColSpan = Math.Max(minCol, targetColSpan); finalRowSpan = Math.Max(minRow, targetRowSpan); }
				Grid.SetColumnSpan(widget, finalColSpan); Grid.SetRowSpan(widget, finalRowSpan);
				widget.Tag = $"{finalColSpan}x{finalRowSpan}";
				if (widget is CameraListWidget listWidget) listWidget.UpdateTable(listWidget._currentPage.ToString());
                
                SmartCascadePush(widget); // Cập nhật layout ngay khi resize
                _isDirty = true;
			}
		}

		private FrameworkElement FindParentWidget(DependencyObject child)
		{
			DependencyObject parentObject = VisualTreeHelper.GetParent(child);
			if (parentObject == null) return null;
			if (parentObject is UserControl || (parentObject is FrameworkElement fe && DashboardGrid.Children.Contains(fe))) return parentObject as FrameworkElement;
			return FindParentWidget(parentObject);
		}

		bool IsOverlap(FrameworkElement a, FrameworkElement b)
		{
			int r1 = Grid.GetRow(a); int c1 = Grid.GetColumn(a); int rs1 = Grid.GetRowSpan(a); int cs1 = Grid.GetColumnSpan(a);
			int r2 = Grid.GetRow(b); int c2 = Grid.GetColumn(b); int rs2 = Grid.GetRowSpan(b); int cs2 = Grid.GetColumnSpan(b);
			return r1 < r2 + rs2 && r1 + rs1 > r2 && c1 < c2 + cs2 && c1 + cs1 > c2;
		}

        private bool HorizontalOverlap(FrameworkElement a, FrameworkElement b)
        {
            int c1 = Grid.GetColumn(a); int cs1 = Grid.GetColumnSpan(a);
            int c2 = Grid.GetColumn(b); int cs2 = Grid.GetColumnSpan(b);
            return c1 < c2 + cs2 && c1 + cs1 > c2;
        }

        void SmartCascadePush(FrameworkElement movedWidget)
        {
            var widgets = DashboardGrid.Children.OfType<FrameworkElement>().ToList();
            if (widgets.Count <= 1) return;

            // Widget vừa di chuyển/thả là vật neo (Anchor) - giữ nguyên vị trí người dùng chọn
            var anchor = movedWidget;
            var placed = new List<FrameworkElement> { anchor };

            // Sắp xếp các widget còn lại theo Row (ưu tiên giữ thứ tự từ trên xuống dưới)
            var orderedWidgets = widgets
                .Where(w => w != anchor)
                .OrderBy(w => Grid.GetRow(w))
                .ThenBy(w => Grid.GetColumn(w))
                .ToList();

            foreach (var widget in orderedWidgets)
            {
                int rowSpan = Grid.GetRowSpan(widget);
                int col = Grid.GetColumn(widget);
                int colSpan = Grid.GetColumnSpan(widget);
                
                // Thử đặt ở vị trí cao nhất có thể (Row 0) và tăng dần cho đến khi không còn va chạm
                int candidateRow = 0;
                
                bool collision;
                do
                {
                    collision = false;
                    foreach (var other in placed)
                    {
                        // Kiểm tra va chạm hình chữ nhật giữa 'widget' (tại candidateRow) và 'other'
                        if (IsRectOverlap(candidateRow, col, rowSpan, colSpan,
                                         Grid.GetRow(other), Grid.GetColumn(other), 
                                         Grid.GetRowSpan(other), Grid.GetColumnSpan(other)))
                        {
                            // Nếu va chạm, đẩy candidateRow xuống dưới hẳn vật cản này
                            candidateRow = Grid.GetRow(other) + Grid.GetRowSpan(other);
                            collision = true;
                            break; // Kiểm tra lại từ đầu với candidateRow mới đối với tất cả placed widgets
                        }
                    }
                } while (collision);

                Grid.SetRow(widget, candidateRow);
                placed.Add(widget);
            }

            // Đảm bảo Grid đủ hàng để hiển thị
            int maxRow = widgets.Max(w => Grid.GetRow(w) + Grid.GetRowSpan(w));
            EnsureRow(maxRow);
        }

        // Hàm kiểm tra va chạm giữa hai hình chữ nhật trong Grid
        private bool IsRectOverlap(int r1, int c1, int rs1, int cs1, int r2, int c2, int rs2, int cs2)
        {
            return r1 < r2 + rs2 && r1 + rs1 > r2 && c1 < c2 + cs2 && c1 + cs1 > c2;
        }

		void AddMoreRows(int count)
		{
			for (int i = 0; i < count; i++)
			{
				DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
				GridOverlay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
			}
            CreateGrid();
        }

        private void DashboardScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null) { scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta); e.Handled = true; }
        }

        private void DashboardScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!editMode) return;
            ScrollViewer sv = sender as ScrollViewer;
			if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 5) AddMoreRows(7);
		}

		private void WorkspaceBtn_Click(object sender, RoutedEventArgs e)
		{
			var win = new LightInsight.Dashboard.Dashboard.Workspace.WorkspaceWindow();
			win.ShowDialog();
		}

		private void LoadSidebar()
		{
			var root = FindById(WorkspaceService.Instance.Workspaces, "ROOT_DASHBOARD");
			if (root != null) { DashboardMenus = root.Children; DataContext = this; }
		}

		private WorkspaceModel FindById(IEnumerable<WorkspaceModel> list, string id)
		{
			foreach (var item in list) { if (item.Id == id) return item; var found = FindById(item.Children, id); if (found != null) return found; }
			return null;
		}

		private object OnThemeChanged(Message message, FQID dest, FQID sender)
		{
			var theme = message?.Data as Theme; ApplySmartClientTheme(theme); return null;
		}

		private void ApplySmartClientTheme(Theme scTheme)
		{
			Dispatcher.Invoke(() =>
			{
				var themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Dark.xaml";
				var crTheme = ClientControl.Instance.Theme.ThemeType;
				if (crTheme == ThemeType.Light) themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";
				var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };
				if (_currentThemeDictionary != null) Resources.MergedDictionaries.Remove(_currentThemeDictionary);
				Resources.MergedDictionaries.Add(newDict); _currentThemeDictionary = newDict;
			});
		}

		private void ApplySmartClientLanguage(string name)
		{
			var uri = name == "vi-VN" ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml" : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml";
            var newDict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
            if (_currentLanguageDictionary != null) Resources.MergedDictionaries.Remove(_currentLanguageDictionary);
            Resources.MergedDictionaries.Add(newDict); _currentLanguageDictionary = newDict;
        }

        private bool ConfirmBeforeLeave()
        {
            if (!editMode || !_isDirty) return true;
			var result = MessageBox.Show("Bạn có thay đổi chưa lưu. Bạn muốn lưu không?", "Cảnh báo", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
			if (result == MessageBoxResult.Yes)
			{
				List<WidgetLayout> layouts = new List<WidgetLayout>();
				foreach (UIElement child in DashboardGrid.Children)
				{
					if (child is FrameworkElement widget)
					{
						WidgetLayout layout = new WidgetLayout { Dashboard = CurrentDashboard, Type = widget.GetType().Name, Row = Grid.GetRow(widget), Column = Grid.GetColumn(widget), RowSpan = Grid.GetRowSpan(widget), ColumnSpan = Grid.GetColumnSpan(widget) };
						layouts.Add(layout);
					}
				}
				SaveLayout(layouts); ExitEditMode(); return true;
			}
			else if (result == MessageBoxResult.No) return true;
			else return false;
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			if (!ConfirmBeforeLeave()) return;
		}

		bool IsWidgetAdded(Type widgetType)
		{
			return DashboardGrid.Children.OfType<FrameworkElement>().Any(x => x.GetType() == widgetType);
		}

		void SetFilePermissionForAllUsers(string filePath)
		{
			try
			{
				var fileInfo = new FileInfo(filePath); var fileSecurity = fileInfo.GetAccessControl();
				var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
				var rule = new FileSystemAccessRule(users, FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
				fileSecurity.AddAccessRule(rule); fileInfo.SetAccessControl(fileSecurity);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Set permission failed: " + ex.Message); }
        }

        void EnsureDashboardFile()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LightInsight");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, "dashboard_layout.json");
            if (!File.Exists(filePath))
            {
                string defaultJson = @"[
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""CameraOnlineWidget"", ""Row"": 0, ""Column"": 0, ""RowSpan"": 2, ""ColumnSpan"": 2 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""CameraOfflineWidget"", ""Row"": 0, ""Column"": 2, ""RowSpan"": 2, ""ColumnSpan"": 2 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""TotalCameraCount"", ""Row"": 0, ""Column"": 4, ""RowSpan"": 2, ""ColumnSpan"": 2 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""CameraListWidget"", ""Row"": 5, ""Column"": 0, ""RowSpan"": 4, ""ColumnSpan"": 12 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""CameraHealthScoreWidget"", ""Row"": 0, ""Column"": 6, ""RowSpan"": 2, ""ColumnSpan"": 2 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""StorageUsageWidget"", ""Row"": 0, ""Column"": 8, ""RowSpan"": 3, ""ColumnSpan"": 4 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""TotalServersWidget"", ""Row"": 2, ""Column"": 0, ""RowSpan"": 2, ""ColumnSpan"": 2 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""AlarmSLABreachWidget"", ""Row"": 2, ""Column"": 2, ""RowSpan"": 2, ""ColumnSpan"": 2 },
                                          { ""Dashboard"": ""Default Workspace"", ""Type"": ""AlarmBySeverityWidget"", ""Row"": 2, ""Column"": 4, ""RowSpan"": 3, ""ColumnSpan"": 3 }
                                        ]";
                File.WriteAllText(filePath, defaultJson); SetFilePermissionForAllUsers(filePath);
            }
        }
    }
}
