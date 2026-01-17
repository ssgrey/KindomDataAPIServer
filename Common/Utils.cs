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
    public class Utils
    {
        public static List<UnitType> UnitTypes = new List<UnitType>();

        public static MeasureUnit GetDepthOrXYUnit(bool IsFt)
        {
            MeasureUnit measureUnit = new MeasureUnit() { MeasureID = 4 };
            var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 4);
            if (type != null)
            {
                string abbr = IsFt ? "ft" : "m";
                var info = type.UnitInfoList.FirstOrDefault(o => o.Abbr == abbr);
                if (info != null)
                {
                    measureUnit.UnitId = info.Id;
                    measureUnit.Unit = info.Abbr;
                }
            }
            return measureUnit;
        }


        public static MeasureUnit GetOilOrWaterUnit(bool IsFt)
        {
            MeasureUnit measureUnit = new MeasureUnit() { MeasureID = 21 };
            var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 21);
            if (type != null)
            {
                string abbr = IsFt ? "bbl" : "m3";
                var info = type.UnitInfoList.FirstOrDefault(o => o.Abbr == abbr);
                if (info != null)
                {
                    measureUnit.UnitId = info.Id;
                    measureUnit.Unit = info.Abbr;
                }
            }
            return measureUnit;
        }

        public static MeasureUnit GetGasUnit(bool IsFt)
        {
            MeasureUnit measureUnit = new MeasureUnit() { MeasureID = 25 };
            var type = UnitTypes.FirstOrDefault(o => o.UnitTypeID == 25);
            if (type != null)
            {
                //string abbr = IsFt ? "mcf" : "m3";
                string abbr = IsFt ? "ft3" : "m3";
                var info = type.UnitInfoList.FirstOrDefault(o => o.Abbr == abbr);
                if (info != null)
                {
                    measureUnit.UnitId = info.Id;
                    measureUnit.Unit = info.Abbr;
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
    }
}

