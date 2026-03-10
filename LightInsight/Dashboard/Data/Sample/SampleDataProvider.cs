using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightInsight.Dashboard.Data.AlarmsAndEvents;

namespace LightInsight.Dashboard.Data
{
    public static class SampleDataProvider
    {
        public static object GetData(WigetType type)
        {
            if (type == WigetType.SampleWidget)
            {
                return new List<SampleItem>
                {
                    //Data
                };
            }
            return null;
        }
    }
}
