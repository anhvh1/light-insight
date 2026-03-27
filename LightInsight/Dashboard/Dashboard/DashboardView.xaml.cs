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
using MahApps.Metro.IconPacks;


namespace LightInsight.Dashboard.Dashboard
{
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

        public static readonly DependencyProperty IsSidebarCollapsedProperty =
            DependencyProperty.Register("IsSidebarCollapsed", typeof(bool), typeof(DashboardView), new PropertyMetadata(false));

        public bool IsSidebarCollapsed
        {
            get { return (bool)GetValue(IsSidebarCollapsedProperty); }
            set { SetValue(IsSidebarCollapsedProperty, value); }
        }

        FrameworkElement selectedWidget = null;
        bool isDraggingWidget = false;
        private Point _clickOffset;
        public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

        private ObservableCollection<WorkspaceModel> _navigationItems = new ObservableCollection<WorkspaceModel>();
        public ObservableCollection<WorkspaceModel> NavigationItems
        {
            get => _navigationItems;
            set { _navigationItems = value; OnPropertyChanged(nameof(NavigationItems)); }
        }

        #region MENU LOGIC
        private void LoadUnifiedMenu()
        {
            var allWorkspaces = WorkspaceService.Instance.Workspaces;
            var systemMenus = GetStaticSystemMenus();
            var systemTree = BuildTree(systemMenus); // Build tree ONLY for system menus
            
            var combined = allWorkspaces.Concat(systemTree).ToList();
            NavigationItems = new ObservableCollection<WorkspaceModel>(combined);
        }

        private List<WorkspaceModel> BuildTree(List<WorkspaceModel> flatList)
        {
            foreach (var item in flatList) item.Children.Clear();
            var itemMap = flatList.ToDictionary(x => x.Id);
            var rootNodes = new List<WorkspaceModel>();

            foreach (var item in flatList)
            {
                if (string.IsNullOrEmpty(item.ParentId) || !itemMap.ContainsKey(item.ParentId))
                {
                    rootNodes.Add(item);
                }
                else
                {
                    if (itemMap.ContainsKey(item.ParentId))
                    {
                        var parent = itemMap[item.ParentId];
                        parent.Children.Add(item);
                        parent.IsGroup = true;
                    }
                    else
                    {
                        rootNodes.Add(item);
                    }
                }
            }
            return rootNodes;
        }

        private List<WorkspaceModel> GetStaticSystemMenus()
        {
            var list = new List<WorkspaceModel>();

            // 1. Camera
            list.Add(new WorkspaceModel { Id = "sys-cam", Name = "Camera", Icon = PackIconMaterialKind.Camera, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "cam-inv", Name = "Inventory", Icon = PackIconMaterialKind.FormatListBulleted, ParentId = "sys-cam", Path = "/camera/inventory", Type = null });
            list.Add(new WorkspaceModel { Id = "cam-store", Name = "Storage", Icon = PackIconMaterialKind.Database, ParentId = "sys-cam", Path = "/camera/storage", Type = null });
            list.Add(new WorkspaceModel { Id = "cam-health", Name = "Health", Icon = PackIconMaterialKind.HeartPulse, ParentId = "sys-cam", Path = "/camera/health", Type = null });

            // 2. Recording Server
            list.Add(new WorkspaceModel { Id = "sys-srv", Name = "Recording Server", Icon = PackIconMaterialKind.Server, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "srv-det", Name = "Details", Icon = PackIconMaterialKind.Information, ParentId = "sys-srv", Path = "/recording-server/details", Type = null });
            list.Add(new WorkspaceModel { Id = "srv-store", Name = "Storage", Icon = PackIconMaterialKind.DatabaseOutline, ParentId = "sys-srv", Path = "/recording-server/storage", Type = null });
            list.Add(new WorkspaceModel { Id = "srv-health", Name = "Health", Icon = PackIconMaterialKind.HeartPulse, ParentId = "sys-srv", Path = "/recording-server/health", Type = null });

            // 3. Monitoring
            list.Add(new WorkspaceModel { Id = "sys-mon", Name = "Monitoring", Icon = PackIconMaterialKind.Monitor, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "mon-live", Name = "Live View", Icon = PackIconMaterialKind.PlayCircle, ParentId = "sys-mon", Path = "/monitoring/live", Type = null });
            list.Add(new WorkspaceModel { Id = "mon-play", Name = "Playback", Icon = PackIconMaterialKind.History, ParentId = "sys-mon", Path = "/monitoring/playback", Type = null });

            // 4. Alarm Details
            list.Add(new WorkspaceModel { Id = "sys-alm", Name = "Alarm Details", Icon = PackIconMaterialKind.Bell, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "alm-det", Name = "Details", Icon = PackIconMaterialKind.InformationOutline, ParentId = "sys-alm", Path = "/alarms/details", Type = null });
            list.Add(new WorkspaceModel { Id = "alm-day", Name = "Daily Count", Icon = PackIconMaterialKind.ChartBar, ParentId = "sys-alm", Path = "/alarms/daily-count", Type = null });
            list.Add(new WorkspaceModel { Id = "alm-src", Name = "Daily Count by Source", Icon = PackIconMaterialKind.SourceBranch, ParentId = "sys-alm", Path = "/alarms/source-count", Type = null });

            // 5. Trends
            list.Add(new WorkspaceModel { Id = "sys-trd", Name = "Trends", Icon = PackIconMaterialKind.TrendingUp, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "trd-alm", Name = "Alarms", Icon = PackIconMaterialKind.BellAlert, ParentId = "sys-trd", Path = "/trends/alarms", Type = null });
            list.Add(new WorkspaceModel { Id = "trd-cam", Name = "Cameras", Icon = PackIconMaterialKind.Camera, ParentId = "sys-trd", Path = "/trends/cameras", Type = null });

            // 6. Access Control
            list.Add(new WorkspaceModel { Id = "sys-acs", Name = "Access Control", Icon = PackIconMaterialKind.Lock, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "acs-stat", Name = "Item Status", Icon = PackIconMaterialKind.ListStatus, ParentId = "sys-acs", Path = "/acs/status", Type = null });
            list.Add(new WorkspaceModel { Id = "acs-alm", Name = "ACS Alarms", Icon = PackIconMaterialKind.ShieldAlert, ParentId = "sys-acs", Path = "/acs/alarms", Type = null });
            list.Add(new WorkspaceModel { Id = "acs-evt", Name = "ACS Events", Icon = PackIconMaterialKind.CalendarText, ParentId = "sys-acs", Path = "/acs/events", Type = null });
            list.Add(new WorkspaceModel { Id = "acs-card", Name = "Cardholders", Icon = PackIconMaterialKind.AccountGroup, ParentId = "sys-acs", Path = "/acs/cardholders", Type = null });

            // 7. IoT
            list.Add(new WorkspaceModel { Id = "sys-iot", Name = "IoT", Icon = PackIconMaterialKind.AccessPoint, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "iot-sen", Name = "Sensors", Icon = PackIconMaterialKind.Leak, ParentId = "sys-iot", Path = "/iot/sensors", Type = null });
            list.Add(new WorkspaceModel { Id = "iot-dash", Name = "IoT Dashboard", Icon = PackIconMaterialKind.ViewDashboard, ParentId = "sys-iot", Path = "/iot/dashboard", Type = null });
            list.Add(new WorkspaceModel { Id = "iot-alt", Name = "Alerts", Icon = PackIconMaterialKind.AlertDecagram, ParentId = "sys-iot", Path = "/iot/alerts", Type = null });

            // 8. Reporting
            list.Add(new WorkspaceModel { Id = "sys-rep", Name = "Reporting", Icon = PackIconMaterialKind.FileDocument, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "rep-list", Name = "Reports", Icon = PackIconMaterialKind.FileChart, ParentId = "sys-rep", Path = "/reporting/reports", Type = null });
            list.Add(new WorkspaceModel { Id = "rep-sch", Name = "Scheduled", Icon = PackIconMaterialKind.Clock, ParentId = "sys-rep", Path = "/reporting/scheduled", Type = null });

            // 9. Notifications
            list.Add(new WorkspaceModel { Id = "sys-not", Name = "Notifications", Icon = PackIconMaterialKind.BellRing, Path = "/notifications", Type = null });

            // 10. Settings
            list.Add(new WorkspaceModel { Id = "sys-set", Name = "Settings", Icon = PackIconMaterialKind.Cog, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "set-br", Name = "Branding", Icon = PackIconMaterialKind.Palette, ParentId = "sys-set", Path = "/settings/branding", Type = null });
            list.Add(new WorkspaceModel { Id = "set-not", Name = "Notification", Icon = PackIconMaterialKind.EmailOutline, ParentId = "sys-set", Path = "/settings/notification", Type = null });
            list.Add(new WorkspaceModel { Id = "set-wea", Name = "Weather", Icon = PackIconMaterialKind.WeatherPartlyCloudy, ParentId = "sys-set", Path = "/settings/weather", Type = null });
            list.Add(new WorkspaceModel { Id = "set-iot", Name = "IoT", Icon = PackIconMaterialKind.AccessPointNetwork, ParentId = "sys-set", Path = "/settings/iot", Type = null });
            list.Add(new WorkspaceModel { Id = "set-rep", Name = "Reporting", Icon = PackIconMaterialKind.FileTable, ParentId = "sys-set", Path = "/settings/reporting", Type = null });
            list.Add(new WorkspaceModel { Id = "set-rol", Name = "Roles", Icon = PackIconMaterialKind.AccountKey, ParentId = "sys-set", Path = "/settings/roles", Type = null });
            list.Add(new WorkspaceModel { Id = "set-lic", Name = "License Info", Icon = PackIconMaterialKind.Key, ParentId = "sys-set", Path = "/settings/license", Type = null });
            list.Add(new WorkspaceModel { Id = "set-db", Name = "Database Statistics", Icon = PackIconMaterialKind.DatabaseSettings, ParentId = "sys-set", Path = "/settings/database", Type = null });

            // 11. Help
            list.Add(new WorkspaceModel { Id = "sys-hlp", Name = "Help", Icon = PackIconMaterialKind.HelpCircle, IsGroup = true, Type = null });
            list.Add(new WorkspaceModel { Id = "hlp-ws", Name = "Workspace", Icon = PackIconMaterialKind.Briefcase, ParentId = "sys-hlp", Path = "/help/workspace", Type = null });
            list.Add(new WorkspaceModel { Id = "hlp-rep", Name = "Reporting", Icon = PackIconMaterialKind.Information, ParentId = "sys-hlp", Path = "/help/reporting", Type = null });

            return list;
        }

        private void RefreshMenuLocalization()
        {
            if (NavigationItems != null) foreach (var item in NavigationItems) item.RefreshLocalization();
        }
        #endregion

        #region DASHBOARD ENGINE
		private ObservableCollection<WidgetGroup> _widgetGroups = new ObservableCollection<WidgetGroup>();
		public ObservableCollection<WidgetGroup> WidgetGroups
		{
			get => _widgetGroups;
			set { _widgetGroups = value; OnPropertyChanged(nameof(WidgetGroups)); }
		}

		List<WidgetItem> allWidgets = new List<WidgetItem>()
		{
			new WidgetItem{ Name="Camera Online Count", Category="Camera", Description="Cameras online", WidgetType = typeof(CameraOnlineWidget), Icon = PackIconMaterialKind.Camera},
			new WidgetItem{ Name="Camera Offline Count", Category="Camera", Description="Cameras offline", WidgetType = typeof(CameraOfflineWidget), Icon = PackIconMaterialKind.CameraOff},
			new WidgetItem{ Name="Camera Total Count", Category="Camera", Description="Total cameras", WidgetType = typeof(TotalCameraCount), Icon = PackIconMaterialKind.Video},
			new WidgetItem{ Name="Camera Online + Offline", Category="Camera", Description="Online and offline summary", WidgetType = typeof(CameraOnlineNOffline), Icon = PackIconMaterialKind.VideoOutline},
			new WidgetItem{ Name="Camera Status Donut", Category="Camera", Description="Camera status distribution", WidgetType = typeof(CameraStatusDonut), Icon = PackIconMaterialKind.ChartDonut},
			new WidgetItem{ Name="Camera Offline Duration top 10", Category="Camera", Description="Top 10 longest disconnections", WidgetType = typeof(CameraOfflineDurationTop10), Icon = PackIconMaterialKind.Table},
			new WidgetItem{ Name="Camera Disconnection Trend", Category="Camera", Description="Trend of disconnected cameras", WidgetType = typeof(CameraDisconnectionTrend), Icon = PackIconMaterialKind.ChartLine},
			new WidgetItem{ Name="Camera Analytics Summary", Category="Camera", Description="Analytics events summary", WidgetType = typeof(CameraAnalyticsSummaryWidget), Icon = PackIconMaterialKind.GoogleAnalytics},
			new WidgetItem{ Name="Camera List", Category="Camera", Description="Detailed camera inventory", WidgetType = typeof(CameraListWidget), Icon = PackIconMaterialKind.FormatListBulleted},
			new WidgetItem{ Name="Camera Health Score", Category="Camera", Description="Health metrics overview", WidgetType = typeof(CameraHealthScoreWidget), Icon = PackIconMaterialKind.HeartPulse},
			new WidgetItem{ Name="Live Alarm Feed", Category="Alarms & Events", Description="Recent alarm activity", WidgetType = typeof(LiveAlarmsFeedWidget), Icon = PackIconMaterialKind.BellRing},
			new WidgetItem{ Name="Alarm by Severity", Category="Alarms & Events", Description="Alarms grouped by severity", WidgetType = typeof(AlarmBySeverityWidget), Icon = PackIconMaterialKind.ChartPie},
			new WidgetItem{ Name="Alarm Daily Count", Category="Alarms & Events", Description="Alarm trend by day", WidgetType = typeof(AlarmDailyCountWidget), Icon = PackIconMaterialKind.ChartBar},
			new WidgetItem{ Name="Alarm by Source", Category="Alarms & Events", Description="Alarms by source device", WidgetType = typeof(AlarmBySourceWidget), Icon = PackIconMaterialKind.SourceBranch},
			new WidgetItem{ Name="Alarm by Type", Category="Alarms & Events", Description="Alarms by category", WidgetType = typeof(AlarmByTypeWidget), Icon = PackIconMaterialKind.Shape},
			new WidgetItem{ Name="Alarm SLA Breach", Category="Alarms & Events", Description="SLA compliance status", WidgetType = typeof(AlarmSLABreachWidget), Icon = PackIconMaterialKind.ClockAlert},
			new WidgetItem{ Name="Event Trend Chart", Category="Alarms & Events", Description="Events frequency trend", WidgetType = typeof(EventTrendChartWidget), Icon = PackIconMaterialKind.ChartTimelineVariant},
            new WidgetItem{ Name="Servers Online Count", Category="Recording Server", Description="Servers online", WidgetType = typeof(ServersOnlineCountWidget), Icon = PackIconMaterialKind.Server},
            new WidgetItem{ Name="Servers Offline Count", Category="Recording Server", Description="Servers offline", WidgetType = typeof(ServersOfflineCountWidget), Icon = PackIconMaterialKind.ServerOff},
            new WidgetItem{ Name="Servers Total", Category="Recording Server", Description="Total recording servers", WidgetType = typeof(TotalServersWidget), Icon = PackIconMaterialKind.ServerNetwork},
            new WidgetItem{ Name="Storage Usage by Server", Category="Recording Server", Description="Storage utilization", WidgetType = typeof(StorageUsageWidget), Icon = PackIconMaterialKind.Database},
		};

        public DashboardView()
        {
            InitializeComponent();
            this.DataContext = this;
            TimeRangeCombo.SelectedIndex = 0;
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            LocalizeWidgetNames();
            
            WorkspaceService.Instance.OnWorkspaceChanged += () =>
            {
                Application.Current.Dispatcher.Invoke(() => { LoadUnifiedMenu(); });
            };
            
            ApplySmartClientTheme(ClientControl.Instance?.Theme);
            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            
            UpdateWidgetGroups();
            EnsureDashboardFile();
            LoadUnifiedMenu();

            var firstDb = NavigationItems.FirstOrDefault(x => x.Id == "ROOT_DASHBOARD")?.Children.FirstOrDefault();
            if (firstDb != null) { firstDb.IsSelected = true; CurrentDashboard = firstDb.Name; BreadcrumbText.Text = $"Dashboard > {firstDb.DisplayTitle}"; }
            
            LoadLayout();
        }

        private void LocalizeWidgetNames()
        {
            string GetText(string key, string fallback) { return TryFindResource(key) as string ?? fallback; }
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
					if (int.TryParse(parts[0], out int cols) && int.TryParse(parts[1], out int rows))
						return (cols, rows);
				}
			}
			return (2, 2);
		}

		private void AddWidget_Click(object sender, RoutedEventArgs e)
		{
			Button btn = sender as Button; WidgetItem widget = btn.Tag as WidgetItem;
			if (widget == null) return;
			FrameworkElement newWidget = Activator.CreateInstance(widget.WidgetType) as FrameworkElement;
			if (newWidget == null) return;
			if (IsWidgetAdded(widget.WidgetType)) { MessageBox.Show("Widget này đã tồn tại!"); return; }
			SetupWidget(newWidget);
			var span = CalculateWidgetSpan(newWidget);
			var pos = FindFreePosition(span.rowSpan, span.colSpan);
			EnsureRow(pos.Row + span.rowSpan);
			Grid.SetRow(newWidget, pos.Row); Grid.SetColumn(newWidget, pos.Column);
			Grid.SetColumnSpan(newWidget, span.colSpan); Grid.SetRowSpan(newWidget, span.rowSpan);
			DashboardGrid.Children.Add(newWidget); _isDirty = true;
		}

		private void ExitEditModeBtn_Click(object sender, RoutedEventArgs e) { ExitEditMode(); }

		private void ExitEditMode()
		{
			WidgetLibraryColumn.Width = new GridLength(0); GridOverlay.Visibility = Visibility.Collapsed; WidgetLibrary.Visibility = Visibility.Collapsed;
			EditLayoutBtn.Visibility = Visibility.Visible; SaveBtn.Visibility = Visibility.Collapsed; CancelBtn.Visibility = Visibility.Collapsed;
			foreach (var widget in DashboardGrid.Children.OfType<IDashboardWidget>()) { widget.SetEditMode(false); }
			TrimEmptyRows(); editMode = false; GridOverlay.Children.Clear();
		}

		private (int Row, int Column) FindFreePosition(int rowSpan, int colSpan)
		{
			int maxCols = 12; HashSet<string> used = new HashSet<string>();
			foreach (UIElement child in DashboardGrid.Children)
			{
				int r = Grid.GetRow(child); int c = Grid.GetColumn(child);
				int rs = Grid.GetRowSpan(child); int cs = Grid.GetColumnSpan(child);
				for (int i = r; i < r + rs; i++) for (int j = c; j < c + cs; j++) used.Add($"{i}-{j}");
			}
			for (int row = 0; row < 100; row++)
			{
				for (int col = 0; col <= maxCols - colSpan; col++)
				{
					bool free = true;
					for (int r = row; r < row + rowSpan; r++) for (int c = col; c < col + colSpan; c++) if (used.Contains($"{r}-{c}")) { free = false; break; }
					if (free) return (row, col);
				}
			}
			return (0, 0);
		}

        private void UpdateWidgetGroups(string filter = "")
        {
            var query = allWidgets.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(filter)) query = query.Where(x => x.Name.ToLower().Contains(filter.ToLower()));
            var grouped = query.GroupBy(x => x.Category).Select(g => new WidgetGroup { Title = g.Key, Items = new ObservableCollection<WidgetItem>(g.ToList()) }).OrderBy(x => x.Title).ToList();
            WidgetGroups = new ObservableCollection<WidgetGroup>(grouped);
        }

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { UpdateWidgetGroups(SearchBox.Text); }

		private void RefreshBtn_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Refreshing..."); }

		private void WorkspaceBtn_Click(object sender, RoutedEventArgs e) { var win = new LightInsight.Dashboard.Dashboard.Workspace.WorkspaceWindow(); win.ShowDialog(); }

		private void Menu_Click(object sender, RoutedEventArgs e)
		{
			var item = (sender as FrameworkElement)?.DataContext as WorkspaceModel;
			if (item == null) return;
            if (item.IsGroup) { item.IsExpanded = !item.IsExpanded; return; }
            DeselectAll(NavigationItems); item.IsSelected = true;
            if (item.Type != null) { OpenDashboardByName(item.Name); }
            else { BreadcrumbText.Text = $"System > {item.DisplayTitle}"; }
        }

        private void DeselectAll(IEnumerable<WorkspaceModel> items)
        {
            foreach (var i in items) { i.IsSelected = false; if (i.Children != null) DeselectAll(i.Children); }
        }

		private void OpenDashboardByName(string name)
		{
			if (!ConfirmBeforeLeave()) return;
			CurrentDashboard = name; var localized = Application.Current.TryFindResource(name) as string;
			BreadcrumbText.Text = $"Dashboard > {localized ?? name}"; LoadLayout();
		}

		private void EditLayoutBtn_Click(object sender, RoutedEventArgs e)
		{
			editMode = true; _isDirty = false; InitGrid(); CreateGrid();
			GridOverlay.Visibility = Visibility.Visible; WidgetLibraryColumn.Width = new GridLength(320); WidgetLibrary.Visibility = Visibility.Visible;
			EditLayoutBtn.Visibility = Visibility.Collapsed; SaveBtn.Visibility = Visibility.Visible; CancelBtn.Visibility = Visibility.Visible;
			foreach (var widget in DashboardGrid.Children.OfType<FrameworkElement>()) { SetupWidget(widget); if (widget is IDashboardWidget dw) dw.SetEditMode(true); }
		}

		private void SaveBtn_Click(object sender, RoutedEventArgs e)
		{
			List<WidgetLayout> layouts = new List<WidgetLayout>();
			foreach (UIElement child in DashboardGrid.Children)
			{
				if (child is FrameworkElement widget) layouts.Add(new WidgetLayout { Dashboard = CurrentDashboard, Type = widget.GetType().Name, Row = Grid.GetRow(widget), Column = Grid.GetColumn(widget), RowSpan = Grid.GetRowSpan(widget), ColumnSpan = Grid.GetColumnSpan(widget) });
			}
			SaveLayout(layouts); ExitEditMode();
		}

		private void CancelBtn_Click(object sender, RoutedEventArgs e) { LoadLayout(); ExitEditMode(); }

        private void EnsureRow(int rowIndex)
        {
            bool added = false;

            while (DashboardGrid.RowDefinitions.Count <= rowIndex)
            {
                DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
                GridOverlay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
                added = true;
            }

            // 🔥 FIX: nếu có thêm row thì rebuild grid overlay
            if (added)
            {
                CreateGrid();
            }
        }

        private void TrimEmptyRows()
		{
		    int maxRowUsed = 0;
		    foreach (FrameworkElement child in DashboardGrid.Children) { int row = Grid.GetRow(child); int rowSpan = Grid.GetRowSpan(child); if (row + rowSpan > maxRowUsed) maxRowUsed = row + rowSpan; }
		    int targetRows = Math.Max(maxRowUsed, 10);
		    while (DashboardGrid.RowDefinitions.Count > targetRows) DashboardGrid.RowDefinitions.RemoveAt(DashboardGrid.RowDefinitions.Count - 1);
		    while (GridOverlay.RowDefinitions.Count > targetRows) GridOverlay.RowDefinitions.RemoveAt(GridOverlay.RowDefinitions.Count - 1);
		}

        private void DashboardGrid_DragOver(object sender, DragEventArgs e) 
        {
            if (!e.Data.GetDataPresent(typeof(WidgetItem)) && !e.Data.GetDataPresent("WidgetItem"))
                return;

            var widgetItem = e.Data.GetData(typeof(WidgetItem)) as WidgetItem
                          ?? e.Data.GetData("WidgetItem") as WidgetItem;
        }

        private void Widget_MouseMove(object sender, MouseEventArgs e)
        {
            if (!editMode || !isDraggingWidget || selectedWidget == null) return;

            // 🔥 LẤY POSITION THEO SCROLLVIEWER (FIX BUG NGƯỢC CHIỀU)
            Point currentPoint = e.GetPosition(DashboardScroll);

            double cellWidth = DashboardGrid.ActualWidth / 12;
            double cellHeight = 80;

            // position theo grid (để set row/column)
            Point gridPoint = e.GetPosition(DashboardGrid);

            int column = (int)Math.Round((gridPoint.X - _clickOffset.X) / cellWidth);
            int row = (int)Math.Round((gridPoint.Y - _clickOffset.Y) / cellHeight);

            int colSpan = Grid.GetColumnSpan(selectedWidget);
            int rowSpan = Grid.GetRowSpan(selectedWidget);

            // =============================
            // 🔥 CLAMP COLUMN
            // =============================
            column = Math.Max(0, Math.Min(column, 12 - colSpan));

            // =============================
            // 🔥 AUTO SCROLL (MƯỢT)
            // =============================
            double threshold = 60;

            // kéo xuống
            if (currentPoint.Y > DashboardScroll.ViewportHeight - threshold)
            {
                double speed = (currentPoint.Y - (DashboardScroll.ViewportHeight - threshold)) / 5;
                DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset + speed);
            }
            // kéo lên
            else if (currentPoint.Y < threshold)
            {
                double speed = (threshold - currentPoint.Y) / 5;
                DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset - speed);
            }

            // =============================
            // 🔥 AUTO ADD ROW
            // =============================
            if (row + rowSpan >= DashboardGrid.RowDefinitions.Count - 2)
            {
                EnsureRow(DashboardGrid.RowDefinitions.Count + 5);
            }

            // =============================
            // 🔥 LIMIT KHÔNG VƯỢT VIEWPORT
            // =============================
            double maxVisibleRow = (DashboardScroll.VerticalOffset + DashboardScroll.ViewportHeight) / cellHeight;

            row = (int)Math.Min(row, maxVisibleRow - rowSpan);
            row = Math.Max(0, row);

            // =============================
            // 🔥 UPDATE POSITION
            // =============================
            if (Grid.GetColumn(selectedWidget) != column || Grid.GetRow(selectedWidget) != row)
            {
                Grid.SetColumn(selectedWidget, column);
                Grid.SetRow(selectedWidget, row);
                _isDirty = true;
            }
        }

        private void DashboardGrid_Drop(object sender, DragEventArgs e)
		{
			if (!editMode || !e.Data.GetDataPresent(typeof(WidgetItem))) return;
			WidgetItem widgetItem = e.Data.GetData(typeof(WidgetItem)) as WidgetItem;
			FrameworkElement newWidget = Activator.CreateInstance(widgetItem.WidgetType) as FrameworkElement;
			SetupWidget(newWidget); Point pos = e.GetPosition(DashboardGrid);
			int column = (int)(pos.X / (DashboardGrid.ActualWidth / 12)); int row = (int)(pos.Y / 80);
			var span = CalculateWidgetSpan(newWidget);
			Grid.SetColumn(newWidget, Math.Max(0, Math.Min(column, 12 - span.colSpan))); 
            Grid.SetRow(newWidget, row); 
            Grid.SetColumnSpan(newWidget, span.colSpan); 
            Grid.SetRowSpan(newWidget, span.rowSpan);
			DashboardGrid.Children.Add(newWidget); SmartCascadePush(newWidget); _isDirty = true;
		}

		void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!editMode) return; selectedWidget = sender as FrameworkElement;
			startPoint = e.GetPosition(DashboardGrid); _clickOffset = e.GetPosition(selectedWidget);
			isDraggingWidget = true; selectedWidget.CaptureMouse(); Panel.SetZIndex(selectedWidget, 999);
		}

		void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (selectedWidget != null) { SmartCascadePush(selectedWidget); selectedWidget.ReleaseMouseCapture(); } isDraggingWidget = false; selectedWidget = null; }

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
                Grid.SetColumnSpan(widget, targetColSpan); Grid.SetRowSpan(widget, targetRowSpan);
                SmartCascadePush(widget); _isDirty = true;
            }
        }

        private FrameworkElement FindParentWidget(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is UserControl || (parent is FrameworkElement fe && DashboardGrid.Children.Contains(fe))) return parent as FrameworkElement;
            return FindParentWidget(parent);
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
                try { allLayouts = JsonSerializer.Deserialize<List<WidgetLayout>>(File.ReadAllText(filePath)) ?? new List<WidgetLayout>(); } catch { }
            }
            allLayouts.RemoveAll(x => x.Dashboard == CurrentDashboard);
            if (newLayouts != null) allLayouts.AddRange(newLayouts);
            File.WriteAllText(filePath, JsonSerializer.Serialize(allLayouts, new JsonSerializerOptions { WriteIndented = true }));
            SetFilePermissionForAllUsers(filePath);
        }

        void LoadLayout() 
        { 
            DashboardGrid.Children.Clear();
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LightInsight");
            string filePath = Path.Combine(folder, "dashboard_layout.json");
            if (!File.Exists(filePath)) return;
            try
            {
                var layouts = JsonSerializer.Deserialize<List<WidgetLayout>>(File.ReadAllText(filePath));
                if (layouts == null) return;
                foreach (var layout in layouts.Where(x => x.Dashboard == CurrentDashboard))
                {
                    var widgetType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == layout.Type);
                    if (widgetType == null) continue;
                    FrameworkElement widget = Activator.CreateInstance(widgetType) as FrameworkElement;
                    SetupWidget(widget);
                    Grid.SetRow(widget, layout.Row); Grid.SetColumn(widget, layout.Column);
                    Grid.SetRowSpan(widget, layout.RowSpan); Grid.SetColumnSpan(widget, layout.ColumnSpan);
                    DashboardGrid.Children.Add(widget);
                    EnsureRow(layout.Row + layout.RowSpan);
                }
            } catch { }
            _isDirty = false;
        }

		void InitGrid() { DashboardGrid.RowDefinitions.Clear(); GridOverlay.RowDefinitions.Clear(); for (int i = 0; i < 10; i++) EnsureRow(i); }

		void CreateGrid() { GridOverlay.Children.Clear(); for (int r = 0; r < GridOverlay.RowDefinitions.Count; r++) for (int c = 0; c < 12; c++) { Border b = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)), BorderThickness = new Thickness(0.5) }; Grid.SetRow(b, r); Grid.SetColumn(b, c); GridOverlay.Children.Add(b); } }

		void SetupWidget(FrameworkElement widget)
		{
            if (widget is Control ctrl && ctrl.Background == null) ctrl.Background = Brushes.Transparent;
            else if (widget is Panel panel && panel.Background == null) panel.Background = Brushes.Transparent;

			widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown; widget.MouseMove += Widget_MouseMove; widget.MouseLeftButtonUp += Widget_MouseLeftButtonUp;
            
            widget.Loaded += (s, e) => {
                if (widget is IResizableWidget resizable && resizable.ResizeThumb != null)
                {
                    resizable.ResizeThumb.DragDelta -= Thumb_DragDelta;
                    resizable.ResizeThumb.DragDelta += Thumb_DragDelta;
                }
            };

            if (widget is IDashboardWidget dw)
            {
                dw.DeleteRequested += (s, e) => { DashboardGrid.Children.Remove(widget); _isDirty = true; };
                dw.SetEditMode(editMode);
            }
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (IsSidebarCollapsed) { SidebarColumn.Width = new GridLength(220); CollapseIcon.Kind = PackIconMaterialKind.ChevronLeft; }
            else { SidebarColumn.Width = new GridLength(40); CollapseIcon.Kind = PackIconMaterialKind.ChevronRight; }
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        void SmartCascadePush(FrameworkElement movedWidget)
        {
            var widgets = DashboardGrid.Children.OfType<FrameworkElement>().ToList();
            if (widgets.Count <= 1) return;
            var anchor = movedWidget;
            var placed = new List<FrameworkElement> { anchor };
            var ordered = widgets.Where(w => w != anchor).OrderBy(w => Grid.GetRow(w)).ThenBy(w => Grid.GetColumn(w)).ToList();
            foreach (var widget in ordered)
            {
                int rowSpan = Grid.GetRowSpan(widget); int col = Grid.GetColumn(widget); int colSpan = Grid.GetColumnSpan(widget);
                int candidateRow = 0; bool collision;
                do {
                    collision = false;
                    foreach (var other in placed) {
                        if (IsRectOverlap(candidateRow, col, rowSpan, colSpan, Grid.GetRow(other), Grid.GetColumn(other), Grid.GetRowSpan(other), Grid.GetColumnSpan(other))) {
                            candidateRow = Grid.GetRow(other) + Grid.GetRowSpan(other); collision = true; break;
                        }
                    }
                } while (collision);
                Grid.SetRow(widget, candidateRow); placed.Add(widget);
            }
            if (widgets.Any()) EnsureRow(widgets.Max(w => Grid.GetRow(w) + Grid.GetRowSpan(w)));
        }

        private bool IsRectOverlap(int r1, int c1, int rs1, int cs1, int r2, int c2, int rs2, int cs2)
        { return r1 < r2 + rs2 && r1 + rs1 > r2 && c1 < c2 + cs2 && c1 + cs1 > c2; }

		private WorkspaceModel FindById(IEnumerable<WorkspaceModel> list, string id)
		{
			foreach (var item in list) { if (item.Id == id) return item; var found = FindById(item.Children, id); if (found != null) return found; }
			return null;
		}

		private object OnThemeChanged(Message message, FQID dest, FQID sender) { ApplySmartClientTheme(null); return null; }

		private void ApplySmartClientTheme(Theme scTheme) { Dispatcher.Invoke(() => { var themeUri = ClientControl.Instance.Theme.ThemeType == ThemeType.Light ? "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml" : "/LightInsight;component/Dashboard/Dashboard/Themes/Dark.xaml"; var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) }; if (_currentThemeDictionary != null) Resources.MergedDictionaries.Remove(_currentThemeDictionary); Resources.MergedDictionaries.Add(newDict); _currentThemeDictionary = newDict; }); }

		private void ApplySmartClientLanguage(string name) { var uri = name == "vi-VN" ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml" : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml"; var newDict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) }; if (_currentLanguageDictionary != null) Resources.MergedDictionaries.Remove(_currentLanguageDictionary); Resources.MergedDictionaries.Add(newDict); _currentLanguageDictionary = newDict; RefreshMenuLocalization(); }

        private bool ConfirmBeforeLeave() { return true; }

		void SetFilePermissionForAllUsers(string filePath)
		{
			try
			{
				var fileInfo = new FileInfo(filePath); var fileSecurity = fileInfo.GetAccessControl();
				var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
				var rule = new System.Security.AccessControl.FileSystemAccessRule(users, System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.Modify, System.Security.AccessControl.InheritanceFlags.None, System.Security.AccessControl.PropagationFlags.NoPropagateInherit, System.Security.AccessControl.AccessControlType.Allow);
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

		bool IsWidgetAdded(Type widgetType) { return DashboardGrid.Children.OfType<FrameworkElement>().Any(x => x.GetType() == widgetType); }

        private void WidgetLibrary_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { startPoint = e.GetPosition(null); }
        private void WidgetLibrary_MouseMove(object sender, MouseEventArgs e) { if (e.LeftButton != MouseButtonState.Pressed) return; Point mousePos = e.GetPosition(null); Vector diff = startPoint - mousePos; if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance) { FrameworkElement element = sender as FrameworkElement; WidgetItem widget = element?.DataContext as WidgetItem; if (widget != null) DragDrop.DoDragDrop(element, new DataObject(typeof(WidgetItem), widget), DragDropEffects.Copy); } }
        private void WidgetLibrary_Item_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { Point mousePos = e.GetPosition(null); if (Math.Abs(startPoint.X - mousePos.X) <= 3 && Math.Abs(startPoint.Y - mousePos.Y) <= 3) { WidgetItem widget = (sender as FrameworkElement)?.DataContext as WidgetItem; if (widget != null) AddWidget_Click(new Button { Tag = widget }, null); } }
        private void DashboardScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e) { DashboardScroll.ScrollToVerticalOffset(DashboardScroll.VerticalOffset - e.Delta); e.Handled = true; }
        private void DashboardScroll_ScrollChanged(object sender, ScrollChangedEventArgs e) { if (editMode && DashboardScroll.VerticalOffset + DashboardScroll.ViewportHeight >= DashboardScroll.ExtentHeight - 20) EnsureRow(DashboardGrid.RowDefinitions.Count + 5); }
        Popup _currentPopup;
        bool _isHoveringPopup = false;

        private void ItemBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsSidebarCollapsed) return;

            var border = sender as FrameworkElement;
            var popup = border.Tag as Popup;

            if (popup == null) return;

            // 🔥 FIX QUAN TRỌNG: đóng popup cũ ngay lập tức
            if (_currentPopup != null && _currentPopup != popup)
            {
                _currentPopup.IsOpen = false;
            }

            popup.IsOpen = true;
            _currentPopup = popup;
        }


        public static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    return t;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
       
        private async void Popup_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHoveringPopup = false;

            await Task.Delay(200);

            if (!_isHoveringPopup && _currentPopup != null)
            {
                _currentPopup.IsOpen = false;
                _currentPopup = null;
            }
        }

        private void Popup_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHoveringPopup = true;
        }
        
        private async void ItemBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            var popup = (sender as FrameworkElement)?.Tag as Popup;

            await Task.Delay(150);

            // chỉ đóng nếu vẫn là popup hiện tại
            if (!_isHoveringPopup && popup == _currentPopup)
            {
                popup.IsOpen = false;
                _currentPopup = null;
            }
        }

    }
}
#endregion