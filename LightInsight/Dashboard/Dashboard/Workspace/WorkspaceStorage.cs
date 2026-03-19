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

        private static string file = Path.Combine(folder, "workspace.json");

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
            EnsureStorage();

            string json = JsonSerializer.Serialize(list, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(file, json);
        }

        /// <summary>
        /// Đảm bảo storage tồn tại (folder + permission + file)
        /// </summary>
        private static void EnsureStorage()
        {
            // 1. Tạo folder nếu chưa có
            if (!Directory.Exists(folder))
            {
                DirectoryInfo dir = Directory.CreateDirectory(folder);
                SetFolderPermission(dir);
            }

            // 2. Tạo file nếu chưa có
            if (!File.Exists(file))
            {
                using (File.Create(file)) { }

                // 3. Set quyền cho file
                SetFilePermission(file);

                // 4. Ghi dữ liệu mặc định
                var defaultData = CreateDefault();
                string json = JsonSerializer.Serialize(defaultData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(file, json);
            }
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
        private static void SetFolderPermission(DirectoryInfo dir)
        {
            try
            {
                var security = dir.GetAccessControl();

                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                var rule = new FileSystemAccessRule(
                    users,
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                security.AddAccessRule(rule);
                dir.SetAccessControl(security);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Set folder permission failed: " + ex.Message);
            }
        }
        private static void SetFilePermission(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSecurity = fileInfo.GetAccessControl();

                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                var rule = new FileSystemAccessRule(
                    users,
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify,
                    AccessControlType.Allow);

                fileSecurity.AddAccessRule(rule);
                fileInfo.SetAccessControl(fileSecurity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Set file permission failed: " + ex.Message);
            }
        }
    }
}