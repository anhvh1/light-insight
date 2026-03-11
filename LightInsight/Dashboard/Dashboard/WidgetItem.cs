using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard
{
    public class WidgetItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public Type WidgetType { get; set; }
    }
    public class WidgetLayout
    {
        public string Dashboard { get; set; }
        public string Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
