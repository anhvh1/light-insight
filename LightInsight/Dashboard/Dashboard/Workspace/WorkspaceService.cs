using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard.Workspace
{

    public class WorkspaceService
    {
        private static WorkspaceService _instance;

        public static WorkspaceService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WorkspaceService();

                return _instance;
            }
        }

        public ObservableCollection<WorkspaceModel> Workspaces { get; set; }
            = new ObservableCollection<WorkspaceModel>();


        public void AddWorkspace(string name, PackIconMaterialKind icon, string type, WorkspaceModel parent)
        {
            var item = new WorkspaceModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Icon = icon,
                Type = type,
                ParentId = parent?.Id
            };

            if (parent != null)
            {
                parent.Children.Add(item);
            }
            else
            {
                Workspaces.Add(item);
            }

            Save();
        }
        public void Save()
        {
            var list = new List<WorkspaceModel>();

            foreach (var root in Workspaces)
            {
                Flatten(root, list);
            }

            WorkspaceStorage.SaveWorkspaces(
                new ObservableCollection<WorkspaceModel>(list));
        }
        private void Flatten(WorkspaceModel node, List<WorkspaceModel> list)
        {
            list.Add(new WorkspaceModel
            {
                Id = node.Id,
                Name = node.Name,
                Icon = node.Icon,
                Type = node.Type,
                ParentId = node.ParentId
            });

            foreach (var c in node.Children)
            {
                Flatten(c, list);
            }
        }
    }
}

