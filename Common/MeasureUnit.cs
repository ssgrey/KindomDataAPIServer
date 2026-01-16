using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public class MeasureUnit
    {
        public int MeasureID {  get; set; }
        public int UnitId { get; set; }
        public string Unit { get; set; }//Abbr
    }

    public class MeasureUnitList
    {
        public static MeasureUnit DepthFeet { get; } = new MeasureUnit();

    }
}
