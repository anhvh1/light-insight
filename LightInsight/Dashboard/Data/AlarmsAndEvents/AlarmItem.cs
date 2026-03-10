using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Data.AlarmsAndEvents
{
    class AlarmItem
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

    public class SeverityData
    {
        public string Title { get; set; }
        public int Count { get; set; }

        public string ColorHex
        {
            get
            {
                switch (Title)
                {
                    case "Minor": return "#2ECC71";
                    case "Warning": return "#F39C12";
                    case "Major": return "#E74C3C";
                    case "Critical": return "#E67E22";
                    default: return "#888888";
                }
            }
        }
    }
}
