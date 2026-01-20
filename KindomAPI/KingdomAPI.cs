using DevExpress.Mvvm;
using DevExpress.Spreadsheet.Functions;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid.Hierarchy;
using Google.Protobuf;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
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
using System.Windows.Media;
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;

namespace KindomDataAPIServer.KindomAPI
{
    public class KingdomAPI :BindableBase
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


        #region Unit


        private bool IsDepthFeet
        {
            get
            {
                if (project != null)
                {
                    if (project.VerticalUnit == DistanceUnit.Feet)
                    {
                        return true;
                    }
                }
                return true;
            }

        }

        private bool IsXYFeet
        {
            get
            {
                if (project != null)
                {
                    if (project.MapUnit == DistanceUnit.Feet)
                    {
                        return true;
                    }
                }
                return true;
            }

        }

        public UnitInfo DepthUnit
        {
            get
            {
                var depthUnit = Utils.GetDepthOrXYUnit(IsDepthFeet);
                return depthUnit;
            }
        }

        public UnitInfo XYUnit
        {
            get
            {
                var depthUnit = Utils.GetDepthOrXYUnit(IsXYFeet);
                return depthUnit;
            }
        }
        public UnitInfo OilOrWaterUnit
        {
            get
            {
                var Unit = Utils.GetOilOrWaterUnit(IsDepthFeet);
                return Unit;
            }
        }

        public UnitInfo GasUnit
        {
            get
            {
                var Unit = Utils.GetGasUnit(IsDepthFeet);
                return Unit;
            }
        }


        public UnitInfo _OilOrWaterInfo;
        public UnitInfo OilOrWaterInfo
        {
            get
            {
                return _OilOrWaterInfo;
            }
            set
            {
                SetProperty(ref _OilOrWaterInfo, value, nameof(OilOrWaterInfo));

            }
        }

        public UnitInfo _GasInfo;
        public UnitInfo GasInfo
        {
            get
            {
                return _GasInfo;
            }
            set
            {
                SetProperty(ref _GasInfo, value, nameof(GasInfo));

            }
        }

        public UnitInfo _ChokeSizeUnit;
        public UnitInfo ChokeSizeUnit
        {
            get
            {             
                return _ChokeSizeUnit;
            }
            set
            {
                SetProperty(ref _ChokeSizeUnit, value, nameof(ChokeSizeUnit));

            }
        }

        public UnitInfo _FlowingTubingPressureUnit;
        public UnitInfo FlowingTubingPressureUnit
        {
            get
            {
                return _FlowingTubingPressureUnit;
            }
            set
            {
                SetProperty(ref _FlowingTubingPressureUnit, value, nameof(FlowingTubingPressureUnit));

            }
        }


        public UnitInfo _BottomHoleTemperatureUnit;
        public UnitInfo BottomHoleTemperatureUnit
        {
            get
            {
                return _BottomHoleTemperatureUnit;
            }
            set
            {
                SetProperty(ref _BottomHoleTemperatureUnit, value, nameof(BottomHoleTemperatureUnit));

            }
        }


        private List<UnitInfo> _PressureUnitInfos;
        public List<UnitInfo> PressureUnitInfos
        {

            get
            {
                return _PressureUnitInfos;
            }
            set
            {
                SetProperty(ref _PressureUnitInfos, value, nameof(PressureUnitInfos));
            }
        }

        private List<UnitInfo> _ChokeUnitInfos;
        public List<UnitInfo> ChokeUnitInfos
        {
            get
            {
                return _ChokeUnitInfos;
            }
            set
            {
                SetProperty(ref _ChokeUnitInfos, value, nameof(ChokeUnitInfos));
            }
        }

        private List<UnitInfo> _TemperatureUnitInfos;
        public List<UnitInfo> TemperatureUnitInfos
        {
            get
            {
                return _TemperatureUnitInfos;
            }
            set
            {
                SetProperty(ref _TemperatureUnitInfos, value, nameof(TemperatureUnitInfos));
            }
        }
        #endregion

        public void SetProjectPath(string projectPath)
        {
            try
            {
                ProjectPath = projectPath;
                if (project != null)
                {
                    project.Dispose();
                }
                project = new Project(ProjectPath);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"SetProjectPath failed: {ex.Message}");
                LogManagerService.Instance.Log($"SetProjectPath failed: {ex.Message}");
            }
        }


        public List<string> LoginDB(string dbUser, string dbPassword)
        {
            try
            {
                if (project == null)
                {
                    LogManagerService.Instance.Log($"please select project path first!");
                    return null;
                }
                if (!string.IsNullOrWhiteSpace(dbUser))
                {
                    project.DBUserName = dbUser;
                }
                else
                {
                    project.DBUserName = "";
                }
                if (!string.IsNullOrWhiteSpace(dbPassword))
                {
                    project.SetPassword(dbPassword);
                }
                else
                {
                    project.SetPassword("");
                }

                var logons = project.AuthorizedLogOnAuthorNames ?? Enumerable.Empty<string>();
                var logonList = logons.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

                return logonList;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"LoginDB failed: {ex.Message}");
            }
            return null;
        }


        public bool LoadByUser(string loginName)
        {
            try
            {
                if (project == null)
                {
                    LogManagerService.Instance.Log($"please select project path first!");
                    return false;
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
            try
            {
                if (project != null)
                {
                    project.Dispose();
                }
            }
            catch(Exception ex)
            {
                LogManagerService.Instance.Log($"close failed: {ex.Message}");

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
            try
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
                        x => new 
                        {
                            Name = x.Name,
                            LogType = x.LogType,
                        },
                        x => true,
                        false).ToList();

                    List<CheckNameLog> checklogs = new List<CheckNameLog>();
                    foreach (var logName in logNames)
                    {
                        CheckNameLog checkNameLog = new CheckNameLog()
                        {
                            Name = logName.Name,
                        };
                        checkNameLog.LogType = Utils.GetLogDictByName(logName.LogType.Name, logName.Name);
                        checkNameLog.UnitID = checkNameLog.LogType == null? 0: checkNameLog.LogType.DbUnit;
                        checklogs.Add(checkNameLog);
                    }


                    var formationNames = context.Get(new FormationTopName(),
                         x => new CheckNameExport
                         {
                             Name = x.Name,
                         },
                         x => true,
                         false).ToList();




                    formationNames = formationNames.Where(o => !string.IsNullOrEmpty(o.Name)).ToList();
#if DEBUG

                    //var formationTopNames = context.Get(new FormationTopName(),
                    // x => new 
                    // {
                    //     data = x,
                    // },
                    // x => true,
                    // false).ToList();
                    // var IntervalRecords = context.Get(new Smt.Entities.IntervalRecord(),
                    //  x => new
                    //  {
                    //      data = x,
                    //  },
                    //  x => true,
                    //  false).ToList();

                    // var trajy = context.Get(new Smt.Entities.DeviationSurvey(),
                    //          x => new
                    //          {
                    //              data = x,
                    //          },
                    //          x => true,
                    //          false).ToList();

     //               var res = boreholes.FirstOrDefault(o => o.Uwi == "ZJ19H");

     //               var res2 = digitalLogs.FirstOrDefault(o => o.LogData.BoreholeId == res.BoreholeId);
     //               var ProductionEntitys = context.Get(new Smt.Entities.ProductionVolumeHistory(),
     //                        x => new
     //                        {
     //                            data = x,
     //                        },
     //                        x => x.BoreholeId == res.BoreholeId,
     //                        false).ToList();
     //               var IntervalRecord = context.Get(new Smt.Entities.IntervalRecord(),
     //                    x => new
     //                    {
     //                        data = x,
     //                        intervalName = x.IntervalName,
     //                        texts = x.IntervalTextValues
     //                    },
     //                     x => x.BoreholeId == res.BoreholeId,
     //                    false).ToList();

     //               var IntervalAttribute = context.Get(new Smt.Entities.IntervalTextValue(),
     //x => new
     //{
     //    data = x,
     //    intervalAttr = x.IntervalAttribute,
     //    id = x.IntervalRecordId
     //},
     // x => true,
     //false).ToList();

     //               var DeviationSurveys = context.Get(new Smt.Entities.DeviationSurvey(),
     //x => new
     //{
     //    data = x,
     //},
     // x => true,
     //false).ToList();

     //               var TestProduction11 = context.Get(new Smt.Entities.TestInitialPotential(),
     //                    x => new
     //                    {
     //                        data = x,
     //                    },
     //                     x => x.BoreholeId == res.BoreholeId,
     //                    false).ToList();
     //                               var TestProduction3 = context.Get(new Smt.Entities.TestProductionPerforation(),
     //                     x => new
     //                     {
     //                         data = x,
     //                     },
     //                      x => true,
     //                     false).ToList();

     //                               var TestProduction4 = context.Get(new Smt.Entities.ProductionEntity(),
     //               x => new
     //               {
     //                   data = x,
     //               },
     //               x => true,
     //               false).ToList();

     //                var TestProduction5 = context.Get(new Smt.Entities.ProducingField(),
     //               x => new
     //               {
     //                   data = x,
     //               },
     //               x => true,
     //               false).ToList();

     //               var survey = context.Get(new Smt.Entities.IntervalRecord(),
     //               x => new
     //               {
     //                   data = x,
     //               },
     //               x => true,
     //               false).ToList();
                #endif
                    return new ProjectResponse
                    {
                        ProjectPath = this.ProjectPath,
                        MapUnit = mapUnit,
                        VerticalUnit = verticalUnit,
                        Wells = wells,
                        FormationNames = formationNames,
                        LogNames = checklogs
                    };
                }

            }
            catch (Exception ex)
            {
                string res = ex.Message;
                if (ex.InnerException != null)
                {
                    res += ex.InnerException.Message;
                }
                LogManagerService.Instance.Log(res + ex.StackTrace);
            }
            return null;
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


        public List<WellTrajData> GetWellTrajs(ProjectResponse KingDomData,PbViewMetaObjectList WellIDandNameList)
        {
            List<WellTrajData> datas = new List<WellTrajData>();
            List<WellExport> Wells = KingDomData.Wells;
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

            List<MetaInfo> MetaInfoList = new List<MetaInfo>();
            MetaInfo metaInfo = new MetaInfo();
            metaInfo.DisplayName = "测深";
            metaInfo.PropertyName = "coordList.md";
            metaInfo.UnitId = DepthUnit.Id;
            metaInfo.MeasureId = DepthUnit.MeasureID;
            MetaInfoList.Add(metaInfo);
            MetaInfo metaInfo2 = new MetaInfo();
            metaInfo2.DisplayName = "垂深";
            metaInfo2.PropertyName = "coordList.tvd";
            metaInfo2.UnitId = DepthUnit.Id;
            metaInfo2.MeasureId = DepthUnit.MeasureID;
            MetaInfoList.Add(metaInfo2);

            MetaInfo metaInfo3 = new MetaInfo();
            metaInfo3.DisplayName = "dx";
            metaInfo3.PropertyName = "coordList.dx";
            metaInfo3.UnitId = XYUnit.Id;
            metaInfo3.MeasureId = XYUnit.MeasureID;
            MetaInfoList.Add(metaInfo3);


            MetaInfo metaInfo4 = new MetaInfo();
            metaInfo4.DisplayName = "dy";
            metaInfo4.PropertyName = "coordList.dy";
            metaInfo4.UnitId = XYUnit.Id;
            metaInfo4.MeasureId = XYUnit.MeasureID;
            MetaInfoList.Add(metaInfo4);

            using (var context = project.GetKingdom())
            {
                var DeviationSurveys = context.Get(new Smt.Entities.DeviationSurvey(),
                 x => new
                 {
                     borehole = x.Borehole,
                     boreholeId = x.BoreholeId,
                     wellUWI = x.Borehole.Uwi,
                     data = x,
                 },
                   x => BoreholeIds.Contains(x.BoreholeId),
                 false).ToList();

                var dicts = DeviationSurveys.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());
                foreach (var item in dicts)
                {
                    long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                    if (wellWebID == -1)
                        continue;

                    foreach (var formItem in item.Value)
                    {
                        if (formItem.data != null)
                        {
                            WellTrajData wellTrajData = new WellTrajData();
                            wellTrajData.MetaInfoList = MetaInfoList;
                            wellTrajData.WellId = wellWebID;
                            for (int i = 0; i < formItem.data.MD.Count; i++)
                            {
                                CoordData coordData = new CoordData()
                                {
                                    Md = formItem.data.MD[i],
                                    Tvd = formItem.data.TVD[i],
                                    Dx = formItem.data.DX[i],
                                    Dy = formItem.data.DY[i]
                                };

                                wellTrajData.CoordList.Add(coordData);
                                AimuthData aimuthData = new AimuthData()
                                {
                                    Md = formItem.data.MD[i],
                                    Azim = formItem.data.Azimuth[i],
                                    Devi = formItem.data.Inclination[i],
                                };
                                wellTrajData.AimuthList.Add(aimuthData);
                            }
                            datas.Add(wellTrajData);
                        }
                    }

                }
            }

            return datas;
        }

        public PbWellLogCreateList GetWellLogs(ProjectResponse KingDomData,string resdataSetID, PbViewMetaObjectList WellIDandNameList)
        {
            long dataSetId = long.Parse(resdataSetID);
            List<WellExport> Wells = KingDomData.Wells;
            var checkNames = KingDomData.LogNames.Where(o => o.IsChecked).Select(o => o.Name).ToList();
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

            List<List<int>> allBoreholeIds = new List<List<int>>();
            List<int> tempBoreholeIds = new List<int>();
            allBoreholeIds.Add(tempBoreholeIds);
            for (int i = 0; i < BoreholeIds.Count; i++)
            {
                if (tempBoreholeIds.Count < 10)
                {
                    tempBoreholeIds.Add(BoreholeIds[i]);
                }
                else
                {
                    tempBoreholeIds = new List<int>();
                    allBoreholeIds.Add(tempBoreholeIds);
                    tempBoreholeIds.Add(BoreholeIds[i]);
                }
            }

            PbWellLogCreateList logList = new PbWellLogCreateList();
         
            List<LogData> digitalLogExports = new List<LogData>();


            foreach (var tempids in allBoreholeIds)
            {
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
                        x => tempids.Contains(x.BoreholeId),
                        false).ToList();
                    var dicts = formations.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());
                    foreach (var item in dicts)
                    {
                        long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                        if (wellWebID == -1)
                            continue;

                        foreach (var formItem in item.Value)
                        {
                            if (formItem.LogData != null)
                            {
                                if (!checkNames.Contains(formItem.LogCurveName.Name))
                                    continue;
                                Console.WriteLine("count" + formItem.LogData.ValuesCount);
                                var dataArray = formItem.LogData.LogDataValues.Select(o => (double)o);
                                PbWellLogCreateParams logObj = new PbWellLogCreateParams
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

            }

            return logList;
        }

        public async Task CreateWellLogsToWeb(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList, string resdataSetID)
        {
            var wellDataService = ServiceLocator.GetService<IDataWellService>();

            long dataSetId = long.Parse(resdataSetID);
            List<WellExport> Wells = KingDomData.Wells;
            var checkNames = KingDomData.LogNames.Where(o => o.IsChecked).Select(o => o.Name).ToList();
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();
            List<LogData> digitalLogExports = new List<LogData>();

            int index = 1;
            foreach (var borID in BoreholeIds)
            {
                using (var context = project.GetKingdom())
                {
                    PbWellLogCreateList logList = new PbWellLogCreateList();
                    var formations = context.Get(new LogData(),
                        x => new
                        {
                            LogData = x,
                            LogCurveName = x.LogCurveName,
                            LogType = x.LogCurveName.LogType,
                            borehole = x.Borehole,
                            boreholeId = x.BoreholeId,
                            wellUWI = x.Borehole.Uwi,                         
                        },
                        x => x.BoreholeId == borID,
                        false).ToList();
                    var dicts = formations.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());//只有一个key  暂时的
                    foreach (var item in dicts)
                    {
                        long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                        if (wellWebID == -1)
                            continue;

                        foreach (var formItem in item.Value)
                        {
                            if (formItem.LogData != null)
                            {
                                if (!checkNames.Contains(formItem.LogCurveName.Name))
                                    continue;
                                var dataArray = formItem.LogData.LogDataValues.Select(o => (double)o).ToArray();
                                PbWellLogCreateParams logObj = new PbWellLogCreateParams
                                {
                                    WellId = wellWebID,
                                    SampleRate = formItem.LogData.DepthSampleRate.HasValue ? formItem.LogData.DepthSampleRate.Value : 0,
                                    StartDepth = formItem.LogData.StartDepth.HasValue ? formItem.LogData.StartDepth.Value : 0,
                                    CurveName = formItem.LogCurveName.Name,
                                    DataSetId = dataSetId,                                   
                                };

                                var checkNameObj = KingDomData.LogNames.FirstOrDefault(o => o.Name == formItem.LogCurveName.Name);
                                if (checkNameObj != null)
                                {
                                    logObj.CurveType = checkNameObj.LogType.FamilyName;
                                    logObj.SampleMeatureId = checkNameObj.LogType.MeasureType;
                                    logObj.SampleUnitId = checkNameObj.UnitID;
                                }
      
                                logObj.Samples.AddRange(dataArray);
                                logList.LogList.Add(logObj);
                            }
                        }
                    }

                    var res4 = await wellDataService.batch_create_well_log(logList);
                    if (res4 != null)
                    {

                    }

                    LogManagerService.Instance.Log($"welllog synchronize ({index}/{BoreholeIds.Count})");
                }
                index++;
            }    
        }

        public List<WellDailyProductionData> GetWellProductionData(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList)
        {
            List<WellDailyProductionData> datas = new List<WellDailyProductionData>();
            List<WellExport> Wells = KingDomData.Wells;
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

            List<MetaInfo> MetaInfoList = new List<MetaInfo>();
            MetaInfo metaInfo = new MetaInfo();
            metaInfo.DisplayName = "油体积";
            metaInfo.PropertyName = "dailyList.oilVol";
            metaInfo.UnitId = OilOrWaterUnit.Id;
            metaInfo.MeasureId = OilOrWaterUnit.MeasureID;
            MetaInfoList.Add(metaInfo);
            MetaInfo metaInfo2 = new MetaInfo();
            metaInfo2.DisplayName = "气体积";
            metaInfo2.PropertyName = "dailyList.gasVol";
            metaInfo2.UnitId = GasUnit.Id;
            metaInfo2.MeasureId = GasUnit.MeasureID;
            MetaInfoList.Add(metaInfo2);

            MetaInfo metaInfo3 = new MetaInfo();
            metaInfo3.DisplayName = "水体积";
            metaInfo3.PropertyName = "dailyList.waterVol";
            metaInfo3.UnitId = OilOrWaterUnit.Id;
            metaInfo3.MeasureId = OilOrWaterUnit.MeasureID;
            MetaInfoList.Add(metaInfo3);


            using (var context = project.GetKingdom())
            {
                foreach (var boreID in BoreholeIds)
                {
                    var kindomDatas = context.Get(new Smt.Entities.ProductionVolumeHistory(),
                     x => new
                     {
                         borehole = x.Borehole,
                         boreholeId = x.BoreholeId,
                         wellUWI = x.Borehole.Uwi,
                         data = x,
                     },
                       x => x.BoreholeId == boreID,
                     false).ToList();

                    if (kindomDatas.Count > 0)
                    {
                        string WellUWI = kindomDatas.FirstOrDefault().wellUWI;
                        long wellWebID = Utils.GetWellIDByWellUWI(WellUWI, WellIDandNameList);
                        if (wellWebID == -1)
                            continue;


                        WellDailyProductionData productionData = new WellDailyProductionData();
                        productionData.WellId = wellWebID;
                        productionData.MetaInfoList = MetaInfoList;
                        foreach (var item in kindomDatas)
                        {
                            if (item.data.StartDate == null || item.data.EndDate == null)
                                continue;
                            TimeSpan timespan = item.data.EndDate.Value - item.data.StartDate.Value;
                            int totalDays = timespan.Days + 1;
                            for (int i = 0; i < totalDays; i++)
                            {
                                DailyData dailyData = new DailyData()
                                {
                                    OilVol = item.data.OilProductionVolume == null ? 0 : item.data.OilProductionVolume.Value / totalDays,
                                    GasVol = item.data.GasProductionVolume == null ? 0 : item.data.GasProductionVolume.Value / totalDays,
                                    WaterVol = item.data.WaterProductionVolume == null ? 0 : item.data.WaterProductionVolume.Value / totalDays,
                                    MeasureDate = item.data.StartDate.Value.AddDays(i).ToShortDateString()
                                };
                                productionData.DailyList.Add(dailyData);
                            }
                        }
                        datas.Add(productionData);
                    }
                }
            }

            return datas;
        }

        public (List<WellGasTestData>,List<WellOilTestData>) GetWellGasTestData(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList)
        {
            List<WellGasTestData> datas = new List<WellGasTestData>();
            List<WellOilTestData> datasOil = new List<WellOilTestData>();
            List<WellExport> Wells = KingDomData.Wells;
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

            List<MetaInfo> OilTestDataMetaInfoList = new List<MetaInfo>();
            MetaInfo metaInfo = new MetaInfo();
            metaInfo.DisplayName = "chokeSize";
            metaInfo.PropertyName = "oilTestList.chokeSize";
            metaInfo.UnitId = ChokeSizeUnit.Id;
            metaInfo.MeasureId = ChokeSizeUnit.MeasureID;
            OilTestDataMetaInfoList.Add(metaInfo);
            MetaInfo metaInfo2 = new MetaInfo();
            metaInfo2.DisplayName = "FlowingTubingPressure";
            metaInfo2.PropertyName = "oilTestList.flowPressure";
            metaInfo2.UnitId = FlowingTubingPressureUnit.Id;
            metaInfo2.MeasureId = FlowingTubingPressureUnit.MeasureID;
            OilTestDataMetaInfoList.Add(metaInfo2);

            MetaInfo metaInfo3 = new MetaInfo();
            metaInfo3.DisplayName = "staticTemp";
            metaInfo3.PropertyName = "oilTestList.staticTemp";
            metaInfo3.UnitId = BottomHoleTemperatureUnit.Id;
            metaInfo3.MeasureId = BottomHoleTemperatureUnit.MeasureID;
            OilTestDataMetaInfoList.Add(metaInfo3);


            using (var context = project.GetKingdom())
            {
                var DeviationSurveys = context.Get(new Smt.Entities.TestInitialPotential(),
                 x => new
                 {
                     boreholeId = x.BoreholeId,
                     data = x,
                 },
                   x => BoreholeIds.Contains(x.BoreholeId),
                 false).ToList();

                var dicts = DeviationSurveys.GroupBy(o => o.boreholeId).ToDictionary(a => a.Key, a => a.ToList());
                foreach (var item in dicts)//一口井一般一条试数据
                {
                    var well = Wells.FirstOrDefault(o => o.BoreholeId == item.Key);
                    var uwi = well?.Uwi;
                    long wellWebID = Utils.GetWellIDByWellUWI(uwi, WellIDandNameList);
                    if (wellWebID == -1)
                        continue;
                    WellGasTestData wellGasTestData = new WellGasTestData();
                    wellGasTestData.MetaInfoList = new List<MetaInfo>();
                    wellGasTestData.WellId = wellWebID;
                    datas.Add(wellGasTestData);

                    WellOilTestData wellOilTestData = new WellOilTestData();
                    wellOilTestData.MetaInfoList = OilTestDataMetaInfoList;
                    wellOilTestData.WellId = wellWebID;
                    datasOil.Add(wellOilTestData);

                    foreach (var formItem in item.Value)
                    {
                        if (formItem.data != null)
                        {
                            string duanName = "";
                            if (formItem.data.StartDepth.HasValue && formItem.data.EndDepth.HasValue)
                            {
                                duanName = formItem.data.StartDepth.Value.ToString("G") + "-" + formItem.data.EndDepth.Value.ToString("G");
                            }

                            if (formItem.data.GasVolume.HasValue)
                            {
                                GasTestData gasTestData = new GasTestData()
                                {
                                    WellId = wellWebID.ToString(),
                                    WellName = well?.WellName,
                                    Interval = duanName,
                                    Wpr = formItem.data.GasVolume.Value,                                  
                                };
                                if (formItem.data.WaterVolume.HasValue)
                                {
                                    gasTestData.Wp = formItem.data.WaterVolume.Value;
                                }
                                wellGasTestData.GasTestList.Add(gasTestData);
                            }

                            if (formItem.data.OilVolume.HasValue)
                            {
                                OilTestData gasTestData = new OilTestData()
                                {
                                    WellId = wellWebID.ToString(),
                                    WellName = well?.WellName,
                                    TestWellSection = duanName,
                                    OilAmountPerDay = formItem.data.OilVolume.Value,
                                    ChokeSize = formItem.data.TopChokeSize.HasValue ?  formItem.data.TopChokeSize.Value:0,
                                    FlowPressure = formItem.data.FlowingTubingPressure.HasValue ?  formItem.data.FlowingTubingPressure.Value:0,
                                    StaticTemp = formItem.data.BottomHoleTemperature.HasValue ?  formItem.data.BottomHoleTemperature.Value:0
                                }
                            ;
                                if (formItem.data.WaterVolume.HasValue)
                                {
                                    gasTestData.WaterAmountPerDay = formItem.data.WaterVolume.Value;
                                }
                                wellOilTestData.OilTestList.Add(gasTestData);
                            }

                        }
                    }

                }
            }

            return (datas,datasOil);
        }

        public List<DatasetItemDto> GetWellConclusion(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList, List<SymbolMappingDto> SymbolMapping)
        {         
            List<DatasetItemDto> datas = new List<DatasetItemDto>();
            List<WellExport> Wells = KingDomData.Wells;
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();
            
            List<MetaInfo> MetaInfoList = new List<MetaInfo>();
            MetaInfo metaInfo = new MetaInfo();
            metaInfo.DisplayName = "top";
            metaInfo.PropertyName = "conclusionList.top";
            metaInfo.UnitId = DepthUnit.Id;
            metaInfo.MeasureId = DepthUnit.MeasureID;
            MetaInfoList.Add(metaInfo);
            MetaInfo metaInfo2 = new MetaInfo();
            metaInfo2.DisplayName = "bottom";
            metaInfo2.PropertyName = "conclusionList.bottom";
            metaInfo2.UnitId = DepthUnit.Id;
            metaInfo2.MeasureId = DepthUnit.MeasureID;
            MetaInfoList.Add(metaInfo2);

            using (var context = project.GetKingdom())
            {
                var DeviationSurveys = context.Get(new Smt.Entities.IntervalRecord(),
                 x => new
                 {
                     borehole = x.Borehole,
                     boreholeId = x.BoreholeId,
                     wellUWI = x.Borehole.Uwi,
                     data = x,
                     TextValues = x.IntervalTextValues
                 },
                   x => BoreholeIds.Contains(x.BoreholeId),
                 false).ToList();

                var dicts = DeviationSurveys.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());//按井分组
                foreach (var item in dicts)
                {
                    long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                    if (wellWebID == -1)
                        continue;
                    DatasetItemDto datasetItemDto = new DatasetItemDto();
                    datasetItemDto.MetaInfoList = MetaInfoList;
                    datasetItemDto.WellId = wellWebID;                   
                    foreach (var dictItem in item.Value)
                    {
                        if (dictItem.data != null)
                        {
                            if (dictItem.TextValues.Count > 0)//必须由第三列字段
                            {
                                string consolusionName = dictItem.TextValues[0].Value;
                                ConclusionDto conclusionDto = new ConclusionDto();
                                conclusionDto.ConclusionName = consolusionName;
                                conclusionDto.Color = Utils.ColorToInt(Colors.Red);
                                conclusionDto.Top = dictItem.data.StartDepth;
                                conclusionDto.Bottom = dictItem.data.EndDepth;

                                var res =  SymbolMapping.FirstOrDefault(o => o.ConclusionName == consolusionName);
                                if (res != null)
                                    conclusionDto.SymbolLibraryCode = res.SymbolLibraryCode;
                                datasetItemDto.ConclusionList.Add(conclusionDto);
                            }
                        }
                    }

                    if (datasetItemDto.ConclusionList.Count > 0)
                    {
                        datas.Add(datasetItemDto);
                    }

                }
            }

            return datas;
        }



        public List<string> GetConclusionNames(ProjectResponse KingDomData)
        {
            List<string> ConclusionNames = new List<string>();

            List<WellExport> Wells = KingDomData.Wells;
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

            using (var context = project.GetKingdom())
            {
                var DeviationSurveys = context.Get(new Smt.Entities.IntervalRecord(),
                 x => new
                 {
                     borehole = x.Borehole,
                     boreholeId = x.BoreholeId,
                     wellUWI = x.Borehole.Uwi,
                     data = x,
                     TextValues = x.IntervalTextValues
                 },
                   x => BoreholeIds.Contains(x.BoreholeId),
                 false).ToList();

                var dicts = DeviationSurveys.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());//按井分组
                foreach (var item in DeviationSurveys)
                {
                    if (item.TextValues.Count > 0)//必须由第三列字段
                    {
                        string consolusionName = item.TextValues[0].Value;
                        if (!ConclusionNames.Contains(consolusionName))
                            ConclusionNames.Add(consolusionName);
                    }
                }
            }


            return ConclusionNames;
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
