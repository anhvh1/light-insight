using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using LightInsight.Dashboard.Dashboard;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using VideoOS.Platform.Data;
using System.Threading;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    // 1. MODEL CẬP NHẬT ĐỦ 4 TRẠNG THÁI
    public class AlarmItem
    {
        public string Location { get; set; }
        public string EventType { get; set; }
        public string IdAndTime { get; set; }
        public string Status { get; set; }
        public bool IsHighPriority { get; set; }
        public bool IsLastItem { get; set; }

        public string PriorityColor => IsHighPriority && Status == "New" ? "#FF4B4B" : "Transparent";

        public string IndicatorColor
        {
            get
            {
                switch (Status)
                {
                    case "New": return IsHighPriority ? "#FF4B4B" : "#FFC107";
                    case "In Progress": return "#FFC107"; // Vàng
                    case "On Hold": return "#2196F3";     // Xanh dương
                    case "Closed": return "#666666";      // Xám
                    default: return "#666666";
                }
            }
        }

        public string BadgeBgColor
        {
            get
            {
                switch (Status)
                {
                    case "New": return "#331515";
                    case "In Progress": return "#332A15";
                    case "On Hold": return "#152433";
                    case "Closed": return "#222222";
                    default: return "#222222";
                }
            }
        }

        public string BadgeTextColor
        {
            get
            {
                switch (Status)
                {
                    case "New": return "#FF4B4B";
                    case "In Progress": return "#FFC107";
                    case "On Hold": return "#2196F3";
                    case "Closed": return "#AAAAAA";
                    default: return "#AAAAAA";
                }
            }
        }

        public string BottomLineColor => IsLastItem ? "Transparent" : "#2A2B31";
    }

    // 2. WIDGET CLASS
    public partial class LiveAlarmsFeedWidget : UserControl, IResizableWidget
    {
        private ResourceDictionary _currentThemeDictionary;
        private object _themeChangedRegistration;
        private bool _widgetEditMode;

        private ObservableCollection<AlarmItem> _alarmsList;
        private object _newAlarmReceiver;

        public int MinCol => 3;
        public int MinRow => 4;

        public Thumb ResizeThumb => this.InternalResizeThumb;
        public event EventHandler DeleteRequested;

        public LiveAlarmsFeedWidget()
        {
            ApplySmartClientLanguage(Thread.CurrentThread.CurrentUICulture.Name);
            InitializeComponent();
            ApplySmartClientTheme(ClientControl.Instance?.Theme);

            _themeChangedRegistration = EnvironmentManager.Instance.RegisterReceiver(
                new MessageReceiver(OnThemeChanged),
                new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));
            DeleteButton.Visibility = Visibility.Collapsed;

            // Dùng ObservableCollection để giao diện tự cập nhật khi có alarm mới
            _alarmsList = new ObservableCollection<AlarmItem>();
            this.DataContext = _alarmsList;

            // Đăng ký sự kiện nạp/hủy dữ liệu
            this.Loaded += LiveAlarmsFeedWidget_Loaded;
            this.Unloaded += LiveAlarmsFeedWidget_Unloaded;
        }

        private void LiveAlarmsFeedWidget_Loaded(object sender, RoutedEventArgs e)
        {
            _alarmsList.Clear();
            LoadHistoricalAlarms();

            // Đăng ký nhận Alarm Live
            if (_newAlarmReceiver == null)
            {
                _newAlarmReceiver = AlarmServices.RegisterForLiveAlarms(OnNewAlarmReceived);
            }
        }

        private void LiveAlarmsFeedWidget_Unloaded(object sender, RoutedEventArgs e)
        {
            // Hủy đăng ký khi đóng Widget tránh rò rỉ bộ nhớ
            if (_newAlarmReceiver != null)
            {
                AlarmServices.UnregisterLiveAlarms(_newAlarmReceiver);
                _newAlarmReceiver = null;
            }
        }

        private void LoadHistoricalAlarms()
        {
            List<Alarm> alarmList = AlarmServices.GetHistoricalAlarms(50); // Lấy 50 báo động gần nhất
            if (alarmList == null || alarmList.Count == 0) return;

            for (int i = 0; i < alarmList.Count; i++)
            {
                AlarmItem item = MapMilestoneAlarmToAlarmItem(alarmList[i]);
                if (i == alarmList.Count - 1) item.IsLastItem = true;
                _alarmsList.Add(item);
            }
        }

        private void OnNewAlarmReceived(Alarm newAlarm)
        {
            if (newAlarm == null) return;

            // Phải đẩy lên luồng UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    AlarmItem item = MapMilestoneAlarmToAlarmItem(newAlarm);

                    // Xử lý ẩn hiện dòng kẻ gạch dưới
                    if (_alarmsList.Count == 0) item.IsLastItem = true;
                    if (_alarmsList.Count > 0) _alarmsList.Last().IsLastItem = true;

                    // Chèn báo động mới lên đầu danh sách
                    _alarmsList.Insert(0, item);

                    // Giữ lại tối đa 100 báo động trên UI để không bị nặng máy
                    if (_alarmsList.Count > 100)
                    {
                        _alarmsList.RemoveAt(_alarmsList.Count - 1);
                        if (_alarmsList.Count > 0) _alarmsList.Last().IsLastItem = true;
                    }
                }
                catch (Exception ex)
                {
                    EnvironmentManager.Instance.Log(false, "LightInsight Widget", "Update Live Alarm Error: " + ex.Message);
                }
            }));
        }

        private AlarmItem MapMilestoneAlarmToAlarmItem(Alarm alarm)
        {
            // Map State chuẩn
            string statusText = "New";
            switch (alarm.State)
            {
                case 1: statusText = "New"; break;
                case 4: statusText = "In Progress"; break;
                case 9: statusText = "On Hold"; break;
                case 11: statusText = "Closed"; break;
                default: statusText = alarm.StateName ?? "Unknown"; break;
            }

            // KHỞI TẠO CÁC BIẾN MẶC ĐỊNH
            bool isHigh = false;
            string camName = "Unknown Camera";
            string eventMsg = "Unknown Event";
            string alarmName = "Unknown Alarm";
            DateTime time = DateTime.Now;

            // BẮT BUỘC TRUY CẬP QUA EventHeader ĐỂ TRÁNH LỖI CS1061
            if (alarm.EventHeader != null)
            {
                isHigh = alarm.EventHeader.Priority == 1;
                camName = alarm.EventHeader.Source?.Name ?? "Unknown Camera";

                // Thuộc tính Message thường chứa tên hiển thị của Báo động
                eventMsg = alarm.EventHeader.Message ?? "Unknown Alarm";

                alarmName = alarm.EventHeader.Name.ToString();
                time = alarm.EventHeader.Timestamp.ToLocalTime();
            }

            return new AlarmItem
            {
                Location = camName,
                EventType = eventMsg,
                IdAndTime = $"{alarmName} • {time:dd/MM/yyyy HH:mm:ss}",
                Status = statusText,
                IsHighPriority = isHigh,
                IsLastItem = false
            };
        }

        // ========================================================
        // CÁC HÀM CŨ XỬ LÝ THEME, NGÔN NGỮ, KÉO THẢ (KHÔNG THAY ĐỔI)
        // ========================================================
        private void ApplySmartClientLanguage(string name)
        {
            var uri = name == "vi-VN"
                       ? "/LightInsight;component/Dashboard/Dashboard/Language/Vi.xaml"
                       : "/LightInsight;component/Dashboard/Dashboard/Language/English.xaml";

            var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);
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
                if (crTheme == ThemeType.Light)
                    themeUri = "/LightInsight;component/Dashboard/Dashboard/Themes/Light.xaml";

                var newDict = new ResourceDictionary { Source = new Uri(themeUri, UriKind.RelativeOrAbsolute) };

                if (_currentThemeDictionary != null)
                    Resources.MergedDictionaries.Remove(_currentThemeDictionary);

                Resources.MergedDictionaries.Insert(0, newDict);
                _currentThemeDictionary = newDict;
                DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            });
        }

        public void SetEditMode(bool isEdit)
        {
            _widgetEditMode = isEdit;
            DeleteButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            if (InternalResizeThumb != null)
                InternalResizeThumb.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            DashboardWidgetChrome.SyncMainBorderBrush(this, _widgetEditMode);
            this.Cursor = isEdit ? Cursors.SizeAll : Cursors.Arrow;
        }

        private void DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}