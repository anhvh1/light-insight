using System.Windows.Controls.Primitives;
namespace LightInsight.Dashboard.Dashboard
{
	internal interface IResizableWidget : IDashboardWidget
	{
		int MinCol { get; }
		int MinRow { get; }
		Thumb ResizeThumb { get; }
	}
}
