using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Mvvm;
using DevExpress.Spreadsheet.Functions;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid.Hierarchy;
using DevExpress.Xpo.Logger;
using Google.Protobuf;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.Models;
using KindomDataAPIServer.Models.Settings;
using KindomDataAPIServer.ViewModels;
using log4net;
using Smt;
using Smt.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography.X509Certificates;
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


        public bool IsDepthFeet
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

        public bool IsXYFeet
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

                //var logons = project.AuthorizedLogOnAuthorNames ?? Enumerable.Empty<string>();
                var logons = GetProjectAuthors() ?? Enumerable.Empty<string>();
                var logonList = logons.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

                return logonList;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"LoginDB failed: {ex.Message + ex.StackTrace}");
                if (ex.InnerException != null)
                {
                      LogManagerService.Instance.Log($"LoginDB InnerException failed: {ex.InnerException.Message + ex.InnerException.StackTrace}");
                }
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
                        Kb = b.Well.KBElevation
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
                            Kb = bh.Kb.HasValue ? bh.Kb.Value : 0,
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
                    //var IntervalRecords = context.Get(new Smt.Entities.IntervalName(),
                    // x => new
                    // {
                    //     data = x,
                    //     attrs = x.IntervalAttributes
                    // },
                    // x => true,
                    // false).ToList();

                    //foreach (var intervalRecord in IntervalRecords)
                    //{
                    //    {
                    //        foreach (var attr in intervalRecord.attrs)
                    //        {
                    //            var IntervalAttributes = context.Get(new Smt.Entities.IntervalAttribute(),
                    //                    x => new
                    //                    {
                    //                        attrdata = x,
                    //                        texts = x.IntervalTextValues
                    //                    },
                    //                    x => x.Id == attr.Id,
                    //                    false).ToList();
                    //        }

                    //    }
                    //}

                    //var IntervalAttribute = context.Get(new Smt.Entities.IntervalRecord(),
                    // x => new
                    // {
                    //     data = x,
                    //     texts = x.IntervalTextValues,
                    //     bore = x.Borehole
                    // },
                    //  x => true,
                    // false).ToList();


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
   
                                if (IsDepthFeet)
                                {
                                    pbWellFormation.Items.Add(new PbFormationItem()
                                    {
                                        Name = formItem.FormationTopName.Name,
                                        Top = formItem.FormationTop.Depth.Value.ToMeters(),
                                        Bottom = formItem.FormationTop.Depth.Value.ToMeters(),
                                    });
                                }
                                else
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
                                if(IsDepthFeet)
                                {
                                    coordData.Md = coordData.Md.ToMeters();
                                    coordData.Tvd = coordData.Tvd.ToMeters();
                                }
                                if(IsXYFeet)
                                {
                                    coordData.Dx = coordData.Dx.ToMeters();
                                    coordData.Dy = coordData.Dy.ToMeters();
                                }

                                wellTrajData.CoordList.Add(coordData);
                                AimuthData aimuthData = new AimuthData()
                                {
                                    Md = formItem.data.MD[i],
                                    Azim = formItem.data.Azimuth[i],
                                    Devi = formItem.data.Inclination[i],
                                };
                                if(IsDepthFeet)
                                {
                                    aimuthData.Md = aimuthData.Md.ToMeters();
                                }
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

        public async Task CreateWellLogsToWeb(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList, string resdataSetID, SyncKindomDataViewModel syncKindomDataViewModel)
        {
            var wellDataService = ServiceLocator.GetService<IDataWellService>();

            long dataSetId = 0;
            if(!string.IsNullOrEmpty(resdataSetID))
                dataSetId =long.Parse(resdataSetID);
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
                                if (IsDepthFeet)
                                {
                                    logObj.SampleRate = logObj.SampleRate.ToMeters();
                                    logObj.StartDepth = logObj.StartDepth.ToMeters();
                                }

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
                    if (logList.LogList.Count > 0)
                    {
                        var res4 = await wellDataService.batch_create_well_log(logList);
                        if (res4 != null)
                        {

                        }
                    }
                    syncKindomDataViewModel.ProgressValue =60 + (double)index / BoreholeIds.Count * 20;
                    LogManagerService.Instance.Log($"welllog synchronize ({index}/{BoreholeIds.Count})");
                }
                index++;
            }    
        }


        public void CreateWellLogsToKindom()
        {
            using (var context = project.GetKingdom())
            {
                string wellUWI = "ZJ19H";
                var borehole = context.Get(new Borehole(),
                        x => new
                        {
                            boreholeId = x.Id,
                        },
                        x => x.Uwi == wellUWI,
                        false).ToList();
                if (borehole.Count == 0)
                    return;

                var logTypes = context.Get(new LogType(),
        x => new
        {
            data = x,
        },
        x => true,
        false).ToList();
                int id = borehole.FirstOrDefault().boreholeId;

                LogData logData = new LogData(CRUDOption.CreateOrUpdate);
                logData.BoreholeId = id;
                logData.LogCurveName = new LogCurveName() { Name = "GRCC", EntityCRUDOption = CRUDOption.CreateOrUpdate};
                logData.StartDepth = 0;
                logData.DepthSampleRate = 2;
                float[] values = new float[4];
                values[0] = 10;
                values[1] = 20;
                values[2] = 30;
                values[3] = 10;
                float[] depths = new float[4];
                depths[0] = 0;
                depths[1] = 2;
                depths[2] = 4;
                depths[3] = 6;
                logData.SetLogDepthsAndValues(depths, values);
                context.AddObject(logData);
                context.SaveChanges();
            }
        }

        public List<WellDailyProductionData> GetWellProductionData(ProjectResponse kindomData, PbViewMetaObjectList wellIDandNameList, bool isShowOil, bool isShowGas, bool isShowWater)
        {
            List<WellDailyProductionData> datas = new List<WellDailyProductionData>();
            List<WellExport> Wells = kindomData.Wells;
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
                        long wellWebID = Utils.GetWellIDByWellUWI(WellUWI, wellIDandNameList);
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
                                    MeasureDate = item.data.StartDate.Value.AddDays(i).ToShortDateString()
                                };

                                if (isShowOil)
                                {
                                    dailyData.OilVol = item.data.OilProductionVolume == null ? 0 : item.data.OilProductionVolume.Value / totalDays;
                                }
                                if (isShowGas)
                                {
                                    dailyData.GasVol = item.data.GasProductionVolume == null ? 0 : item.data.GasProductionVolume.Value / totalDays;

                                }
                                if (isShowWater)
                                {
                                    dailyData.WaterVol = item.data.WaterProductionVolume == null ? 0 : item.data.WaterProductionVolume.Value / totalDays;
                                }
                                productionData.DailyList.Add(dailyData);
                            }
                        }
                        datas.Add(productionData);
                    }
                }
            }

            return datas;
        }

        public async Task CreateWellProductionDataToWeb(ProjectResponse kindomData, PbViewMetaObjectList wellIDandNameList, bool isShowOil, bool isShowGas, bool isShowWater, SyncKindomDataViewModel syncKindomDataViewModel)
        {
            try
            {
                var wellDataService = ServiceLocator.GetService<IDataWellService>();

                List<WellExport> Wells = kindomData.Wells;
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
                    int index = 1;
                    int allCount = BoreholeIds.Count;
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
                            long wellWebID = Utils.GetWellIDByWellUWI(WellUWI, wellIDandNameList);
                            if (wellWebID == -1)
                                continue;

                            List<WellDailyProductionData> datas = new List<WellDailyProductionData>();
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
                                        MeasureDate = item.data.StartDate.Value.AddDays(i).ToShortDateString()
                                    };

                                    if (isShowOil)
                                    {
                                        dailyData.OilVol = item.data.OilProductionVolume == null ? 0 : item.data.OilProductionVolume.Value / totalDays;
                                    }
                                    if (isShowGas)
                                    {
                                        dailyData.GasVol = item.data.GasProductionVolume == null ? 0 : item.data.GasProductionVolume.Value / totalDays;

                                    }
                                    if (isShowWater)
                                    {
                                        dailyData.WaterVol = item.data.WaterProductionVolume == null ? 0 : item.data.WaterProductionVolume.Value / totalDays;
                                    }
                                    productionData.DailyList.Add(dailyData);
                                }
                            }
                            datas.Add(productionData);

                            if (datas.Count > 0)
                            {
                                WellProductionDataRequest Request = new WellProductionDataRequest();
                                Request.Items = datas;
                                var res4 = await wellDataService.batch_create_well_production_with_meta_infos(Request);
                                if (res4 != null)
                                {

                                }
                            }

                            LogManagerService.Instance.Log($"Well Production Datas synchronize ({index}/{allCount})");
                            syncKindomDataViewModel.ProgressValue = 30 + (index * 20) / allCount;
                        }
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("CreateWellProductionDataToWeb error:" + ex.Message + ex.StackTrace);
            }
        }


        public UnitInfo GetUnitInfoByKingdomName(List<UnitMappingItem> UnitMappingItems,string kindomName)
        {

            var mappingItem = UnitMappingItems.FirstOrDefault(o => o.KindomUnitName == kindomName);
            if (mappingItem != null)
            {
                return mappingItem.NewUnit;
            }
            return null;
        }

        public (List<WellGasTestData>,List<WellOilTestData>) GetWellGasTestData(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList, List<UnitMappingItem> UnitMappingItems)
        {
            List<WellGasTestData> datas = new List<WellGasTestData>();
            List<WellOilTestData> datasOil = new List<WellOilTestData>();
            List<WellExport> Wells = KingDomData.Wells;
            List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

            MetaInfo metaInfo = new MetaInfo();
            metaInfo.DisplayName = "chokeSize";
            metaInfo.PropertyName = "oilTestList.chokeSize";
            metaInfo.UnitId = ChokeSizeUnit.Id;
            metaInfo.MeasureId = ChokeSizeUnit.MeasureID;

            MetaInfo metaInfo2 = new MetaInfo();
            metaInfo2.DisplayName = "FlowingTubingPressure";
            metaInfo2.PropertyName = "oilTestList.flowPressure";
            metaInfo2.UnitId = FlowingTubingPressureUnit.Id;
            metaInfo2.MeasureId = FlowingTubingPressureUnit.MeasureID;

            MetaInfo metaInfo3 = new MetaInfo();
            metaInfo3.DisplayName = "staticTemp";
            metaInfo3.PropertyName = "oilTestList.staticTemp";
            metaInfo3.UnitId = BottomHoleTemperatureUnit.Id;
            metaInfo3.MeasureId = BottomHoleTemperatureUnit.MeasureID;


            using (var context = project.GetKingdom())
            {
                var DeviationSurveys = context.Get(new Smt.Entities.TestInitialPotential(),
                 x => new
                 {
                     boreholeId = x.BoreholeId,
                     data = x,
                     header = x.ProductionTestHeader
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
                    wellOilTestData.MetaInfoList = new List<MetaInfo>();
                    wellOilTestData.MetaInfoList.Add(metaInfo);
                    wellOilTestData.MetaInfoList.Add(metaInfo2);
                    wellOilTestData.MetaInfoList.Add(metaInfo3);
                    wellOilTestData.WellId = wellWebID;
                    datasOil.Add(wellOilTestData);

                    foreach (var formItem in item.Value)
                    {
                        if (formItem.data != null)
                        {
                            string duanName = "";
                            double thickness = 0;
                            if (formItem.data.StartDepth.HasValue && formItem.data.EndDepth.HasValue)
                            {
                                duanName = formItem.data.StartDepth.Value.ToString("G") + "-" + formItem.data.EndDepth.Value.ToString("G");
                                thickness = formItem.data.EndDepth.Value - formItem.data.StartDepth.Value;
                            }
                            else
                            {
                                continue;
                            }
                            
                            if (formItem.data.GasVolume.HasValue)
                            {
                                int order = 0;
                                bool resB = int.TryParse(formItem.header.TestNumber, out order);
                                GasTestData gasTestData = new GasTestData()
                                {
                                    WellId = wellWebID.ToString(),                                  
                                    WellName = well?.WellName,
                                    Interval = duanName,
                                     Freq = order,
                                    Wpr = formItem.data.GasVolume.Value,                               
                                };
                                var unit = GetUnitInfoByKingdomName(UnitMappingItems, formItem.data.GasRate);
                                if (unit != null)
                                {
                                    MetaInfo metaInfoGas = new MetaInfo();
                                    metaInfoGas.DisplayName = "wpr";
                                    metaInfoGas.PropertyName = "gasTestList.wpr";
                                    metaInfoGas.UnitId = unit.Id;
                                    metaInfoGas.MeasureId = unit.MeasureID;
                                    wellGasTestData.MetaInfoList.Add(metaInfoGas);
                                }

                                if (formItem.data.WaterVolume.HasValue)
                                {
                                    gasTestData.Wp = formItem.data.WaterVolume.Value;
                                }

                                var waterUnit = GetUnitInfoByKingdomName(UnitMappingItems, formItem.data.WaterRate);
                                if (waterUnit != null)
                                {
                                    MetaInfo metaInfoWater = new MetaInfo();
                                    metaInfoWater.DisplayName = "wp";
                                    metaInfoWater.PropertyName = "gasTestList.wp";
                                    metaInfoWater.UnitId = waterUnit.Id;
                                    metaInfoWater.MeasureId = waterUnit.MeasureID;
                                    wellGasTestData.MetaInfoList.Add(metaInfoWater);
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
                                     Sequence = formItem.header.TestNumber,
                                    OilAmountPerDay = formItem.data.OilVolume.Value,
                                    ChokeSize = formItem.data.TopChokeSize.HasValue ? formItem.data.TopChokeSize.Value : 0,
                                    FlowPressure = formItem.data.FlowingTubingPressure.HasValue ? formItem.data.FlowingTubingPressure.Value : 0,
                                    StaticTemp = formItem.data.BottomHoleTemperature.HasValue ? formItem.data.BottomHoleTemperature.Value : 0,
                                    Thickness = thickness
                                     
                                };

                                var oilUnit = GetUnitInfoByKingdomName(UnitMappingItems, formItem.data.OilRate);
                                if (oilUnit != null)
                                {
                                    MetaInfo metaInfoOil = new MetaInfo();
                                    metaInfoOil.DisplayName = "oilAmountPerDay";
                                    metaInfoOil.PropertyName = "oilTestList.oilAmountPerDay";
                                    metaInfoOil.UnitId = oilUnit.Id;
                                    metaInfoOil.MeasureId = oilUnit.MeasureID;
                                    wellOilTestData.MetaInfoList.Add(metaInfoOil);
                                }
                                
                                if (formItem.data.WaterVolume.HasValue)
                                {
                                    gasTestData.WaterAmountPerDay = formItem.data.WaterVolume.Value;
                                }

                                var waterUnit = GetUnitInfoByKingdomName(UnitMappingItems, formItem.data.WaterRate);
                                if (waterUnit != null)
                                { 
                                    MetaInfo metaInfoWater = new MetaInfo();
                                    metaInfoWater.DisplayName = "waterAmountPerDay";
                                    metaInfoWater.PropertyName = "oilTestList.waterAmountPerDay";
                                    metaInfoWater.UnitId = waterUnit.Id;
                                    metaInfoWater.MeasureId = waterUnit.MeasureID;
                                    wellOilTestData.MetaInfoList.Add(metaInfoWater);
                                }
                                wellOilTestData.OilTestList.Add(gasTestData);
                            }

                        }
                    }

                }
            }

            return (datas,datasOil);
        }


        public const string TestOilVolume = "Oil Volume";
        public const string TestGasVolume = "Gas Volume";
        public const string TestWaterVolume = "Water Volume";
        public List<UnitMappingItem> GetWellProductionTestDataUnits(ProjectResponse KingDomData)
        {
            List<UnitMappingItem> UnitMappingItems = new List<UnitMappingItem>();
            try
            {
                List<WellExport> Wells = KingDomData.Wells;
                List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

                using (var context = project.GetKingdom())
                {
                    var TestInitialPotentials = context.Get(new Smt.Entities.TestInitialPotential(),
                     x => new
                     {
                         boreholeId = x.BoreholeId,
                         data = x,
                     },
                       x => BoreholeIds.Contains(x.BoreholeId),
                     false).ToList();


                    foreach (var item in TestInitialPotentials)
                    {
                        if (!string.IsNullOrEmpty(item.data.OilRate))
                        {
                            var res = UnitMappingItems.FirstOrDefault(o => o.KindomUnitName == item.data.OilRate && o.PropName == TestOilVolume);
                            if (res == null)
                            {
                                UnitMappingItem unitMappingItem = new UnitMappingItem
                                {
                                    KindomUnitName = item.data.OilRate,
                                    PropName = TestOilVolume
                                };

                                unitMappingItem.UnitInfos = Utils.OilOrWaterInfos;
                                unitMappingItem.NewUnit = OilOrWaterUnit;
                                UnitMappingItems.Add(unitMappingItem);
                            }
                        }
                        if (!string.IsNullOrEmpty(item.data.GasRate))
                        {
                            var res = UnitMappingItems.FirstOrDefault(o => o.KindomUnitName == item.data.GasRate && o.PropName == TestGasVolume);
                            if (res == null)
                            {
                                UnitMappingItem unitMappingItem = new UnitMappingItem
                                {
                                    KindomUnitName = item.data.GasRate,
                                    PropName = TestGasVolume
                                };
                                unitMappingItem.UnitInfos = Utils.GasUnitInfos;
                                unitMappingItem.NewUnit = GasUnit;
                                UnitMappingItems.Add(unitMappingItem);
                            }
                        }
                        if (!string.IsNullOrEmpty(item.data.WaterRate))
                        {
                            var res = UnitMappingItems.FirstOrDefault(o => o.KindomUnitName == item.data.WaterRate && o.PropName == TestWaterVolume);
                            if (res == null)
                            {
                                UnitMappingItem unitMappingItem = new UnitMappingItem
                                {
                                    KindomUnitName = item.data.WaterRate,
                                    PropName = TestWaterVolume
                                };
                                unitMappingItem.UnitInfos = Utils.OilOrWaterInfos;
                                unitMappingItem.NewUnit = OilOrWaterUnit;
                                UnitMappingItems.Add(unitMappingItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("GetWellProductionTestDataUnits error:" + ex.Message + ex.StackTrace);
            }
            return UnitMappingItems;
        }

        public Dictionary<string, CreatePayzoneRequest> CreateWellConclusionsToWeb(ProjectResponse KingDomData, PbViewMetaObjectList WellIDandNameList, List<ConclusionFileNameObj> ConclusionFileNameObjItems)
        {
            //构建请求体 根据设置行数
            Dictionary<string, CreatePayzoneRequest> requestDict = new Dictionary<string, CreatePayzoneRequest>();

            try
            {

                foreach (var FileNameObj in ConclusionFileNameObjItems)
                {
                    var setting = FileNameObj.ConclusionSetting;
                    CreatePayzoneRequest ConclusionRequest = new CreatePayzoneRequest();
                    if (setting.ExplanationType == ExplanationType.Payzon)
                    {
                        ConclusionRequest.DatasetType = 1;
                    }
                    else if (setting.ExplanationType == ExplanationType.Lithology)
                    {
                        ConclusionRequest.DatasetType = 2;
                    }
                    else if (setting.ExplanationType == ExplanationType.SedimentaryFacies)
                    {
                        ConclusionRequest.DatasetType = 3;
                    }
                    ConclusionRequest.DatasetName = setting.NewConclusionName;
                    List<SymbolMappingDto> SymbolMapping = new List<SymbolMappingDto>();
                    foreach (var ConclusionMappingItem in setting.ConclusionMappingItems)
                    {
                        SymbolMappingDto temp = new SymbolMappingDto();
                        temp.Color = Utils.ColorToInt(ConclusionMappingItem.Color);
                        temp.ConclusionName = ConclusionMappingItem.PolygonName;
                        temp.SymbolLibraryCode = ConclusionMappingItem.SymbolLibraryCode;
                        SymbolMapping.Add(temp);
                    }
                    ConclusionRequest.SymbolMapping = SymbolMapping;
                    ConclusionRequest.Items = new List<DatasetItemDto>();
                    requestDict.Add(setting.GUID, ConclusionRequest);
                }


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
                    var IntervalRecords = context.Get(new Smt.Entities.IntervalRecord(),
                     x => new
                     {
                         borehole = x.Borehole,
                         boreholeId = x.BoreholeId,
                         wellUWI = x.Borehole.Uwi,
                         data = x,
                         intervalName = x.IntervalName.Name,
                         TextValues = x.IntervalTextValues,
                         numValues = x.IntervalTimeValues
                     },
                       x => BoreholeIds.Contains(x.BoreholeId),
                     false).ToList();


                    var IntervalAttributes = context.Get(new Smt.Entities.IntervalAttribute(),
                             x => new
                             {
                                 data = x,
                                 id = x.Id
                             },
                               x => true,
                             false).ToList();

                    var dicts = IntervalRecords.GroupBy(o => o.wellUWI).ToDictionary(a => a.Key, a => a.ToList());//按井分组
                    foreach (var item in dicts)
                    {
                        long wellWebID = Utils.GetWellIDByWellUWI(item.Key, WellIDandNameList);
                        if (wellWebID == -1)
                            continue;
                        //DatasetItemDto datasetItemDtoPayzon = new DatasetItemDto();
                        //datasetItemDtoPayzon.MetaInfoList = MetaInfoList;
                        //datasetItemDtoPayzon.WellId = wellWebID;

                        //DatasetItemDto datasetItemDtoLithology = new DatasetItemDto();
                        //datasetItemDtoLithology.MetaInfoList = MetaInfoList;
                        //datasetItemDtoLithology.WellId = wellWebID;


                        //DatasetItemDto datasetItemDtoFacies = new DatasetItemDto();
                        //datasetItemDtoFacies.MetaInfoList = MetaInfoList;
                        //datasetItemDtoFacies.WellId = wellWebID;

                        foreach (var dictItem in item.Value)
                        {
                            if (dictItem.data != null)
                            {
                                foreach (var textValue in dictItem.TextValues)
                                {
                                    var attr = IntervalAttributes.FirstOrDefault(o => o.data.Id == textValue.IntervalAttributeId);
                                    var setting = GetConclusionSetting(ConclusionFileNameObjItems, dictItem.intervalName, attr.data.Name);
                                    if (setting != null)
                                    {
                                        DatasetItemDto dto = requestDict[setting.GUID].Items.FirstOrDefault(o => o.WellId == wellWebID);
                                        if (dto == null)
                                        {
                                            dto = new DatasetItemDto()
                                            {
                                                MetaInfoList = MetaInfoList,
                                                WellId = wellWebID
                                            };
                                            requestDict[setting.GUID].Items.Add(dto);
                                        }
                                        ConclusionDto conclusionDto = new ConclusionDto()
                                        {
                                            ConclusionName = textValue.Value,
                                            Color = Utils.ColorToInt(Colors.Red),
                                            Top = dictItem.data.StartDepth,
                                            Bottom = dictItem.data.EndDepth,
                                        };
                                        conclusionDto.SymbolLibraryCode = GetConclusionSymbolCode(setting.ConclusionMappingItems, textValue.Value);
                                        if (!string.IsNullOrEmpty(conclusionDto.SymbolLibraryCode))
                                        {
                                            dto.ConclusionList.Add(conclusionDto);
                                        }
                                        else
                                        {
                                            LogManagerService.Instance.Log("No corresponding symbol found" + textValue.Value);
                                        }

                                    }
                                    else
                                    {
                                        LogManagerService.Instance.Log("No corresponding settings found" + dictItem.intervalName + "-" + attr.data.Name);
                                    }
                                }

                                foreach (var numValue in dictItem.numValues)
                                {
                                    var attr = IntervalAttributes.FirstOrDefault(o => o.data.Id == numValue.IntervalAttributeId);
                                    var setting = GetConclusionSetting(ConclusionFileNameObjItems, dictItem.intervalName, attr.data.Name);
                                    if (setting != null)
                                    {
                                        DatasetItemDto dto = requestDict[setting.GUID].Items.FirstOrDefault(o => o.WellId == wellWebID);
                                        if (dto == null)
                                        {
                                            dto = new DatasetItemDto()
                                            {
                                                MetaInfoList = MetaInfoList,
                                                WellId = wellWebID
                                            };
                                            requestDict[setting.GUID].Items.Add(dto);
                                        }
                                        ConclusionDto conclusionDto = new ConclusionDto()
                                        {
                                            ConclusionName = numValue.Value.ToString(),
                                            Color = Utils.ColorToInt(Colors.Red),
                                            Top = dictItem.data.StartDepth,
                                            Bottom = dictItem.data.EndDepth,
                                        };
                                        conclusionDto.SymbolLibraryCode = GetConclusionSymbolCode(setting.ConclusionMappingItems, numValue.Value.ToString());
                                        if (!string.IsNullOrEmpty(conclusionDto.SymbolLibraryCode))
                                        {
                                            dto.ConclusionList.Add(conclusionDto);
                                        }
                                        else
                                        {
                                            LogManagerService.Instance.Log("No corresponding symbol found" + numValue.Value.ToString());
                                        }
                                    }
                                    else
                                    {
                                        LogManagerService.Instance.Log("No corresponding settings found" + dictItem.intervalName + "-" + attr.data.Name);
                                    }

                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("CreateWellConclusionsToWeb error:" + ex.Message + ex.StackTrace);
            }
            return requestDict;
        }


       public string GetConclusionSymbolCode(ObservableCollection<ConclusionMappingItem> SymbolMapping, string conclusionName)
        {
            var res = SymbolMapping.FirstOrDefault(o => o.PolygonName == conclusionName);
            if (res != null)
                return res.SymbolLibraryCode;
            return "";
        }


        public List<SymbolMappingDto> GetSymbolMapping(ConclusionFileNameObjConclusionSetting setting)
        {
            List<SymbolMappingDto> symbolMappingDtos = new List<SymbolMappingDto>();
            foreach (var item in setting.ConclusionMappingItems)
            {
                SymbolMappingDto symbolMappingDto = new SymbolMappingDto()
                {
                    Color = Utils.ColorToInt(item.Color),
                    ConclusionName = item.PolygonName,
                    SymbolLibraryCode = item.SymbolLibraryCode
                };
                symbolMappingDtos.Add(symbolMappingDto);
            }
            return symbolMappingDtos;
        }
        /// 
        /// </summary>
        /// <param name="ConclusionFileNameObjItems"></param>
        /// <param name="fileName">文件名</param>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public ConclusionFileNameObjConclusionSetting GetConclusionSetting(List<ConclusionFileNameObj> ConclusionFileNameObjItems, string fileName, string columnName)
        {
             var setting  =  ConclusionFileNameObjItems.FirstOrDefault(o => o.FileName.FileName == fileName && o.ColumnName == columnName);
            if(setting != null)
                return setting.ConclusionSetting;
            return null;
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

        public Dictionary<string, List<string>> ColumnNameDict = new Dictionary<string, List<string>>();


        public List<ConclusionFileNameObj> GetConclusionFileNameObjs(ProjectResponse KingDomData)
        {
            ColumnNameDict = new Dictionary<string, List<string>>();
            List<ConclusionFileNameObj> ConclusionNames = new List<ConclusionFileNameObj>();

            try
            {
                //List<WellExport> Wells = KingDomData.Wells;
                //List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();
                //if(BoreholeIds.Count==0)
                //    return ConclusionNames;

                using (var context = project.GetKingdom())
                {
                    var IntervalNames = context.Get(new Smt.Entities.IntervalName(),
                       x => new
                       {
                           data = x,
                           attrs = x.IntervalAttributes,
                           Name = x.Name,
                       },
                       x => true,
                       false).ToList();

                    foreach (var intervalName in IntervalNames)
                    {
                        ConclusionFileNameObj conclusionFileNameObj = new ConclusionFileNameObj();
                       // conclusionFileNameObj.FileName = intervalName.Name;
                        foreach (var attr in intervalName.attrs)
                        {
                            if (attr.IntervalAttributeType == AttributeType.Text || attr.IntervalAttributeType == AttributeType.Numeric)
                            {
                                var IntervalAttribute = context.Get(new Smt.Entities.IntervalAttribute(),
                                     x => new
                                     {
                                         texts = x.IntervalTextValues,
                                         numberTexts = x.IntervalNumericValues
                                     },
                                     x => x.Id == attr.Id,
                                     false);
                                List<string> textValues = IntervalAttribute.FirstOrDefault().texts.Select(o => o.Value).Distinct().ToList();
                                List<string> numValues = IntervalAttribute.FirstOrDefault().numberTexts.Select(o => o.Value.ToString()).Distinct().ToList();
                                textValues.AddRange(numValues);
                                textValues = textValues.Distinct().ToList();

                                if (textValues.Count > 0)
                                {
                                    if (ColumnNameDict.ContainsKey(attr.Name))
                                    {
                                        ColumnNameDict[attr.Name].AddRange(textValues);
                                    }
                                    else
                                    {
                                        ColumnNameDict.Add(attr.Name, textValues);
                                    }
                                    //conclusionFileNameObj.Columns.Add(attr.Name);
                                }

                            }
                        }
                        //if (conclusionFileNameObj.Columns.Count > 0)
                        //{
                        //    conclusionFileNameObj.ColumnName = conclusionFileNameObj.Columns.FirstOrDefault();
                        //    ConclusionNames.Add(conclusionFileNameObj);
                        //}
                    }

                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"GetConclusionFileNameObjs failed: {ex.Message+ ex.StackTrace}");
            }
            return ConclusionNames;
        }


        public List<FileNameObj> GetColumnNameDict(ProjectResponse KingDomData)
        {
            List<FileNameObj> FileNameObjs = new List<FileNameObj>();

            try
            {


                List<WellExport> Wells = KingDomData.Wells;
                List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

                using (var context = project.GetKingdom())
                {
                    var IntervalRecords = context.Get(new Smt.Entities.IntervalRecord(),//具体行记录
                     x => new
                     {
                         borehole = x.Borehole,
                         boreholeId = x.BoreholeId,
                         wellUWI = x.Borehole.Uwi,
                         data = x,
                         TextValues = x.IntervalTextValues,
                         NumValues = x.IntervalNumericValues,
                         IntervalName = x.IntervalName.Name,
                     },
                       x => BoreholeIds.Contains(x.BoreholeId),
                     false).ToList();


                    var IntervalAttributes = context.Get(new Smt.Entities.IntervalAttribute(),
                             x => new
                             {
                                 data = x,
                                 FileNameID = x.IntervalNameId,
                                 FileName = x.IntervalName.Name,
                                 IntervalAttributeID = x.Id,
                                 IntervalAttributeName = x.Name
                             },
                               x => true,
                             false).ToList();

                    List<string> fileNames = IntervalRecords.Select(o => o.IntervalName).Distinct().ToList();
                    Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
                    foreach (var file in fileNames)
                    {
                        dict.Add(file, new List<string>());
                    }



                    foreach (var interval in IntervalRecords)
                    {
                        if (interval.TextValues.Count > 0)
                        {
                            foreach (var intervalTextValue in interval.TextValues)
                            {
                                var attr = IntervalAttributes.FirstOrDefault(o => o.IntervalAttributeID == intervalTextValue.IntervalAttributeId);

                                if (attr != null && dict.ContainsKey(attr.FileName) && !dict[attr.FileName].Contains(attr.IntervalAttributeName))
                                {
                                    dict[attr.FileName].Add(attr.IntervalAttributeName);
                                }
                            }
                        }

                        if (interval.NumValues.Count > 0)
                        {
                            foreach (var intervalTextValue in interval.NumValues)
                            {
                                var attr = IntervalAttributes.FirstOrDefault(o => o.IntervalAttributeID == intervalTextValue.IntervalAttributeId);
                                if (attr != null && dict.ContainsKey(attr.FileName) && !dict[attr.FileName].Contains(attr.IntervalAttributeName))
                                {
                                    dict[attr.FileName].Add(attr.IntervalAttributeName);
                                }
                            }
                        }

                    }


                    foreach (var item in dict)
                    {
                        FileNameObj fileNameObj = new FileNameObj()
                        {
                            FileName = item.Key,
                            Columns = item.Value
                        };
                        FileNameObjs.Add(fileNameObj);
                    }

                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"GetColumnNameDict failed: {ex.Message + ex.StackTrace}");
            }

            return FileNameObjs;
        }



        public List<string> GetConclusionNames(ProjectResponse KingDomData, ConclusionFileNameObj obj)
        {
            List<string> ConclusionNames = new List<string>();

            try
            {


                List<WellExport> Wells = KingDomData.Wells;
                List<int> BoreholeIds = Wells.Where(o => o.IsChecked).Select(o => o.BoreholeId).ToList();

                using (var context = project.GetKingdom())
                {
                    var IntervalRecords = context.Get(new Smt.Entities.IntervalRecord(),//具体行记录
                     x => new
                     {
                         borehole = x.Borehole,
                         boreholeId = x.BoreholeId,
                         wellUWI = x.Borehole.Uwi,
                         data = x,
                         TextValues = x.IntervalTextValues,
                         NumValues = x.IntervalNumericValues,
                         IntervalName = x.IntervalName.Name,
                     },
                       x => BoreholeIds.Contains(x.BoreholeId) && obj.FileName.FileName == x.IntervalName.Name,
                     false).ToList();

                    var IntervalAttributes = context.Get(new Smt.Entities.IntervalAttribute(),
                         x => new
                         {
                             data = x,
                             FileNameID = x.IntervalNameId,
                             FileName = x.IntervalName.Name,
                             IntervalAttributeID = x.Id,
                             IntervalAttributeName = x.Name
                         },
                           x => true,
                         false).ToList();

                    foreach (var interval in IntervalRecords)
                    {
                        if (interval.TextValues.Count > 0)
                        {
                            foreach (var intervalTextValue in interval.TextValues)
                            {
                                var attr = IntervalAttributes.FirstOrDefault(o => o.IntervalAttributeID == intervalTextValue.IntervalAttributeId);

                                if (attr != null && attr.FileName == obj.FileName.FileName && attr.IntervalAttributeName == obj.ColumnName)
                                {
                                    if (!ConclusionNames.Contains(intervalTextValue.Value))
                                    {
                                        ConclusionNames.Add(intervalTextValue.Value);
                                    }
                                }
                            }
                        }

                        if (interval.NumValues.Count > 0)
                        {
                            foreach (var intervalTextValue in interval.NumValues)
                            {
                                var attr = IntervalAttributes.FirstOrDefault(o => o.IntervalAttributeID == intervalTextValue.IntervalAttributeId);
                                if (attr != null && attr.FileName == obj.FileName.FileName && attr.IntervalAttributeName == obj.ColumnName)
                                {
                                    if (!ConclusionNames.Contains(intervalTextValue.Value.ToString()))
                                    {
                                        ConclusionNames.Add(intervalTextValue.Value.ToString());
                                    }
                                }
                            }
                        }

                    }



                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"GetConclusionNames failed: {ex.Message + ex.StackTrace}");
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


        public async Task CreateWellLogsToKindom(List<WellLogData> wellLogDatas, List<WellCheckItem> wellCheckItems, SyncKindomDataViewModel syncKindomDataViewModel)
        {
            try
            {
                var wellDataService = ServiceLocator.GetService<IDataWellService>();

                wellLogDatas = wellLogDatas.Where(o => wellCheckItems.FirstOrDefault(a => a.ID == o.wellId && a.IsChecked) != null).ToList();
                int allWellCount = wellLogDatas.Count;
                if(allWellCount == 0)
                {
                    LogManagerService.Instance.Log("No well logs selected for import.");
                    syncKindomDataViewModel.ProgressValue = 100;
                    return;
                }

                int index = 1;
                foreach (var wellLogData in wellLogDatas)
                {
                    Borehole borehole = null;
                    var wellItem = wellCheckItems.FirstOrDefault(o => o.ID == wellLogData.wellId);
                    using (var context = project.GetKingdom())
                    {
                        borehole = context.Get<Borehole>(o => o.Uwi == wellItem.Name, false).FirstOrDefault();
                    };
                    if(borehole==null)
                    {
                        LogManagerService.Instance.Log($"Well UWI {wellItem.Name} not found in Kingdom.");
                        continue;
                    }
                    var logList = await wellDataService.export_curve_batch_protobuf(new List<WellLogData>() { wellLogData });
                    foreach (var log in logList.Items)
                    {
                        Unit unit = null;
                        using (var context = project.GetKingdom())
                        {
                            unit = context.Get<Unit>(o => o.Name == log.Unit, false).FirstOrDefault();
                        };


                        LogData logData = new LogData(CRUDOption.CreateOrUpdate)
                        {
                            Borehole = borehole,
                            LogCurveName = new LogCurveName
                            {
                                Name = log.Curvename,
                                Abbreviation = log.Curvename,
                                EntityCRUDOption = CRUDOption.CreateOrUpdate,
                                LogType = new LogType(CRUDOption.CreateOrUpdate)
                                {
                                    Name = log.Curvetype,
                                    Abbreviation = log.Curvetype,

                                }
                            },
                            DepthSampleRate = log.SampleRate,
                            StartDepth = log.StartDepth,
                        };
                        if (unit == null)
                        {
                            unit = new Unit
                            {
                                Name = log.Unit,
                                EntityCRUDOption = CRUDOption.CreateOrUpdate,
                            };
                            logData.Unit = unit;
                        }
                        else
                        {
                            logData.UnitId = unit.Id;
                        }


                        int dataCount = log.Samples.Count;
                        List<float> depths = new List<float>();
                        List<float> values = new List<float>();
                        for (int i = 0; i < dataCount; i++)
                        {
                            if (log.Samples[i] != -999)
                            {
                                depths.Add((float)(log.StartDepth + i * log.SampleRate));
                                values.Add((float)log.Samples[i]);
                            }
                        }
                        logData.SetLogDepthsAndValues(depths.ToArray(), values.ToArray());
                        using (var context = project.GetKingdom())
                        {
                            context.AddObject(logData);
                            context.SaveChanges();
                        }
                    }

                    syncKindomDataViewModel.ProgressValue += 100.0 * index / wellLogDatas.Count;
                    index++;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"CreateWellLogsToKindom failed: {ex.Message + ex.StackTrace}");
            }
        }


        public async Task CreateWellIntervalsToKindom(ResultData resultdata, List<WellCheckItem> wellCheckItems, SyncKindomDataViewModel syncKindomDataViewModel)
        {
            try
            {
                resultdata = resultdata.Clone();

                resultdata.wellIds = resultdata.wellIds.Where(o => wellCheckItems.FirstOrDefault(a => a.ID == o && a.IsChecked) != null).ToList();
                int allWellCount = resultdata.wellIds.Count;
                if (allWellCount == 0)
                {
                    LogManagerService.Instance.Log("No well logs selected for import.");
                    syncKindomDataViewModel.ProgressValue = 100;
                    return;
                }

                var wellDataService = ServiceLocator.GetService<IDataWellService>();

                int index = 1;
                List<string> wellIDList = resultdata.wellIds.ToList();

                foreach (var wellID in wellIDList)
                {
                    Borehole borehole = null;
                    var wellItem = wellCheckItems.FirstOrDefault(o => o.ID == wellID);
                    using (var context = project.GetKingdom())
                    {
                        borehole = context.Get<Borehole>(o => o.Uwi == wellItem.Name, false).FirstOrDefault();
                    }
                    ;
                    if (borehole == null)
                    {
                        LogManagerService.Instance.Log($"Well UWI {wellItem.Name} not found in Kingdom.");
                        continue;
                    }
                    resultdata.wellIds = new List<string>() { wellID };
                    var resData = await wellDataService.get_explain_well_log_list(resultdata);

                    if (resData.Count == 0)
                        continue;
                    IntervalName IntervalName =  GetOrCreateIntervalName(resData[0].row.Name);
                    int arrtID = GetOrCreateIntervalAttribute(IntervalName);

                    //foreach (var data in resData)
                    //{

                    //    IntervalRecord intervalRecord = new IntervalRecord()
                    //    {
                    //        Borehole = borehole,
                    //        StartDepth = data.row.Top,
                    //        EndDepth = data.row.Bottom,
                    //    };
                    //    intervalRecord.IntervalTextValues.Add(new IntervalTextValue()
                    //    {
                    //        IntervalAttributeId = arrtID,
                    //        Value = data.row.Result,
                    //    });
                    //    IntervalName.IntervalRecords.Add(intervalRecord);
                    //}
                    List<IntervalRecord> intervalRecords = new List<IntervalRecord>();
                    foreach (var data in resData)
                    {

                        IntervalRecord intervalRecord = new IntervalRecord()
                        {
                            Borehole = borehole,
                            StartDepth = data.row.Top,
                            EndDepth = data.row.Bottom,
                            IntervalNameId = IntervalName.Id,
                             EntityCRUDOption = CRUDOption.CreateOrUpdate
                        };
                        intervalRecord.IntervalTextValues.Add(new IntervalTextValue()
                        {
                            IntervalAttributeId = arrtID,
                            Value = data.row.Result,
                             EntityCRUDOption = CRUDOption.CreateOrUpdate
                        });

                         var record = intervalRecords.FirstOrDefault(o => o.NaturalKeyValues == intervalRecord.NaturalKeyValues);
                        if(record==null)
                        {
                            intervalRecords.Add(intervalRecord);
                        }
                        else
                        {

                        }
                    }

                    using (var context = project.GetKingdom())
                    {
                        context.AddObjects(intervalRecords);
                        context.SaveChanges();
                    }

                    syncKindomDataViewModel.ProgressValue += 100.0 * index / allWellCount;
                    index++;
                }

            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"CreateWellIntervalsToKindom failed: {ex.Message + ex.StackTrace}");
            }


        }

        private IntervalName GetOrCreateIntervalName(string intervalName)
        {
            IntervalName item = null;
            using (var context = project.GetKingdom())
            {
                item = context.Get<IntervalName>(o => o.Name == intervalName, false).FirstOrDefault();
            };

            if (item == null)
            {
                IntervalName IntervalName = new IntervalName
                {
                    Name = intervalName,
                    EntityCRUDOption = CRUDOption.CreateOrUpdate,
                };

                using (var context = project.GetKingdom())
                {
                    context.AddObject(IntervalName);
                    context.SaveChanges();
                    item = context.Get<IntervalName>(o => o.Name == intervalName, false).FirstOrDefault();
                }
            }

            return item;
        }

        private int GetOrCreateIntervalAttribute(IntervalName IntervalName)
        {
            IntervalAttribute item = null;
            using (var context = project.GetKingdom())
            {
                item = context.Get<IntervalAttribute>(o => o.Name == "Symbol"&& o.IntervalNameId == IntervalName.Id, false).FirstOrDefault();
            };

            if (item == null)
            {
                IntervalAttribute attr = new IntervalAttribute
                {
                    Name = "Symbol",
                    IntervalName = IntervalName,
                    IntervalAttributeType = AttributeType.Text, 
                    ColumnIndex = 3,
                    EntityCRUDOption = CRUDOption.CreateOrUpdate,
                };

                using (var context = project.GetKingdom())
                {
                    context.AddObject(attr);
                    context.SaveChanges();
                    item = context.Get<IntervalAttribute>(o => o.Name == "Symbol" && o.IntervalNameId == IntervalName.Id, false).FirstOrDefault();
                }
            }

            return item.Id;
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
                    LogCurveName = new LogCurveName
                    {
                        Name = "RHOB23",
                         Abbreviation = "RHOB23",
                        EntityCRUDOption = CRUDOption.CreateOrUpdate,
                        LogType = new LogType(CRUDOption.CreateOrUpdate)
                        {
                            Name = "test",
                             Abbreviation = "test",
                        }
                    },

                    DepthSampleRate = 0.125,
                    StartDepth = 0,
                };

                var data = ReadFirstTwoColumns();
                logData.SetLogDepthsAndValues(data.firstColumn, data.secondColumn);
                using (var context = project.GetKingdom())
                {
                    context.AddObject(logData);
                    //context.AddObject(formationTopPick);
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
