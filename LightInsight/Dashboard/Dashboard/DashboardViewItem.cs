using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace LightInsight.Dashboard.Dashboard
{
    public class DashboardViewItem : ViewItemPlugin
    {
        private DashboardViewItemManager _manager;
        internal protected static VideoOSIconSourceBase _treeNodeImage;
        public DashboardViewItem()
        {
            _treeNodeImage = new VideoOSIconUriSource
            {
                Uri = new Uri("pack://application:,,,/SCWorkSpace;component/Resources/WorkSpace.png")
            };
        }
        public override Guid Id => new Guid("2FB59DE8-A742-45C8-9EE5-9A8D9CBC3EE5");

        public override string Name => "WorkSpace Plugin View Item";
        public override VideoOSIconSourceBase IconSource => _treeNodeImage;

        public override void Close()
        {
            
        }
        public override ViewItemManager GenerateViewItemManager()
        {
            _manager = new DashboardViewItemManager();
            return _manager;
        }
      

        public override void Init()
        {
            GenerateViewItemManager();
        }
    }
}
