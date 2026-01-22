using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.Logger;
using DevExpress.XtraPrinting;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
using KindomDataAPIServer.Models.Settings;
using KindomDataAPIServer.Views;
using Microsoft.Win32;
using Smt;
using Smt.IO.LAS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Tet.GeoSymbol;
using Tet.GeoSymbol.UI;
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;
using UnitType = KindomDataAPIServer.Models.UnitType;

namespace KindomDataAPIServer.ViewModels
{
    public class SyncKindomDataViewModel : BindableBase
    {
        ISplashScreenManagerService waiter;
       public ConclusionSettingViewModel ConclusionSettingVM { set; get; }

        public bool IsInitial { get; set; } = false;
        IDataWellService wellDataService = null;


        public KingdomAPI KingdomAPIInstance
        {
            get
            {
                return KingdomAPI.Instance;
            }
        }
        public ICommand SyncCommand { get; set; }
        public ICommand NewLogDataSetCommand { get; set; }
        public ICommand ConclusionSettingCommand { get; set; }

        private DispatcherTimer delayTimer;

        public SyncKindomDataViewModel()
        {
            wellDataService = ServiceLocator.GetService<IDataWellService>();
            SyncCommand = new DevExpress.Mvvm.AsyncCommand(SyncCommandAction);
            NewLogDataSetCommand = new DevExpress.Mvvm.AsyncCommand(NewLogDataSetCommandAction);
            ConclusionSettingCommand = new DevExpress.Mvvm.DelegateCommand(ConclusionSettingCommandAction);
            ConclusionSettingVM = ViewModelSource.Create(() => new ConclusionSettingViewModel());
            ConclusionSettingVM.ConclusionFileNameObjChanged += ConclusionSettingVM_ConclusionFileNameObjChanged;
            delayTimer = new DispatcherTimer();
            delayTimer.Interval = TimeSpan.FromSeconds(0.5);
            delayTimer.Tick += DelayTimer_Tick_RefreashConclusion;
            IsInitial = true;
            _ = Initial();
        }

  

        private async Task Initial()
        {
            try
            {
                LogManagerService.Instance.Log("start load config...");


                string pathToFileName = System.AppDomain.CurrentDomain.BaseDirectory + "Configs\\UnitTypes.json";
                if (File.Exists(pathToFileName))
                {
                    string str = File.ReadAllText(pathToFileName);
                    Utils.UnitTypes = JsonHelper.ConvertFrom<List<UnitType>>(str);
                    KingdomAPI.Instance.ChokeSizeUnit = Utils.ChokeUnitInfos.FirstOrDefault(o => o.Abbr == "1/64 in");
                    KingdomAPI.Instance.FlowingTubingPressureUnit = Utils.PressureUnitInfos.FirstOrDefault(o => o.Abbr == "Mpa");
                    KingdomAPI.Instance.BottomHoleTemperatureUnit = Utils.TemperatureUnitInfos.FirstOrDefault(o => o.Abbr == "degC");
                    KingdomAPI.Instance.ChokeUnitInfos = Utils.ChokeUnitInfos;
                    KingdomAPI.Instance.PressureUnitInfos = Utils.PressureUnitInfos;
                    KingdomAPI.Instance.TemperatureUnitInfos = Utils.TemperatureUnitInfos;
                }

                //var res7 = await wellDataService.get_sys_unit();
                //if (res7 != null)
                //{
                //    Utils.UnitTypes = res7;                  
                //    foreach (var unit in Utils.UnitTypes)
                //    {
                //        foreach (var unit2 in unit.UnitInfoList)
                //        {
                //            unit2.MeasureID = unit.UnitTypeID;
                //        }
                //    }

                //    KingdomAPI.Instance.ChokeSizeUnit = Utils.ChokeUnitInfos.FirstOrDefault(o => o.Abbr == "1/64 in");
                //    KingdomAPI.Instance.FlowingTubingPressureUnit = Utils.PressureUnitInfos.FirstOrDefault(o => o.Abbr == "Mpa");
                //    KingdomAPI.Instance.BottomHoleTemperatureUnit = Utils.TemperatureUnitInfos.FirstOrDefault(o => o.Abbr == "degC");
                //    KingdomAPI.Instance.ChokeUnitInfos = Utils.ChokeUnitInfos;
                //    KingdomAPI.Instance.PressureUnitInfos = Utils.PressureUnitInfos;
                //    KingdomAPI.Instance.TemperatureUnitInfos = Utils.TemperatureUnitInfos;
                //}
                var res8 = await wellDataService.get_log_dic();
                if (res8 != null)
                {
                    Utils.LogDicts = res8;
                    LogDicts = Utils.LogDicts;
                }

                await UpdateLogDataSets();

                LogManagerService.Instance.Log("load config over!");
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("Initial failed !" + ex.StackTrace + ex.Message);
                DXMessageBox.Show(ex.Message + ex.StackTrace);
            }
            finally
            {
                IsInitial = false;
            }
        }


        public async Task UpdateLogDataSets()
        {
            var datasetInfos = await wellDataService.get_dataset_list();
            if (datasetInfos != null)
            {
                LogDataSets = datasetInfos;
                SelectedLogDataSet = LogDataSets.FirstOrDefault();
            }
        }

        #region Properties


        public void BrowseProjectPath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Select Project";
            openFileDialog.Filter = " (*.tks)|*.tks";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                ProjectPath = openFileDialog.FileName;
            }
        }
        private string _ProjectPath;
        public string ProjectPath
        {
            get
            {
                return _ProjectPath;
            }
            set
            {
                SetProperty(ref _ProjectPath, value, nameof(ProjectPath));
                if (!string.IsNullOrEmpty(_ProjectPath) && File.Exists(_ProjectPath))
                {
                    try
                    {
                        KindomData = new ProjectResponse();
                        ProgressValue = 0;

                        KingdomAPI.Instance.SetProjectPath(_ProjectPath);
                    }
                    catch (Exception ex)
                    {
                        DXMessageBox.Show("Load project users failed ! please try select project path again!");
                        LogManagerService.Instance.Log("Load project users failed !" + ex.StackTrace + ex.Message);
                        return;
                    }

                }
            }
        }

        public List<LogDictItem> _LogDicts;
        public List<LogDictItem> LogDicts
        {
            get
            {
                return _LogDicts;
            }
            set
            {
                SetProperty(ref _LogDicts, value, nameof(LogDicts));

            }
        }
        public List<LogSetInfo> _LogDataSets;
        public List<LogSetInfo> LogDataSets
        {
            get
            {
                return _LogDataSets;
            }
            set
            {
                SetProperty(ref _LogDataSets, value, nameof(LogDataSets));

            }
        }


        public LogSetInfo _SelectedLogDataSet;
        public LogSetInfo SelectedLogDataSet
        {
            get
            {
                return _SelectedLogDataSet;
            }
            set
            {
                SetProperty(ref _SelectedLogDataSet, value, nameof(SelectedLogDataSet));

            }
        }



        private string _LoginName;
        public string LoginName
        {
            get
            {
                return _LoginName;
            }
            set
            {

                SetProperty(ref _LoginName, value, nameof(LoginName));

            }
        }


        private string _DBUserName;
        public string DBUserName
        {
            get
            {
                return _DBUserName;
            }
            set
            {

                SetProperty(ref _DBUserName, value, nameof(DBUserName));

            }
        }


        private string _DBPassword;
        public string DBPassword
        {
            get
            {
                return _DBPassword;
            }
            set
            {

                SetProperty(ref _DBPassword, value, nameof(DBPassword));

            }
        }

        private List<string> _Authors;
        public List<string> Authors
        {
            get
            {
                return _Authors;
            }
            set
            {

                SetProperty(ref _Authors, value, nameof(Authors));
            }
        }


        private bool _IsEnable = true;
        public bool IsEnable
        {
            get
            {
                return _IsEnable;
            }
            set
            {

                SetProperty(ref _IsEnable, value, nameof(IsEnable));

            }
        }
        private double _ProgressValue;
        public double ProgressValue
        {
            get
            {
                return _ProgressValue;
            }
            set
            {

                SetProperty(ref _ProgressValue, value, nameof(ProgressValue));

            }
        }

        private Visibility _LoginGridVisiable = Visibility.Visible;
        public Visibility LoginGridVisiable
        {
            get
            {
                return _LoginGridVisiable;
            }
            set
            {

                SetProperty(ref _LoginGridVisiable, value, nameof(LoginGridVisiable));

            }
        }

        private ProjectResponse _KindomData;
        public ProjectResponse KindomData
        {
            get
            {
                return _KindomData;
            }
            set
            {
                SetProperty(ref _KindomData, value, nameof(KindomData));
            }
        }
        #region wellSetting

        private bool _IsShowWellHeaderXY = true;
        public bool IsShowWellHeaderXY
        {
            get
            {
                return _IsShowWellHeaderXY;
            }
            set
            {
                SetProperty(ref _IsShowWellHeaderXY, value, nameof(IsShowWellHeaderXY));
            }
        }

        private bool _IsShowWellBottomXY = true;
        public bool IsShowWellBottomXY
        {
            get
            {
                return _IsShowWellBottomXY;
            }
            set
            {
                SetProperty(ref _IsShowWellBottomXY, value, nameof(IsShowWellBottomXY));
            }
        }

        private bool _IsShowBL = true;
        public bool IsShowBL
        {
            get
            {
                return _IsShowBL;
            }
            set
            {
                SetProperty(ref _IsShowBL, value, nameof(IsShowBL));
            }
        }

        private bool _IsShowCountry = true;
        public bool IsShowCountry
        {
            get
            {
                return _IsShowCountry;
            }
            set
            {
                SetProperty(ref _IsShowCountry, value, nameof(IsShowCountry));
            }
        }

        private bool _IsShowState = true;
        public bool IsShowState
        {
            get
            {
                return _IsShowState;
            }
            set
            {
                SetProperty(ref _IsShowState, value, nameof(IsShowState));
            }
        }

        private bool _IsShowCounty = true;
        public bool IsShowCounty
        {
            get
            {
                return _IsShowCounty;
            }
            set
            {
                SetProperty(ref _IsShowCounty, value, nameof(IsShowCounty));
            }
        }

        private bool _IsSyncWellHeader = true;
        public bool IsSyncWellHeader
        {
            get
            {
                return _IsSyncWellHeader;
            }
            set
            {
                SetProperty(ref _IsSyncWellHeader, value, nameof(IsSyncWellHeader));
            }
        }

        private bool _IsSyncTrajectory = true;
        public bool IsSyncTrajectory
        {
            get
            {
                return _IsSyncTrajectory;
            }
            set
            {
                SetProperty(ref _IsSyncTrajectory, value, nameof(IsSyncTrajectory));
            }
        }
        private bool _IsSyncWellFormation = true;
        public bool IsSyncWellFormation
        {
            get
            {
                return _IsSyncWellFormation;
            }
            set
            {
                SetProperty(ref _IsSyncWellFormation, value, nameof(IsSyncWellFormation));
            }
        }

        private bool _IsSyncWellLog = true;
        public bool IsSyncWellLog
        {
            get
            {
                return _IsSyncWellLog;
            }
            set
            {
                SetProperty(ref _IsSyncWellLog, value, nameof(IsSyncWellLog));
            }
        }

        private bool _IsSyncProduction = true;
        public bool IsSyncProduction
        {
            get
            {
                return _IsSyncProduction;
            }
            set
            {
                SetProperty(ref _IsSyncProduction, value, nameof(IsSyncProduction));
            }
        }

        private bool _IsSyncIPProduction = true;
        public bool IsSyncIPProduction
        {
            get
            {
                return _IsSyncIPProduction;
            }
            set
            {
                SetProperty(ref _IsSyncIPProduction, value, nameof(IsSyncIPProduction));
            }
        }

        private bool _IsSyncConclusion = true;
        public bool IsSyncConclusion
        {
            get
            {
                return _IsSyncConclusion;
            }
            set
            {
                SetProperty(ref _IsSyncConclusion, value, nameof(IsSyncConclusion));
            }
        }

        private bool _IsShowOil = true;
        public bool IsShowOil
        {
            get
            {
                return _IsShowOil;
            }
            set
            {
                SetProperty(ref _IsShowOil, value, nameof(IsShowOil));
            }
        }


        private bool _IsShowGas = true;
        public bool IsShowGas
        {
            get
            {
                return _IsShowGas;
            }
            set
            {
                SetProperty(ref _IsShowGas, value, nameof(IsShowGas));
            }
        }


        private bool _IsShowWater = true;
        public bool IsShowWater
        {
            get
            {
                return _IsShowWater;
            }
            set
            {
                SetProperty(ref _IsShowWater, value, nameof(IsShowWater));
            }
        }




        private List<UnitMappingItem> _UnitMappingItems;
        public List<UnitMappingItem> UnitMappingItems
        {
            get
            {
                return _UnitMappingItems;
            }
            set
            {
                SetProperty(ref _UnitMappingItems, value, nameof(UnitMappingItems));
            }
        }

        #endregion

        #endregion



        public DelegateCommand LoginCommand => new DelegateCommand(() =>
        {
            if (!string.IsNullOrEmpty(ProjectPath))
            {
                ProgressValue = 0;
                KindomData = new ProjectResponse();
                Authors = new List<string>();
                LoginName = "";

                var res = KingdomAPI.Instance.LoginDB(DBUserName, DBPassword);

                if (res!=null)
                {
                    Authors = res;
                    LoginName = Authors.FirstOrDefault();
                    LoginGridVisiable = Visibility.Collapsed;
                }
                else
                {
                    DXMessageBox.Show("Login failed！ Please check log！");
                    LogManagerService.Instance.Log($"LoginDB failed！");
                }
            }
            else
            {
                DXMessageBox.Show("The projectpath cannot be empty！");
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (!string.IsNullOrEmpty(LoginName))
            {
                Task.Run(() =>
                {
                    IsEnable = false;
                    ProgressValue = 0;
                    KindomData = new ProjectResponse();
                    bool res = KingdomAPI.Instance.LoadByUser(LoginName);
                    if (!res)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            DXMessageBox.Show("load failed, please try again！");
                        }));
                    }
                    else
                    {
                        LoadKingdomData();
                    }
                    IsEnable = true;
                });
                 
            }
            else
            {
                DXMessageBox.Show("The username cannot be empty！");
            }
        });



        private void LoadKingdomData()
        {
            try
            {
                IsEnable = false;
                KindomData = KingdomAPI.Instance.GetProjectData();
                LoadConclusionFileNameObjAndTestUnits();

                foreach (var item in KindomData.Wells)
                {
                    item.PropertyChanged += Item_PropertyChanged1;
                }  
                if (KindomData == null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        DXMessageBox.Show("Kindom data loading failed！");
                    }));
                }
                else
                {
                    LogManagerService.Instance.Log($"Project {ProjectPath} Kindom data loading successful！");
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message);
                return;
            }
            finally
            {
                IsEnable = true;
            }
        }

        private void Item_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
           if(e.PropertyName == "IsChecked")
            {
                //延时执行
                delayTimer.Stop();
                delayTimer.Start();
            }
        }

        private void DelayTimer_Tick_RefreashConclusion(object sender, EventArgs e)
        {
            delayTimer.Stop();
            LoadConclusionFileNameObjAndTestUnits();
        }

        private void ConclusionSettingCommandAction()
        {
            if (KindomData == null)
                return;
        }

        /// <summary>
        /// 加载解释结论
        /// </summary>
        public void LoadConclusionFileNameObjAndTestUnits()
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                ConclusionSettingVM.ColumnNameDict = KingdomAPI.Instance.GetColumnNameDict(KindomData);
                ConclusionSettingVM.ConclusionFileNameObjItems.Clear();

                this.UnitMappingItems =  KingdomAPI.Instance.GetWellProductionTestDataUnits(KindomData);
            }));

        }

        /// <summary>
        /// 刷新映射
        /// </summary>
        /// <param name="obj"></param>
        private void ConclusionSettingVM_ConclusionFileNameObjChanged(ConclusionFileNameObj obj)
        {
            RefreshConclusionMappingItems(obj);
        }


        public void RefreshConclusionMappingItems(ConclusionFileNameObj obj)
        {
            obj.ConclusionSetting = ViewModelSource.Create(()=> new ConclusionFileNameObjConclusionSetting());
            List<string> ConclusionNames =  KingdomAPI.Instance.GetConclusionNames(KindomData, obj);
            ColorGenerator.ResetColorIndex();
            foreach (var item in ConclusionNames)
            {
                ConclusionMappingItem conclusionMappingItem = new ConclusionMappingItem()
                {
                    Color = ColorGenerator.GetNextColor(),
                    PolygonName = item,
                };
                obj.ConclusionSetting.ConclusionMappingItems.Add(conclusionMappingItem);
            }
        }



        public PbViewMetaObjectList WellIDandNameList = null;

        public async Task SyncCommandAction()
        {

            if(KindomData == null)
            {
                DXMessageBox.Show("Please load data first!");
                return;
            }

            if (IsSyncConclusion)
            {
                if(ConclusionSettingVM.ConclusionFileNameObjItems.Count == 0)
                {
                    DXMessageBox.Show("Please set conclusion config!");
                    return;
                }

                foreach (var obj in ConclusionSettingVM.ConclusionFileNameObjItems)
                {
                    if (obj.FileName == null)
                    {
                        DXMessageBox.Show("Please set all conclusion file name!");
                        return;
                    }

                    if (string.IsNullOrEmpty(obj.ColumnName))
                    {
                        DXMessageBox.Show("Please set all conclusion column name!");
                        return;
                    }

                    if (string.IsNullOrEmpty(obj.ConclusionSetting.NewConclusionName))
                    {
                        DXMessageBox.Show("Please set all conclusion ConclusionName name!");
                        return;
                    }

                    foreach (var item in obj.ConclusionSetting.ConclusionMappingItems)
                    {
                        if (string.IsNullOrEmpty(item.SymbolLibraryCode))
                        {
                            DXMessageBox.Show("Please set all conclusion Symbol!");
                            return;
                        }
                    }

                }

                var list = ConclusionSettingVM.ConclusionFileNameObjItems.Select(o => o.FileName.FileName + o.ColumnName).ToList();
                int allCount = list.Count;
                int disCount = list.Distinct().Count();

                if (allCount!=1 && allCount != disCount)
                {
                    DXMessageBox.Show("Please ensure that there are no duplicate conclusion files and column names!");
                    return;
                }
            }
  

            if(IsSyncWellLog && SelectedLogDataSet == null)
            {
                DXMessageBox.Show("Please select a logDataSet!");
                return;
            }

            IsEnable = false;
            try
            {

                ProgressValue = 0;


                    LogManagerService.Instance.Log($"Kindom Data Synchronization start.");
                    WellDataRequest wellDataRequest = new WellDataRequest();
                    wellDataRequest.Items = new List<WellItemRequest>();
                    KindomData.Wells.ForEach(well =>
                    {
                        if (well.IsChecked == true)
                        {
                            WellItemRequest item = new WellItemRequest()
                            {
                                WellName = well.Uwi,
                                Alias = well.WellName + "-" + well.BoreholeName,
                                WellNumber = well.WellNumber,
                                WellType = 0,
                                WellTrajectoryType = 0,
                            };
                            if (IsShowWellHeaderXY)
                            {
                                item.WellheadX = well.SurfaceX;
                                item.WellheadY = well.SurfaceY;
                            }
                            //if (IsShowWellBottomXY)
                            //{
                            //    item.WellboreBottomX = well.BottomX;
                            //    item.WellboreBottomY = well.BottomY;
                            //}
                            if (IsShowBL)
                            {
                                item.Latitude = well.Latitude;
                                item.Longitude = well.Longitude;
                            }
                            if (IsShowCountry)
                            {
                                item.Country = well.Country;
                            }

                            if (IsShowState)
                            {
                                item.Region = well.Region;
                            }
                            if (IsShowCounty)
                            {
                                item.Districts = well.Districts;
                            }

                            wellDataRequest.Items.Add(item);
                        }

                    });

                if (wellDataRequest.Items.Count == 0)
                {
                    IsEnable = false;
                    DXMessageBox.Show("The number of wells selected cannot be 0");
                    return;
                }

                if (IsSyncWellHeader)
                {
                    var res = await wellDataService.batch_create_well_header(wellDataRequest);
                    if (res != null)
                    {

                    }
                    LogManagerService.Instance.Log($"WellHeader({wellDataRequest.Items.Count}) synchronize over！");
                }

                ProgressValue = 10;

               
                WellIDandNameList = await wellDataService.get_all_meta_objects_by_objecttype_in_protobuf(new string[] { "WellInformation" });

                if (IsSyncWellFormation)
                {
                    PbWellFormationList pbWellFormationList = KingdomAPI.Instance.GetWellFormation(KindomData, WellIDandNameList);

                    var tsk3 = await wellDataService.batch_create_well_formation(pbWellFormationList);

                    if (tsk3 != null)
                    {
                    }
                    ProgressValue = 20;
                    LogManagerService.Instance.Log($"WellFormation({pbWellFormationList.Datas.Count}) synchronize over！");

                }

                #region 井轨迹
                if (IsSyncTrajectory)
                {
                    LogManagerService.Instance.Log($"WellTrajs start synchronize！");

                    List<WellTrajData> AllwellTrajs = KingdomAPI.Instance.GetWellTrajs(KindomData, WellIDandNameList);

                    if (AllwellTrajs.Count > 0)
                    {
                        int AllwellTrajsCount = AllwellTrajs.Count;
                        List<WellTrajRequest> tempList = new List<WellTrajRequest>();
                        WellTrajRequest wellTrajRequest = null;
                        for (int i = 0; i < AllwellTrajsCount; i++)
                        {
                            if (i % 3 == 0)
                            {
                                wellTrajRequest = new WellTrajRequest();
                                tempList.Add(wellTrajRequest);
                                wellTrajRequest.Items.Add(AllwellTrajs[i]);
                            }
                            else
                            {
                                wellTrajRequest.Items.Add(AllwellTrajs[i]);
                            }
                        }
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            var res4 = await wellDataService.batch_create_well_trajectory_with_meta_infos(tempList[i]);
                            if (res4 != null)
                            {

                            }
                            LogManagerService.Instance.Log($"WellTrajs synchronize ({(i + 1) * 3}/{AllwellTrajsCount})");
                            ProgressValue = 20 + ((i + 1) * 3 * 10) / AllwellTrajsCount;
                        }

                        LogManagerService.Instance.Log($"WellTrajs synchronize ({AllwellTrajsCount}/{AllwellTrajsCount}) synchronize over！");
                    }
                    else
                    {
                        LogManagerService.Instance.Log($"WellTrajs Count is 0");
                    }
                }
                #endregion

                ProgressValue = 30;

                #region 井产量
                if (IsSyncProduction)
                {
                    LogManagerService.Instance.Log($"Well Production Datas start synchronize！");

                    List<WellDailyProductionData> AllwellProductionDatas = KingdomAPI.Instance.GetWellProductionData(KindomData, WellIDandNameList, IsShowOil,IsShowGas,IsShowWater);

                    if (AllwellProductionDatas.Count > 0)
                    {
                        int AllwellTrajsCount = AllwellProductionDatas.Count;
                        List<WellProductionDataRequest> tempList = new List<WellProductionDataRequest>();
                        WellProductionDataRequest wellTrajRequest = null;
                        for (int i = 0; i < AllwellTrajsCount; i++)
                        {
                            if (i % 3 == 0)
                            {
                                wellTrajRequest = new WellProductionDataRequest();
                                tempList.Add(wellTrajRequest);
                                wellTrajRequest.Items.Add(AllwellProductionDatas[i]);
                            }
                            else
                            {
                                wellTrajRequest.Items.Add(AllwellProductionDatas[i]);
                            }
                        }
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            var res4 = await wellDataService.batch_create_well_production_with_meta_infos(tempList[i]);
                            if (res4 != null)
                            {

                            }

                            LogManagerService.Instance.Log($"Well Production Datas synchronize ({(i + 1) * 3}/{AllwellTrajsCount})");
                            ProgressValue = 30 + ((i + 1) * 3 * 20) / AllwellTrajsCount;
                        }

                        LogManagerService.Instance.Log($"Well Production Datas synchronize ({AllwellTrajsCount}/{AllwellTrajsCount}) synchronize over！");
                    }
                    else
                    {
                        LogManagerService.Instance.Log($"Well Production Data Count is 0");
                    }
                }
                #endregion

                ProgressValue = 50;

                #region 试油试气

                if (IsSyncIPProduction)
                {

                    (List<WellGasTestData>, List<WellOilTestData>) AllwellTestDatas = KingdomAPI.Instance.GetWellGasTestData(KindomData, WellIDandNameList);

                    if (AllwellTestDatas.Item1.Count > 0)
                    {
                        LogManagerService.Instance.Log($"Well Gas Test Data start synchronize！");
                        int AllCount = AllwellTestDatas.Item1.Count;
                        List<WellGasTestRequest> tempList = new List<WellGasTestRequest>();
                        WellGasTestRequest wellTrajRequest = null;
                        for (int i = 0; i < AllCount; i++)
                        {
                            if (i % 3 == 0)
                            {
                                wellTrajRequest = new WellGasTestRequest();
                                tempList.Add(wellTrajRequest);
                                wellTrajRequest.Items.Add(AllwellTestDatas.Item1[i]);
                            }
                            else
                            {
                                wellTrajRequest.Items.Add(AllwellTestDatas.Item1[i]);
                            }
                        }
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            var res4 = await wellDataService.batch_create_well_gas_pressure_test_with_meta_infos(tempList[i]);
                            if (res4 != null)
                            {

                            }
                        }
                        ProgressValue = 55;
                        LogManagerService.Instance.Log($"Well Gas Test Data synchronize synchronize over！");
                    }
                    else
                    {
                        LogManagerService.Instance.Log($"Well Gas Test Data Count is 0");
                    }


                    if (AllwellTestDatas.Item2.Count > 0)
                    {
                        LogManagerService.Instance.Log($"Well Oil Test Data start synchronize！");
                        int AllCount = AllwellTestDatas.Item2.Count;
                        List<WellOilTestDataRequset> tempList = new List<WellOilTestDataRequset>();
                        WellOilTestDataRequset wellTrajRequest = null;
                        for (int i = 0; i < AllCount; i++)
                        {
                            if (i % 3 == 0)
                            {
                                wellTrajRequest = new WellOilTestDataRequset();
                                tempList.Add(wellTrajRequest);
                                wellTrajRequest.Items.Add(AllwellTestDatas.Item2[i]);
                            }
                            else
                            {
                                wellTrajRequest.Items.Add(AllwellTestDatas.Item2[i]);
                            }
                        }
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            var res4 = await wellDataService.batch_create_well_oil_test_with_meta_infos(tempList[i]);
                            if (res4 != null)
                            {

                            }
                        }
                        ProgressValue = 60;
                        LogManagerService.Instance.Log($"Well Oil Test Data synchronize synchronize over！");
                    }
                    else
                    {
                        LogManagerService.Instance.Log($"Well Oil Test Data Count is 0");
                    }
                }
                #endregion
                ProgressValue = 60;

                #region  井曲线 
                if (IsSyncWellLog)
                {
                    LogManagerService.Instance.Log($"WellLogs start synchronize！");
                    string resdataSetID = SelectedLogDataSet?.Id;

                    await KingdomAPI.Instance.CreateWellLogsToWeb(KindomData, WellIDandNameList, resdataSetID);
                }
                ProgressValue = 80;
                #endregion


                #region 解释结论
                if (IsSyncConclusion)
                {
                    LogManagerService.Instance.Log($"WellConclusions start synchronize！");
                    Dictionary<string, CreatePayzoneRequest> requests = KingdomAPI.Instance.CreateWellConclusionsToWeb(KindomData, WellIDandNameList, ConclusionSettingVM.ConclusionFileNameObjItems.ToList());
                     var listRequests = requests.Values.ToList();
                    int allConclusionsCount = requests.Count;

  
                    for (int i = 0; i < listRequests.Count; i++)
                    {
                        if (listRequests[i].DatasetType == 1)
                        {
                            var res4 = await wellDataService.batch_create_well_payzone_with_meta_infos(listRequests[i]);
                        }
                        else if (listRequests[i].DatasetType == 2)
                        {
                            var res5 = await wellDataService.batch_create_well_lithology_with_meta_infos(listRequests[i]);
                        }
                        else if (listRequests[i].DatasetType == 3)
                        {
                            var res6 = await wellDataService.batch_create_well_facies_with_meta_infos(listRequests[i]);
                        }

                        LogManagerService.Instance.Log($"Intervals synchronize ({(i + 1) * 3}/{allConclusionsCount})");
                        ProgressValue = 80 + ((i + 1) * 3 * 20) / allConclusionsCount;
                    }                          
                }
   
                #endregion



                ProgressValue = 100;
                LogManagerService.Instance.Log($"Kindom data synchronize over!.");
                DXMessageBox.Show("Kindom data synchronize over!");

            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message + ex.StackTrace);
                DXMessageBox.Show("Data synchronize failed：" + ex.Message);
                return;
            }
            finally
            {
                 IsEnable = true;
            }
        }

        public async Task NewLogDataSetCommandAction()
        {
            LogDatasetCreateView logDatasetCreateView = new LogDatasetCreateView();
            if (logDatasetCreateView.ShowDialog() == true)
            {
                IsEnable = false;
                try
                {
                    var resdataSetID = await wellDataService.create_well_log_set(logDatasetCreateView.NewName);

                    if (string.IsNullOrWhiteSpace(resdataSetID))
                    {
                        LogManagerService.Instance.Log($"create_well_log_set ID is null");
                    }
                    else
                    {
                        await UpdateLogDataSets();
                        LogManagerService.Instance.Log($"Create New resdataSetID: {resdataSetID} successful!");
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.Instance.Log($"Create New LogSetID error: {ex.Message} ");
                }finally { IsEnable = true; }
 
            }
        }
    }
}
