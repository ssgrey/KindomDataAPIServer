using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models
{
    
    public class ApiConfig
    {
        public string token { get; set; }
        public string tetproj { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string projectname { get; set; }
        public int type { get; set; }

        //导出曲线参数  
        public string caseId { get; set; }
        public List<WellLogData> welllogdata { get; set; }

        //返回解释结论参数
        public List<TableHeader> tableHeader { get; set; }
        public string unitSystem { get; set; }
        public ResultData resultdata { get; set; }

    }

    public class WellLogData
    {
        public string wellId { get; set; }
        public List<CurveOption> curveOptions { get; set; }
        public List<WellLogInfo> wellLogInfos { get; set; }
        public string exportDepthScopeType { get; set; }
        public double invalidValue { get; set; }
        public double interval { get; set; }
        public string separator { get; set; }
        public string caseId { get; set; }
        public string tvus { get; set; }
    }

    public class CurveOption
    {
        public string name { get; set; }
        public string outputName { get; set; }
        public int precision { get; set; }
        public string datasetId { get; set; }
        public string datasetType { get; set; }
    }

    public class WellLogInfo
    {
        public string id { get; set; }
    }


    public class TableHeader
    {
        public string props { get; set; }
        public string label { get; set; }
        public object width { get; set; } // 可以是string或int，所以用object
        public string @fixed { get; set; } // 使用@前缀因为fixed是C#关键字
    }

    public class ResultData
    {
        public string caseId { get; set; }
        public List<string> wellIds { get; set; }
        public List<string> curveNames { get; set; }
        public List<string> indicator { get; set; }
        public string datasetName { get; set; }

        public ResultData Clone()
        {
            var clone = (ResultData)MemberwiseClone();

            // 对于引用类型的集合，需要创建新的集合实例并复制元素
            if (wellIds != null)
                clone.wellIds = new List<string>(wellIds);

            if (curveNames != null)
                clone.curveNames = new List<string>(curveNames);

            if (indicator != null)
                clone.indicator = new List<string>(indicator);

            return clone;
        }
    }
}
