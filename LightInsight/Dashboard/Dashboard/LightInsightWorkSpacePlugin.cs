using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoOS.Platform.Client;

namespace LightInsight.Dashboard.Dashboard
{
    public class LightInsightWorkSpacePlugin : WorkSpacePlugin
    {
        public override Guid Id => new Guid("5EEF2FD9-AE41-4F69-98FE-2030D3004F7E");

        public override string Name => "Light Insight";

        public override void Init()
        {
            LoadProperties(true);
            List<Rectangle> rectangles = new List<Rectangle>();

            rectangles.Add(new Rectangle(700, 0, 300, 300));
            ViewAndLayoutItem.InsertViewItemPlugin(rectangles.Count - 1, new DashboardViewItem(), new Dictionary<string, string>());
        }

        public override void Close()
        {
        }
        public override void ViewItemConfigurationModified(int index)
        {

        }

    }
}
