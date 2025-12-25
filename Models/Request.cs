using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class WellDataRequest
    {
        /// <summary>
        /// 覆盖标志
        /// </summary>
        [JsonProperty("overWriteFlag")]
        public int OverWriteFlag { get; set; } = 0;

        /// <summary>
        /// 井数据项列表
        /// </summary>
        [JsonProperty("items")]
        public List<WellItemRequest> Items { get; set; }
    }

    public class WellItemRequest
    {
        /// <summary>
        /// 井名
        /// </summary>
        [JsonProperty("wellName")]
        public string WellName { get; set; }

        /// <summary>
        /// 别名
        /// </summary>
        [JsonProperty("alias")]
        public string Alias { get; set; }

        /// <summary>
        /// 井号
        /// </summary>
        [JsonProperty("wellNumber")]
        public string WellNumber { get; set; }

        /// <summary>
        /// 井类型 (0: 油井, 1: 水井, 2: 气井等)
        /// </summary>
        [JsonProperty("wellType")]
        public int WellType { get; set; }

        /// <summary>
        /// 井轨迹类型 (0: 直井, 1: 定向井, 2: 水平井等)
        /// </summary>
        [JsonProperty("wellTrajectoryType")]
        public int WellTrajectoryType { get; set; }

        /// <summary>
        /// 井口X坐标
        /// </summary>
        [JsonProperty("wellheadX")]
        public double WellheadX { get; set; }

        /// <summary>
        /// 井口Y坐标
        /// </summary>
        [JsonProperty("wellheadY")]
        public double WellheadY { get; set; }

        /// <summary>
        /// 井底X坐标
        /// </summary>
        [JsonProperty("wellboreBottomX")]
        public double WellboreBottomX { get; set; }

        /// <summary>
        /// 井底Y坐标
        /// </summary>
        [JsonProperty("wellboreBottomY")]
        public double WellboreBottomY { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// 国家
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }

        /// <summary>
        /// 区域
        /// </summary>
        [JsonProperty("region")]
        public string Region { get; set; }

        /// <summary>
        /// 地区/区县
        /// </summary>
        [JsonProperty("districts")]
        public string Districts { get; set; }

        /// <summary>
        /// 完井层位
        /// </summary>
        [JsonProperty("completionHorizon")]
        public string CompletionHorizon { get; set; }

        /// <summary>
        /// 生产层位
        /// </summary>
        [JsonProperty("prodFormations")]
        public string ProdFormations { get; set; }

        /// <summary>
        /// 当前作业者
        /// </summary>
        [JsonProperty("currentOperator")]
        public string CurrentOperator { get; set; }

        /// <summary>
        /// 生产油田
        /// </summary>
        [JsonProperty("producingField")]
        public string ProducingField { get; set; }
    }
}
