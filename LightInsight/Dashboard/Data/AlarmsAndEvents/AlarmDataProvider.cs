using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Data.AlarmsAndEvents
{
    class AlarmDataProvider
    {
        public static object GetData(WigetType type)
        {
            switch (type)
            {
                case WigetType.LiveAlarmsFeedWidget:
                    return new List<AlarmItem>
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

                case WigetType.AlarmsBySeverityWidget:
                    return new List<SeverityData>
                    {
                        new SeverityData { Title = "Minor", Count = 45 },
                        new SeverityData { Title = "Warning", Count = 66 },
                        new SeverityData { Title = "Major", Count = 27 },
                        new SeverityData { Title = "Critical", Count = 12 }
                    };
                default:
                    return null;
            }
        }
    }
}
