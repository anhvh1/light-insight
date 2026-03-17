using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LightInsight.Dashboard.Dashboard.Workspace
{
    public partial class WorkspaceWindow : Window
    {
        public WorkspaceWindow()
        {
            InitializeComponent();

            DataContext = WorkspaceService.Instance;

            LoadWorkspaces();
        }

        private void LoadWorkspaces()
        {
            var list = WorkspaceStorage.LoadWorkspaces();

            WorkspaceService.Instance.Workspaces.Clear();

            if (list == null || list.Count == 0)
                return;

            var dict = list.ToDictionary(x => x.Id);

            // reset children
            foreach (var item in list)
            {
                item.Children.Clear();
            }

            // build tree
            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(item.ParentId))
                {
                    WorkspaceService.Instance.Workspaces.Add(item);
                }
                else
                {
                    if (dict.TryGetValue(item.ParentId, out var parent))
                    {
                        parent.Children.Add(item);
                    }
                }
            }
        }

        private void AddWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var parent = WorkspaceTree.SelectedItem as WorkspaceModel;
            if (parent == null)
            { parent = WorkspaceService.Instance.Workspaces.FirstOrDefault(x => x.Id == "ROOT_DASHBOARD"); }
            AddWorkspaceWindow win = new AddWorkspaceWindow();
            win.Owner = this;
            if (win.ShowDialog() == true)
            {
                WorkspaceService.Instance.AddWorkspace(win.WorkspaceLabel, win.WorkspaceIcon, win.WorkspaceType, parent);
                LoadWorkspaces();
            }
        }
        private void DeleteWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var item = WorkspaceTree.SelectedItem as WorkspaceModel;

            if (item == null)
                return;

            // tìm parent
            var parent = FindParent(WorkspaceService.Instance.Workspaces, item);

            if (parent != null)
            {
                parent.Children.Remove(item);
            }
            else
            {
                WorkspaceService.Instance.Workspaces.Remove(item);
            }

            WorkspaceService.Instance.Save();

            // refresh lại tree
            LoadWorkspaces();
        }
        private WorkspaceModel FindParent(IEnumerable<WorkspaceModel> list, WorkspaceModel child)
        {
            foreach (var item in list)
            {
                if (item.Children.Contains(child))
                    return item;

                var parent = FindParent(item.Children, child);

                if (parent != null)
                    return parent;
            }

            return null;
        }
        private void EditWorkspace_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceModel item = null;

            // Nếu click từ TreeView item
            if (sender is Button btn && btn.Tag is WorkspaceModel model)
            {
                item = model;
            }
            else
            {
                // Nếu click từ toolbar
                item = WorkspaceTree.SelectedItem as WorkspaceModel;
            }

            if (item == null)
                return;

            AddWorkspaceWindow win = new AddWorkspaceWindow();
            win.Owner = this;

            // Đổ dữ liệu cũ lên form
            win.LabelBox.Text = item.Name;
            win.WorkspaceIcon = item.Icon;
            win.WorkspaceType = item.Type;

            if (win.ShowDialog() == true)
            {
                // Cập nhật lại dữ liệu
                item.Name = win.WorkspaceLabel;
                item.Icon = win.WorkspaceIcon;
                item.Type = win.WorkspaceType;

                WorkspaceService.Instance.Save();

                // refresh lại tree
                LoadWorkspaces();
            }
        }
        private void WorkspaceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = WorkspaceTree.SelectedItem as WorkspaceModel;

            if (item == null)
            {
                EditButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
                return;
            }

            // chỉ cho edit/delete Dashboards
            if (item.Type == "Dashboards")
            {
                EditButton.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible;
            }
            else
            {
                EditButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}