using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard.Workspace
{
    public class WorkspaceModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string IconStr { get; set; }

        public string Type { get; set; }
        public PackIconMaterialKind Icon { get; set; }
        public string ParentId { get; set; }
        public ObservableCollection<WorkspaceModel> Children { get; set; }
            = new ObservableCollection<WorkspaceModel>();
    }
}