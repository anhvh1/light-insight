using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard
{
    public class WidgetItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public Type WidgetType { get; set; }
        public MahApps.Metro.IconPacks.PackIconMaterialKind Icon { get; set; }
    }

    public class WidgetGroup
    {
        public string Title { get; set; }
        public ObservableCollection<WidgetItem> Items { get; set; } = new ObservableCollection<WidgetItem>();
    }

    public class WidgetLayout
    {
        public string Dashboard { get; set; }
        public string Type { get; set; }

        public int Row { get; set; }
        public int Column { get; set; }
        public int RowSpan { get; set; }
        public int ColumnSpan { get; set; }
    }
}
