using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LightInsight.Dashboard.Dashboard;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    // 1. NHÚNG MODEL VÀO ĐÂY
    public class AlarmItem
    {
        public string Location { get; set; }
        public string EventType { get; set; }
        public string IdAndTime { get; set; }
        public string Status { get; set; }
        public bool IsHighPriority { get; set; }
        public bool IsLastItem { get; set; }

        public string PriorityColor => IsHighPriority ? "#FF4B4B" : "Transparent";

        public string IndicatorColor
        {
            get
            {
                if (Status == "Active") return IsHighPriority ? "#FF4B4B" : "#FFC107";
                if (Status == "Ack") return "#FFC107";
                return "#FF9800";
            }
        }

        public string BadgeBgColor
        {
            get
            {
                if (Status == "Active") return "#331515";
                if (Status == "Ack") return "#151A28";
                return "#222222";
            }
        }

        public string BadgeTextColor
        {
            get
            {
                if (Status == "Active") return "#FF4B4B";
                if (Status == "Ack") return "#FFA500";
                return "#AAAAAA";
            }
        }

        public string BottomLineColor => IsLastItem ? "Transparent" : "#2A2B31";
    }

    public partial class LiveAlarmsFeedWidget : UserControl, IDashboardWidget
    {
        public event EventHandler DeleteRequested;

        public LiveAlarmsFeedWidget()
        {
            InitializeComponent();
            DeleteButton.Visibility = Visibility.Collapsed;

            // 2. NHÚNG TRỰC TIẾP FAKE DATA VÀO DATACONTEXT
            this.DataContext = new List<AlarmItem>
            {
                new AlarmItem { Location = "Entrance Gate", EventType = "Face Recognized", IdAndTime = "ALM-001 • 10:23:15", Status = "Active", IsHighPriority = true },
                new AlarmItem { Location = "Parking Lot A", EventType = "Loitering Detected", IdAndTime = "ALM-002 • 10:18:42", Status = "Active", IsHighPriority = false },
                new AlarmItem { Location = "Server Room", EventType = "Temperature Alert", IdAndTime = "ALM-003 • 10:12:08", Status = "Ack", IsHighPriority = false },
                new AlarmItem { Location = "Warehouse Cam 02", EventType = "Connection Lost", IdAndTime = "ALM-004 • 10:05:33", Status = "Active", IsHighPriority = true },
                new AlarmItem { Location = "Lobby", EventType = "Motion", IdAndTime = "ALM-005 • 09:58:17", Status = "Closed", IsHighPriority = false },
                new AlarmItem { Location = "Loading Dock", EventType = "Object Left Behind", IdAndTime = "ALM-006 • 09:45:00", Status = "Active", IsHighPriority = false },
                new AlarmItem { Location = "Perimeter Fence", EventType = "Line Crossing", IdAndTime = "ALM-007 • 09:30:22", Status = "Active", IsHighPriority = true },
                new AlarmItem { Location = "Corridor B", EventType = "Behavior Alert", IdAndTime = "ALM-008 • 09:15:10", Status = "Ack", IsHighPriority = false, IsLastItem = false },
                new AlarmItem { Location = "Main Entrance", EventType = "Unauthorized Person", IdAndTime = "ALM-009 • 09:10:05", Status = "Active", IsHighPriority = true },
                new AlarmItem { Location = "IT Room Door", EventType = "Tailgating Detected", IdAndTime = "ALM-010 • 08:55:12", Status = "Active", IsHighPriority = true },
                new AlarmItem { Location = "Backdoor Alley", EventType = "Intrusion", IdAndTime = "ALM-011 • 08:30:00", Status = "Closed", IsHighPriority = false },
                new AlarmItem { Location = "Parking B", EventType = "Vehicle Wrong Way", IdAndTime = "ALM-012 • 08:15:22", Status = "Ack", IsHighPriority = false, IsLastItem = true }
            };
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