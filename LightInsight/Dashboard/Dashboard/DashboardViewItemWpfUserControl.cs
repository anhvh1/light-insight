using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoOS.Platform.Client;

namespace LightInsight.Dashboard.Dashboard
{
    public class DashboardViewItemWpfUserControl : ViewItemWpfUserControl
    {
        private DashboardView _view;

        public DashboardViewItemWpfUserControl()
        {
            _view = new DashboardView();
            this.Content = _view;
        }

        public override void Init()
        {
            base.Init();
        }

        public override void Close()
        {
            base.Close();
        }
    }
}
