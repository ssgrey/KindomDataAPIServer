using DevExpress.Spreadsheet.Functions;
using Google.Protobuf;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.Models;
using Smt;
using Smt.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;

namespace KindomDataAPIServer.KindomAPI
{
    public class KingdomAPI
    {
        private static KingdomAPI _instance = null;
        public static KingdomAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new KingdomAPI();
                }
                return _instance;
            }
        }

        private Project project = null;
        private string ProjectPath { get; set; } = "";

        private string CurrentLoginName { get; set; } = "";

        public void SetProjectPath(string projectPath)
        {
            try
            {
                ProjectPath = projectPath;
                if (project != null)
                {
                    project.Dispose();
                }
                project = Utils.CreateProject(ProjectPath);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"SetProjectPath failed: {ex.Message}");
            }
        }

        public bool LoginOn(string loginName)
        {
            try
            {
                if (project == null)
                {
                    project = Utils.CreateProject(ProjectPath);
                }
                CurrentLoginName = loginName;
                project.LogOn(CurrentLoginName);
                return true;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"Project LogOn failed: {ex.Message}");
            }
            return false;
        }

        public void Close()
        {
            if (project != null)
            {
                project.Dispose();
            }
        }

        public List<string> GetProjectAuthors()
        {
            List<string> list = new List<string>();
            list.Add("Public");
            var res = project.Authors.Select(o => o.Name).Except(list).ToList();
            return res;
        }

        public static ProjectResponse ExportProject(string projectPath, string loginName)
        {
            var project = Utils.CreateProject(projectPath);
            var res = project.Authors.FirstOrDefault(o => o.Name == loginName);
            if (res != null)
            {
                project.LogOn(loginName);
            }
            else
            {
                project.LogOn(project.Authors.LastOrDefault().Name);
            }

            var mapUnit = project.MapUnit.ToString();
            var verticalUnit = project.VerticalUnit.ToString();

            using (var context = project.GetKingdom())
            {
                var boreholes = context.Get(new Borehole(), b => new
                {
                    BoreholeId = b.Id,
                    BoreholeName = b.Name,
                    Uwi = b.Uwi,
                    WellId = b.Well.Id,
                    WellName = b.Well.Name,
                    WellNumber = b.Well.WellNumber,
                    SurfaceX = b.Well.SurfaceLocX,
                    SurfaceY = b.Well.SurfaceLocY,
                    Country = b.Well.Country.Name,
                    State = b.Well.State.Name,
                    County = b.Well.County.Name
                },
                    _ => true,
                    false).ToList();

                var boreholeIds = boreholes.Select(b => b.BoreholeId).ToList();

                var digitalLogs = context.Get(new LogData(),
                    x => new
                    {
                        LogData = x,
                        LogCurveName = x.LogCurveName,
                    },
                    x => boreholeIds.Contains(x.BoreholeId),
                    false).ToList();

                var wells = new List<WellExport>();
                foreach (var bh in boreholes)
                {
                    var logs = digitalLogs.Where(l => l.LogData.BoreholeId == bh.BoreholeId)
                        .Select(l => new DigitalLogExport
                        {
                            Id = l.LogData.Id,
                            Name = l.LogCurveName.Name,
                            NameId = l.LogCurveName.Id,
                            SampleRate = l.LogData.DepthSampleRate,
                            StartDepth = l.LogData.StartDepth,
                            Count = l.LogData.ValuesCount,

                        }).ToList();

                    //wells.Add(new WellExport
                    //{
                    //    BoreholeId = bh.BoreholeId,
                    //    BoreholeName = bh.BoreholeName,
                    //    WellId = bh.WellId,
                    //    WellName = bh.WellName,
                    //    Uwi = bh.Uwi,
                    //    WellNumber = bh.WellNumber,
                    //    SurfaceX = bh.SurfaceX,
                    //    SurfaceY = bh.SurfaceY,
                    //    Country = bh.Country,
                    //    MapUnit = mapUnit,
                    //    VerticalUnit = verticalUnit,
                    //    DigitalLogs = logs
                    //});
                }

                return new ProjectResponse
                {
                    ProjectPath = projectPath,
                    MapUnit = mapUnit,
                    VerticalUnit = verticalUnit,
                    Wells = wells
                };
            }
        }

        public ProjectResponse GetProjectData()
        {
            var mapUnit = project.MapUnit.ToString();
            var verticalUnit = project.VerticalUnit.ToString();
            using (var context = project.GetKingdom())
            {
                var boreholes = context.Get(new Borehole(), b => new
                {
                    BoreholeId = b.Id,
                    BoreholeName = b.Name,
                    Uwi = b.Uwi,
                    WellId = b.Well.Id,
                    WellName = b.Well.Name,
                    WellNumber = b.Well.WellNumber,
                    SurfaceX = b.Well.SurfaceLocX,
                    SurfaceY = b.Well.SurfaceLocY,
                    Latitude = b.Well.Latitude,
                    Longitude = b.Well.Longitude,
                    Country = b.Well.Country.Name,
                    State = b.Well.State.Name,
                    County = b.Well.County.Name,
                   
                },
                    _ => true,
                    false).ToList();

                var boreholeIds = boreholes.Select(b => b.BoreholeId).ToList();

                var digitalLogs = context.Get(new LogData(),
                    x => new
                    {
                        LogData = x,
                        LogCurveName = x.LogCurveName,
                    },
                    x => boreholeIds.Contains(x.BoreholeId),
                    false).ToList();

                var formations = context.Get(new FormationTopPick(),
                        x => new
                        {
                            FormationTop = x,
                            FormationTopName = x.FormationTopName
                        },
                        x => boreholeIds.Contains(x.BoreholeId),
                        false).ToList();


                var wells = new List<WellExport>();
                foreach (var bh in boreholes)
                {
                    var logs = digitalLogs.Where(l => l.LogData.BoreholeId == bh.BoreholeId)
                        .Select(l => new DigitalLogExport
                        {
                            Id = l.LogData.Id,
                            Name = l.LogCurveName.Name,
                            NameId = l.LogCurveName.Id,
                            SampleRate = l.LogData.DepthSampleRate,
                            StartDepth = l.LogData.StartDepth,
                            Count = l.LogData.ValuesCount,

                        }).ToList();

                    wells.Add(new WellExport
                    {
                        BoreholeId = bh.BoreholeId,
                        BoreholeName = bh.BoreholeName,
                        WellId = bh.WellId,
                        WellName = bh.WellName,
                        Uwi = bh.Uwi,
                        WellNumber = bh.WellNumber,
                        SurfaceX = bh.SurfaceX.HasValue ? bh.SurfaceX.Value : 0,
                        SurfaceY = bh.SurfaceY.HasValue ? bh.SurfaceY.Value : 0,
                        Latitude = bh.Latitude.HasValue ? bh.Latitude.Value : 0,
                        Longitude = bh.Longitude.HasValue ? bh.Longitude.Value : 0,
                        Country = bh.Country,
                        Region = bh.State,
                        Districts = bh.County,
                        MapUnit = mapUnit,
                        VerticalUnit = verticalUnit,
                        DigitalLogs = logs
                    });
                }



                var logNames = context.Get(new LogCurveName(),
                    x => new CheckNameExport
                    {
                        Name = x.Name,
                    },
                    x => true,
                    false).ToList();

                var formationNames = context.Get(new FormationName(),
                     x => new CheckNameExport
                     {
                         Name = x.Name,
                     },
                     x => true,
                     false).ToList();
                formationNames = formationNames.Where(o=>!string.IsNullOrEmpty(o.Name)).ToList();
                return new ProjectResponse
                {
                    ProjectPath = this.ProjectPath,
                    MapUnit = mapUnit,
                    VerticalUnit = verticalUnit,
                    Wells = wells,
                    FormationNames = formationNames,
                    LogNames = logNames
                };
            }
        }


        public ProjectResponse GetProjectWellData()
        {
            var mapUnit = project.MapUnit.ToString();
            var verticalUnit = project.VerticalUnit.ToString();

            using (var context = project.GetKingdom())
            {
                var boreholes = context.Get(new Borehole(), b => new
                {
                    BoreholeId = b.Id,
                    BoreholeName = b.Name,
                    Uwi = b.Uwi,
                    WellId = b.Well.Id,
                    WellName = b.Well.Name,
                    WellNumber = b.Well.WellNumber,
                    SurfaceX = b.Well.SurfaceLocX,
                    SurfaceY = b.Well.SurfaceLocY,
                    Country = b.Well.Country.Name,
                    State = b.Well.State.Name,
                    County = b.Well.County.Name
                },
                    _ => true,
                    false).ToList();

                var boreholeIds = boreholes.Select(b => b.BoreholeId).ToList();

                var wells = new List<WellExport>();
                foreach (var bh in boreholes)
                {
                    //wells.Add(new WellExport
                    //{
                    //    BoreholeId = bh.BoreholeId,
                    //    BoreholeName = bh.BoreholeName,
                    //    WellId = bh.WellId,
                    //    WellName = bh.WellName,
                    //    Uwi = bh.Uwi,
                    //    WellNumber = bh.WellNumber,
                    //    SurfaceX = bh.SurfaceX,
                    //    SurfaceY = bh.SurfaceY,
                    //    Country = bh.Country,
                    //    MapUnit = mapUnit,
                    //    VerticalUnit = verticalUnit,
                    //});
                }

                return new ProjectResponse
                {
                    ProjectPath = this.ProjectPath,
                    MapUnit = mapUnit,
                    VerticalUnit = verticalUnit,
                    Wells = wells
                };
            }
        }

        public WellExport GetWellExportByUWI(string UWI)
        {
            WellExport res = null;
            var mapUnit = project.MapUnit.ToString();
            var verticalUnit = project.VerticalUnit.ToString();

            using (var context = project.GetKingdom())
            {
                var boreholes = context.Get(new Borehole(), b => new WellExport
                {
                    BoreholeId = b.Id,
                    BoreholeName = b.Name,
                    Uwi = b.Uwi,
                    WellId = b.Well.Id,
                    WellName = b.Well.Name,
                    WellNumber = b.Well.WellNumber,
                    //SurfaceX = b.Well.SurfaceLocX,
                    //SurfaceY = b.Well.SurfaceLocY,
                    Country = b.Well.Country.Name,
                },
                   x => x.Uwi == UWI,
                    false).ToList();

                res = boreholes.FirstOrDefault();
            }
            return res;
        }
        public List<DigitalLogExport> GetWellLog(int BoreholeId)
        {
            List<DigitalLogExport> digitalLogExports = new List<DigitalLogExport>();
            using (var context = project.GetKingdom())
            {
                var digitalLogs = context.Get(new LogData(),
                    x => new
                    {
                        LogData = x,
                        LogCurveName = x.LogCurveName,
                    },
                    x => BoreholeId == x.BoreholeId,
                    false).ToList();
            }

            return digitalLogExports;
        }

        public PbWellFormationList GetWellFormation(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList)
        {
            List<WellExport> Wells = KingDomData.Wells;
           var checkNames =   KingDomData.FormationNames.Where(o => o.IsChecked).Select(o=>o.Name).ToList();
            List<int> BoreholeIds = Wells.Where(o=>o.IsChecked).Select(o => o.BoreholeId).ToList();
            PbWellFormationList pbWellFormationList = new PbWellFormationList();

            List<FormationTopPick> digitalLogExports = new List<FormationTopPick>();
            using (var context = project.GetKingdom())
            {
                var formations = context.Get(new FormationTopPick(),
                    x => new
                    {
                        FormationTop = x,
                        FormationTopName = x.FormationTopName,
                        borehole = x.Borehole,                   
                        boreholeId = x.BoreholeId,
                        wellUWI = x.Borehole.Uwi,
                       
                    },
                    x => BoreholeIds.Contains(x.BoreholeId),
                    false).ToList();
                var dicts = formations.GroupBy(o => o.wellUWI).ToDictionary(a=>a.Key,a=>a.ToList());
                foreach (var item in dicts)
                {
                    long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                    if (wellWebID == -1)
                        continue;
                    PbWellFormation pbWellFormation = new PbWellFormation
                    {
                         WellId = wellWebID
                    };

                    foreach (var formItem in item.Value)
                    {
                        if (formItem.FormationTop.Depth.HasValue)
                        {
                            if (!checkNames.Contains(formItem.FormationTopName.Name))
                                continue;
                            var res = pbWellFormation.Items.FirstOrDefault(o => (o.Name == formItem.FormationTopName.Name || o.Name == formItem.FormationTopName.Abbreviation) && o.Top == formItem.FormationTop.Depth.Value);
                            if (res == null)
                            {
                                pbWellFormation.Items.Add(new PbFormationItem()
                                {
                                    Name = formItem.FormationTopName.Name,
                                    Top = formItem.FormationTop.Depth.Value,
                                    Bottom = formItem.FormationTop.Depth.Value,                                   
                                });
                            }
                        }
                    }

                    if (pbWellFormation.Items.Count > 0)
                    {
                        pbWellFormationList.Datas.Add(pbWellFormation);
                    }
                }
            }

            return pbWellFormationList;
        }


        public PbWellLogCreateList GetWellLogs(ProjectResponse KingDomData,string resdataSetID, PbViewMetaObjectList WellIDandNameList)
        {
            long dataSetId = long.Parse( resdataSetID);
            List<WellExport> Wells = KingDomData.Wells;
            var checkNames = KingDomData.LogNames.Where(o => o.IsChecked).Select(o => o.Name).ToList();
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();
            PbWellLogCreateList logList = new PbWellLogCreateList();
         
            List<LogData> digitalLogExports = new List<LogData>();
            using (var context = project.GetKingdom())
            {
                var formations = context.Get(new LogData(),
                    x => new
                    {
                        LogData = x,
                        LogCurveName = x.LogCurveName,
                        borehole = x.Borehole,
                        boreholeId = x.BoreholeId,
                        wellUWI = x.Borehole.Uwi,

                    },
                    x => BoreholeIds.Contains(x.BoreholeId),
                    false).ToList();
                var dicts = formations.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());
                foreach (var item in dicts)
                {
                    long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                    if (wellWebID == -1)
                        continue;
  
                    foreach (var formItem in item.Value)
                    {
                        if (formItem.LogData!=null)
                        {
                            if (!checkNames.Contains(formItem.LogCurveName.Name))
                                continue;
                            // var res = pbWellFormation.Items.FirstOrDefault(o => (o.Name == formItem.FormationTopName.Name || o.Name == formItem.FormationTopName.Abbreviation) && o.Top == formItem.FormationTop.Depth.Value);
                            // if (res == null)

                            var dataArray = formItem.LogData.LogDataValues.Select(o=>(double)o).ToArray();
                            PbWellLogCreateParams logObj =new PbWellLogCreateParams
                            {
                                WellId = wellWebID,
                                SampleRate = formItem.LogData.DepthSampleRate.HasValue ? formItem.LogData.DepthSampleRate.Value : 0,
                                StartDepth = formItem.LogData.StartDepth.HasValue ? formItem.LogData.StartDepth.Value : 0,
                                CurveName = formItem.LogCurveName.Name,
                                 DataSetId = dataSetId,
                            };
                            logObj.Samples.AddRange(dataArray);

                            logList.LogList.Add(logObj);
                        }
                    }
                   
                }
            }

            return logList;
        }

        

        /// <summary>
        /// 创建或更新井数据
        /// </summary>
        /// <param name="wellExport"></param>
        /// <returns></returns>
        public bool CreateOrUpdateWell(WellExport wellExport)
        {
            try
            {
                var bore1 = new Borehole(CRUDOption.CreateOrUpdate)
                {
                    Uwi = wellExport.Uwi,
                    Name = wellExport.BoreholeName,
                    
                };
                bore1.Well = new Well
                {                   
                    Name = wellExport.WellName,
                    WellNumber = wellExport.WellNumber,
                    SurfaceLocX = wellExport.SurfaceX,
                    SurfaceLocY = wellExport.SurfaceY,
                    Country = new Country { Name = wellExport.Country, EntityCRUDOption = CRUDOption.CreateOrUpdate },
                };
                using (var context = project.GetKingdom())
                {
                    context.AddObject(bore1);                   
                    context.SaveChanges();
                };
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"CreateOrUpdateWell failed: {ex.Message}");
                return false;
            }
            return true;
        }


        public bool CreateOrUpdateWellLog(string wellUWI)
        {
            try
            {
                List<LogCurveName> logNames = new List<LogCurveName>();

                Borehole borehole = null;
                using (var context = project.GetKingdom())
                {
                    borehole = context.Get<Borehole>(o => o.Uwi == wellUWI, false).FirstOrDefault();
                    logNames = context.Get<LogCurveName>(null, false).ToList();            
                };

                LogData logData = new LogData(CRUDOption.CreateOrUpdate)
                {
                    Borehole = borehole,
                    LogCurveName = new LogCurveName { Name = "RHOB", EntityCRUDOption = CRUDOption.CreateOrUpdate },
                    DepthSampleRate = 0.125,
                    StartDepth = 0,                 
                };

                FormationTopPick formationTopPick = new FormationTopPick(CRUDOption.CreateOrUpdate)
                {
                    Borehole = borehole,
                    FormationTopName = new FormationTopName { Name = "TestFormation", EntityCRUDOption = CRUDOption.CreateOrUpdate },
                    Depth = 333,
                };
               
                var data = ReadFirstTwoColumns();
                logData.SetLogDepthsAndValues(data.firstColumn, data.secondColumn);
                using (var context = project.GetKingdom())
                {
                    context.AddObject(logData);
                    context.AddObject(formationTopPick);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"CreateOrUpdateWell failed: {ex.Message}");
                return false;
            }
            return true;
        }

        public (float[] firstColumn, float[] secondColumn) ReadFirstTwoColumns()
        {
            string filePath = "C:\\Users\\Administrator\\Desktop\\test.txt";
            List<float> col1 = new List<float>();
            List<float> col2 = new List<float>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // 使用StringSplitOptions.RemoveEmptyEntries移除空字段
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // 确保至少有两列数据
                    if (parts.Length >= 2)
                    {
                        var value2 = float.Parse(parts[1]);
                        if(value2 != -99999)
                        {
                            col1.Add(float.Parse(parts[0]));
                            col2.Add(value2);
                        }

                    }
                }
            }

            return (col1.ToArray(), col2.ToArray());
        }


    }
}
