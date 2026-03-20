using LightInsight.Dashboard.Dashboard;
using LightInsight.Dashboard.Dashboard.Workspace;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Background;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace LightInsight
{
    /// <summary>
    /// The PluginDefinition is the ‘entry’ point to any plugin.  
    /// This is the starting point for any plugin development and the class MUST be available for a plugin to be loaded.  
    /// Several PluginDefinitions are allowed to be available within one DLL.
    /// Here the references to all other plugin known objects and classes are defined.
    /// The class is an abstract class where all implemented methods and properties need to be declared with override.
    /// The class is constructed when the environment is loading the DLL.
    /// </summary>
    public class LightInsightDefinition : PluginDefinition
    {
        private List<WorkSpacePlugin> _workSpacePlugins = new List<WorkSpacePlugin>();
        private List<ViewItemPlugin> _workSpaceViewItemPlugins = new List<ViewItemPlugin>();
        public static List<PackIconMaterialKind> IconList { get; private set; }
        public override Guid Id => new Guid("55A448B4-4487-4BEF-9DBC-892C14F7D3C0");

        public override string Name => "Light Insight";

        public override Image Icon => throw new NotImplementedException();
        private static void LoadIcons()
        {
            IconList = Enum.GetValues(typeof(PackIconMaterialKind))
                .Cast<PackIconMaterialKind>()
                .ToList();
        }
        public override void Init()
        {
            // load trước icon
            LoadIcons();
            // load trước dữ liệu trong workspace
            WorkspaceService.Instance.Load();
            if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.SmartClient)
            {
                _workSpacePlugins.Add(new LightInsightWorkSpacePlugin());
                _workSpaceViewItemPlugins.Add(new DashboardViewItem());

            }
        }
        public override void Close()
        {
            _workSpacePlugins.Clear();
            _workSpaceViewItemPlugins.Clear();
        }
        // Removed the override keyword as PluginDefinition does not define ViewItemManagers as abstract or virtual.
        public override List<WorkSpacePlugin> WorkSpacePlugins
        {
            get { return _workSpacePlugins; }
        }
        public override List<ViewItemPlugin> ViewItemPlugins
        {
            get { return _workSpaceViewItemPlugins; }
        }
    }
}
