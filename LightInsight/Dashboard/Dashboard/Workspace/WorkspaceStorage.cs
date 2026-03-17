using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

namespace LightInsight.Dashboard.Dashboard.Workspace
{
    public static class WorkspaceStorage
    {
        private static string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "LightInsight");

        private static string file =
            Path.Combine(folder, "workspace.json");

        /// <summary>
        /// Load workspace list từ file
        /// </summary>
        public static List<WorkspaceModel> LoadWorkspaces()
        {
            EnsureStorage();

            string json = File.ReadAllText(file);

            if (string.IsNullOrWhiteSpace(json))
                return CreateDefault();

            var list = JsonSerializer.Deserialize<List<WorkspaceModel>>(json);

            if (list == null)
                list = new List<WorkspaceModel>();

            EnsureRoot(list);

            return list;
        }

        /// <summary>
        /// Lưu workspace xuống file
        /// </summary>
        public static void SaveWorkspaces(ObservableCollection<WorkspaceModel> list)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(file, json);
        }

        /// <summary>
        /// Tạo dữ liệu mặc định
        /// </summary>
        private static List<WorkspaceModel> CreateDefault()
        {
            return new List<WorkspaceModel>
            {
                new WorkspaceModel
                {
                    Id = "ROOT_DASHBOARD",
                    Name = "Dashboards",
                    Icon = PackIconMaterialKind.ViewDashboard,
                    Type = "DashboardRoot",
                    ParentId = null
                }
            };
        }

        /// <summary>
        /// Đảm bảo ROOT_DASHBOARD tồn tại
        /// </summary>
        private static void EnsureRoot(List<WorkspaceModel> list)
        {
            if (!list.Any(x => x.Id == "ROOT_DASHBOARD"))
            {
                list.Insert(0, new WorkspaceModel
                {
                    Id = "ROOT_DASHBOARD",
                    Name = "Dashboards",
                    Icon = PackIconMaterialKind.ViewDashboard,
                    Type = "DashboardRoot",
                    ParentId = null
                });
            }
        }
        private static void EnsureStorage()
        {
            if (!Directory.Exists(folder))
            {
                DirectoryInfo dir = Directory.CreateDirectory(folder);

                DirectorySecurity security = dir.GetAccessControl();

                security.AddAccessRule(
                    new FileSystemAccessRule(
                        new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow));

                dir.SetAccessControl(security);
            }

            if (!File.Exists(file))
            {
                var list = CreateDefault();
                SaveWorkspaces(new ObservableCollection<WorkspaceModel>(list));
            }
        }
    }
}