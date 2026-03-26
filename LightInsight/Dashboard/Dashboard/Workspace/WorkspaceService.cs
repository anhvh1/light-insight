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
        public event Action OnWorkspaceChanged;
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

        private bool _isLoaded = false;
        public void Load()
        {
            var list = WorkspaceStorage.LoadWorkspaces();
            BuildTree(list);
            var roots = list.Where(x => string.IsNullOrEmpty(x.ParentId)).ToList();

            Workspaces.Clear();
            foreach (var root in roots)
            {
                Workspaces.Add(root);
            }

            _isLoaded = true;
        }

        public void Refresh()
        {
            _isLoaded = false;
            Load();
        }
        private void BuildTree(List<WorkspaceModel> list)
        {
            // 🔥 Clear children trước để tránh bị duplicate khi load lại
            foreach (var item in list)
            {
                if (item.Children == null)
                    item.Children = new ObservableCollection<WorkspaceModel>();
                else
                    item.Children.Clear();
                
                item.IsGroup = false; // Reset trước khi dựng lại
            }

            // 🔥 Tạo lookup để tìm nhanh
            var lookup = list.ToDictionary(x => x.Id);

            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item.ParentId)
                    && lookup.ContainsKey(item.ParentId))
                {
                    var parent = lookup[item.ParentId];

                    // tránh add chính nó vào chính nó
                    if (parent != item)
                    {
                        parent.Children.Add(item);
                        parent.IsGroup = true; // Có con thì là Group
                        if (parent.Id == "ROOT_DASHBOARD") parent.IsExpanded = true; // Mở sẵn mục Dashboards
                    }
                }
            }
        }
        private WorkspaceService() { }
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
                parent.IsGroup = true;
                if (parent.Id == "ROOT_DASHBOARD") parent.IsExpanded = true;
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
        public void NotifyChanged()
        {
            OnWorkspaceChanged?.Invoke();
        }

        public List<WorkspaceModel> GetAllWorkspacesFlat()
        {
            var list = new List<WorkspaceModel>();
            foreach (var root in Workspaces)
            {
                GetRecursive(root, list);
            }
            return list;
        }

        private void GetRecursive(WorkspaceModel node, List<WorkspaceModel> list)
        {
            list.Add(node);
            foreach (var c in node.Children)
            {
                GetRecursive(c, list);
            }
        }
    }
}

