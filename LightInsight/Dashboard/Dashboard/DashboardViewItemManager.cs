using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoOS.Platform.Client;

namespace LightInsight.Dashboard.Dashboard
{
    public class DashboardViewItemManager : ViewItemManager
    {
        private DashboardViewItemWpfUserControl _viewControl;
        public DashboardViewItemManager() : base() { }

        public override ViewItemWpfUserControl GenerateViewItemWpfUserControl()
        {
            _viewControl = new DashboardViewItemWpfUserControl();
            return _viewControl;
        }
    }
}
