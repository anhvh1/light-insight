using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard
{
    public interface IDashboardWidget
    {
        event EventHandler DeleteRequested;

        void SetEditMode(bool isEdit);
    }
}
