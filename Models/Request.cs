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



    public class MetaInfo
    {
        public int MeasureId { get; set; }
        public int UnitId { get; set; }
        public string DisplayName { get; set; }
        public string PropertyName { get; set; }
    }

    public class AimuthData
    {
        public double Md { get; set; }
        public double Devi { get; set; }
        public double Azim { get; set; }
    }

    public class CoordData
    {
        public double Md { get; set; }
        public double Tvd { get; set; }
        public double Dx { get; set; }
        public double Dy { get; set; }
    }

    public class WellTrajData
    {
        public List<MetaInfo> MetaInfoList { get; set; } = new List<MetaInfo>();
        public long WellId { get; set; }
        public double WellMd { get; set; }
        public bool IsAzimuth { get; set; }
        public List<AimuthData> AimuthList { get; set; } = new List<AimuthData>();
        public List<CoordData> CoordList { get; set; } = new List<CoordData>();
    }

    public class WellTrajRequest
    {
        public List<WellTrajData> Items { get; set; } = new List<WellTrajData>();
    }



    public class DailyData
    {
        public string MeasureDate { get; set; }
        public double Bhp { get; set; }
        public double OilVol { get; set; }
        public double GasVol { get; set; }
        public double WaterVol { get; set; }
        public double WaterInjVol { get; set; }
        public double GasInjVol { get; set; }
        public double Hours { get; set; }
        public double Thp { get; set; }
        public double Chp { get; set; }
        public double Tht { get; set; }
        public double InjectOH { get; set; }
        public double InstTemp { get; set; }
        public double InstPress { get; set; }
        public string Remark { get; set; }
    }

    public class WellDailyProductionData
    {
        public List<MetaInfo> MetaInfoList { get; set; } = new List<MetaInfo>();
        public long WellId { get; set; }
        public List<DailyData> DailyList { get; set; } = new List<DailyData>();
    }

    public class WellProductionDataRequest
    {
        public List<WellDailyProductionData> Items { get; set; } = new List<WellDailyProductionData>();
    }



    public class GasTestData
    {
        public string Id { get; set; }
        public string WellId { get; set; }
        public string WellName { get; set; }
        public int Freq { get; set; }
        public string Sd { get; set; } // Start Date
        public string Ed { get; set; } // End Date
        public string Gty { get; set; } // Gas Test Type
        public string Dod { get; set; } // Date of Data
        public string Interval { get; set; } // Interval/层位
        public double Bsp { get; set; } // Bottom Shut-in Pressure 关井井底压力
        public double Bfp { get; set; } // Bottom Flowing Pressure 流动井底压力
        public double Pc { get; set; } // Pressure Coefficient 压力系数
        public double Mst { get; set; } // Measured Surface Temperature 实测地表温度
        public double Mft { get; set; } // Measured Formation Temperature 实测地层温度
        public double Wpr { get; set; } // Wellhead Pressure 井口压力
        public double Ufr { get; set; } // Unstable Flow Rate 不稳定流量
        public double Wp { get; set; } // Water Production 产水量
        public double H2S { get; set; } // H2S Content 硫化氢含量
        public double Ac { get; set; } // Acid Content 酸含量
        public double Oc { get; set; } // Oil Content 含油量
        public double Sal { get; set; } // Salinity 矿化度
        public double Bc { get; set; } // Base Content 基底含量
        public double Bfv { get; set; } // Base Flow Velocity 基底流速
        public double Bfr { get; set; } // Base Flow Rate 基底流量
        public string Dm { get; set; } // Data Method 数据方法
        public string Remarks { get; set; }
        public string CreateTime { get; set; }
        public string UpdateTime { get; set; }
        public string UpdateUserName { get; set; }
        public string CurveFileName { get; set; }
        public string ReportFileName { get; set; }
        public string MatchingFracturingParameters { get; set; }
        public string CurrentState { get; set; }
    }

    public class WellGasTestData
    {
        public List<MetaInfo> MetaInfoList { get; set; } = new List<MetaInfo>();
        public long WellId { get; set; }
        public List<GasTestData> GasTestList { get; set; } = new List<GasTestData>();
    }

    public class WellGasTestRequest
    {
        public List<WellGasTestData> Items { get; set; } = new List<WellGasTestData>();
    }


    public class OilTestData
    {
        public string Id { get; set; }
        public string WellId { get; set; }
        public string WellName { get; set; }
        public string TestWellSection { get; set; }  // 试油井段
        public string Sequence { get; set; }          // 试油序号
        public string Layer { get; set; }             // 层位
        public int LayerCount { get; set; }           // 层数
        public string StartDate { get; set; }       // 开始日期
        public string EndDate { get; set; }         // 结束日期
        public double Thickness { get; set; }        // 厚度
        public double ChokeSize { get; set; }        // 油嘴尺寸
        public double TubingPressure { get; set; }   // 油管压力
        public double CasingPressure { get; set; }   // 套管压力
        public double PumpingDepth { get; set; }     // 抽油深度
        public int PumpingCount { get; set; }     // 抽油次数
        public double MinWorkingLiquidLevel { get; set; }  // 最小工作液面
        public double MaxWorkingLiquidLevel { get; set; }  // 最大工作液面
        public double OilAmountPerDay { get; set; }        // 日产油量
        public double GasAmountPerDay { get; set; }        // 日产气量
        public double WaterAmountPerDay { get; set; }      // 日产水量
        public double AccumOilTotal { get; set; }          // 累计产油量
        public int TestDays { get; set; }                   // 试油天数
        public string Conclusion { get; set; }              // 结论
        public double ViscosityAt50 { get; set; }          // 50℃粘度
        public double OilDensity { get; set; }             // 原油密度
        public double ChlorineRoot { get; set; }           // 氯根含量
        public double TotalDissolvedSolids { get; set; }   // 总溶解固体
        public string WaterType { get; set; }               // 水性
        public double StaticTemp { get; set; }             // 静态温度
        public double StaticTempGradient { get; set; }     // 静态温度梯度
        public double FlowTemp { get; set; }               // 流动温度
        public double FlowTempGradient { get; set; }       // 流动温度梯度
        public double FlowPressure { get; set; }           // 流动压力
        public double FlowPressureGradient { get; set; }   // 流动压力梯度
        public double StaticPressure { get; set; }         // 静态压力
        public double StaticPressureGradient { get; set; } // 静态压力梯度
        public double FitFormationPressure { get; set; }   // 拟合地层压力
        public string UpdateTime { get; set; }            // 更新时间
        public int UpdateUserId { get; set; }               // 更新用户ID
        public string UpdateUserName { get; set; }          // 更新用户名
    }

    public class WellOilTestData
    {
        public List<MetaInfo> MetaInfoList { get; set; } = new List<MetaInfo>();
        public long WellId { get; set; }
        public List<OilTestData> OilTestList { get; set; } = new List<OilTestData>();
    }

    public class WellOilTestDataRequset
    {
        public List<WellOilTestData> Items { get; set; } = new List<WellOilTestData>();
    }


     public class CreatePayzoneRequest
    {
        public int DatasetType { get; set; }
        public string DatasetName { get; set; }
        public List<SymbolMappingDto> SymbolMapping { get; set; } = new List<SymbolMappingDto>();
        public bool IncludeItemResults { get; set; }
        public List<DatasetItemDto> Items { get; set; } = new List<DatasetItemDto>();
    }

    public class SymbolMappingDto
    {
        public int Color { get; set; }
        public string ConclusionName { get; set; }
        public string SymbolLibraryCode { get; set; }
    }

    public class DatasetItemDto
    {
        public List<MetaInfo> MetaInfoList { get; set; } = new List<MetaInfo>();
        public int WellId { get; set; }
        public List<ConclusionDto> ConclusionList { get; set; } = new List<ConclusionDto>();
    }



    public class ConclusionDto
    {
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public string ConclusionName { get; set; }
        public int Color { get; set; }
        public string SymbolLibraryCode { get; set; }
    }
}
