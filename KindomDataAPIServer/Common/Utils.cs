using DevExpress.Mvvm;
using DevExpress.Utils.About;
using KindomDataAPIServer.Models;
using Newtonsoft.Json;
using Smt;
using Smt.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Tet.Transport.Protobuf.Metaobjs;

namespace KindomDataAPIServer.Common
{
    public static class Utils
    {
        public static ISplashScreenManagerService WaitIndicatorService
        {
            get
            {
                var ret = ServiceContainer.Default.GetService<ISplashScreenManagerService>("TetWaitIndicatorService");
                if (ret != null)
                {
                    ret.ViewModel = new DXSplashScreenViewModel { Status = "请等待" };
                }
                return ret;
            }
        }
        public static double ToMeters(this double feet) => feet * 0.3048;

        public static List<UnitType> UnitTypes = new List<UnitType>();
        public static List<LogDictItem> LogDicts = new List<LogDictItem>();


        private static List<UnitInfo> _OilOrWaterInfos;
        public static List<UnitInfo> OilOrWaterInfos
        {
            get
            {
                if (_OilOrWaterInfos == null)
                {
                    _OilOrWaterInfos = new List<UnitInfo>();
                    var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 21);
                    if (type != null)
                        _OilOrWaterInfos = type.UnitInfoList;
                }
                return _OilOrWaterInfos;
            }
        }

        private static List<UnitInfo> _GasUnitInfos;
        public static List<UnitInfo> GasUnitInfos
        {
            get
            {
                if (_GasUnitInfos==null)
                {
                    _GasUnitInfos = new List<UnitInfo>();
                    var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 25);
                    if (type != null)
                        _GasUnitInfos = type.UnitInfoList;
                }
                return _GasUnitInfos;
            }
        }

        private static List<UnitInfo> _PressureUnitInfos;
        public static List<UnitInfo> PressureUnitInfos
        {
            get
            {
                if (_PressureUnitInfos == null)
                {
                    _PressureUnitInfos = new List<UnitInfo>();
                    var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 32);
                    if (type != null)
                        _PressureUnitInfos = type.UnitInfoList;
                }
                return _PressureUnitInfos;
            }
        }

        private static List<UnitInfo> _ChokeUnitInfos;
        public static List<UnitInfo> ChokeUnitInfos
        {
            get
            {
                if (_ChokeUnitInfos == null)
                {
                    _ChokeUnitInfos = new List<UnitInfo>();
                    var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 90);
                    if (type != null)
                        _ChokeUnitInfos = type.UnitInfoList;
                }
                return _ChokeUnitInfos;
            }
        }

        private static List<UnitInfo> _TemperatureUnitInfos;
        public static List<UnitInfo> TemperatureUnitInfos
        {
            get
            {
                if (_TemperatureUnitInfos == null)
                {
                    _TemperatureUnitInfos = new List<UnitInfo>();
                    var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 7);
                    if (type != null)
                        _TemperatureUnitInfos = type.UnitInfoList;
                }
                return _TemperatureUnitInfos;
            }
        }


        public static LogDictItem GetLogDictByName(string curveType, string curveName)
        {
            foreach (var item in LogDicts)
            {
                if(item.FamilyName == curveType)
                {
                    if( item.CurveList.Contains(curveName))
                        return item;           
                }
            }
            curveType = "Generic Curve";
            foreach (var item in LogDicts)
            {
                if (item.FamilyName == curveType)
                {
                    return item;
                }
            }

            return null;
        }

        public static UnitInfo GetDepthOrXYUnit(bool IsFt)
        {
            UnitInfo measureUnit = new UnitInfo() { MeasureID = 4 };
            var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 4);
            if (type != null)
            {
                string abbr = IsFt ? "ft" : "m";
                var info = type.UnitInfoList.FirstOrDefault(o => o.Abbr == abbr);
                if (info != null)
                {
                    measureUnit = info;                 
                }
            }
            return measureUnit;
        }


        public static UnitInfo GetOilOrWaterUnit(bool IsFt)
        {
            UnitInfo measureUnit = new UnitInfo() { MeasureID = 21 };
            var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 21);
            if (type != null)
            {
                string abbr = IsFt ? "bbl" : "m3";
                var info = type.UnitInfoList.FirstOrDefault(o => o.Abbr == abbr);
                if (info != null)
                {
                    measureUnit = info;
                }
            }
            return measureUnit;
        }

        public static UnitInfo GetGasUnit(bool IsFt)
        {
            UnitInfo measureUnit = new UnitInfo() { MeasureID = 25 };
            var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 25);
            if (type != null)
            {
                //string abbr = IsFt ? "mcf" : "m3";
                string abbr = IsFt ? "ft3" : "m3";
                var info = type.UnitInfoList.FirstOrDefault(o => o.Abbr == abbr);
                if (info != null)
                {
                    measureUnit = info;
                }
            }
            return measureUnit;
        }

        public static int ColorToInt(Color color)
        {
            int argb = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            return argb;
        }

        public static Project CreateProject(string path)
        {
            Project project = null;
            int tryCount = 0;
            start:
            try
            {
                tryCount++;
                project = new Project(path);
            }
            catch (UnauthorizedAccessException ex)//有时候第一次会报这个错，再试一次就好了
            {

                if (tryCount < 100)
                    goto start;
                else
                    throw ex;
            }
            if (tryCount > 3)
            {
                LogManagerService.Instance.Log($"Opened project at {path} Count:{tryCount}");
            }
            return project;
        }

        public static ApiConfig ParseUri(string uri)
        {
            try
            {
                // 方法1：使用字符串操作提取JSON
                int startIndex = uri.IndexOf("{");
                int endIndex = uri.LastIndexOf("}");

                if (startIndex == -1 || endIndex == -1)
                {
                    throw new ArgumentException("URI中未找到有效的JSON数据");
                }

                string jsonString = uri.Substring(startIndex, endIndex - startIndex + 1);

                // 方法2：使用正则表达式（更健壮）
                // var match = Regex.Match(uri, @"kingdomapi://(\{.*\})/");
                // if (!match.Success)
                // {
                //     throw new ArgumentException("URI格式不正确");
                // }
                // string jsonString = match.Groups[1].Value;

                // 使用 Newtonsoft.Json 反序列化
                var apiData = JsonConvert.DeserializeObject<ApiConfig>(jsonString);

                if (apiData == null)
                {
                    throw new ArgumentException("JSON反序列化失败");
                }

                return apiData;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"JSON解析失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"解析URI失败: {ex.Message}", ex);
            }
        }

        public static long GetWellIDByWellUWI(string wellUwi, PbViewMetaObjectList WellIDandNameList)
        {
            if (WellIDandNameList != null&& !string.IsNullOrEmpty(wellUwi))
            {
                var obj = WellIDandNameList.MetaObjects.FirstOrDefault(o => o.Name == wellUwi);
                if (obj != null)
                {
                    long id = 0;
                    long.TryParse(obj.Id, out id);
                    return id;
                }
            }
            return -1;
        }

        public static string GetWellNameOrUWIByWellID(string wellID, PbViewMetaObjectList WellIDandNameList)
        {
            if (WellIDandNameList != null && !string.IsNullOrEmpty(wellID))
            {
                var obj = WellIDandNameList.MetaObjects.FirstOrDefault(o => o.Id == wellID);
                if (obj != null)
                {
                    return obj.Name;
                }
            }
            return "";
        }
    }
}

