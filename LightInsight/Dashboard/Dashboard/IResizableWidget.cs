using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightInsight.Dashboard.Dashboard
{
	internal interface IResizableWidget : IDashboardWidget
	{
		int MinCol { get; }
		int MinRow { get; }
	}
}
