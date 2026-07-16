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
using Newtonsoft.Json;
using Smt;
using Smt.IO.LAS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        public DownLoadDataViewModel _DownLoadDataVM;
        public DownLoadDataViewModel DownLoadDataVM
        {
            get
            {
                return _DownLoadDataVM;
            }
            set
            {
                SetProperty(ref _DownLoadDataVM, value, nameof(DownLoadDataVM));

            }
        }
        public bool IsInitial { get; set; } = false;
        IDataWellService wellDataService = null;

        public ApiConfig  ApiConfig { get; set; }
        /// <summary>
        /// 二次接口返回的参数 
        /// </summary>
        public ApiConfig2 ApiConfig2 { get; set; }
      
        public KingdomAPI KingdomAPIInstance
        {
            get
            {
                return KingdomAPI.Instance;
            }
        }
        public ICommand SyncCommand { get; set; }
        public ICommand Sync2Command { get; set; }
        
        public ICommand NewLogDataSetCommand { get; set; }
        public ICommand ConclusionSettingCommand { get; set; }

        private DispatcherTimer delayTimer;

        public SyncKindomDataViewModel(ApiConfig ApiConfig)
        {
            this.ApiConfig = ApiConfig;
            wellDataService = ServiceLocator.GetService<IDataWellService>();
            SyncCommand = new DevExpress.Mvvm.AsyncCommand(SyncCommandAction);
            Sync2Command = new DevExpress.Mvvm.AsyncCommand(Sync2CommandAction);
          
            NewLogDataSetCommand = new DevExpress.Mvvm.AsyncCommand(NewLogDataSetCommandAction);
            ConclusionSettingCommand = new DevExpress.Mvvm.DelegateCommand(ConclusionSettingCommandAction);
            ConclusionSettingVM = ViewModelSource.Create(() => new ConclusionSettingViewModel());
            ConclusionSettingVM.ConclusionFileNameObjChanged += ConclusionSettingVM_ConclusionFileNameObjChanged;

            delayTimer = new DispatcherTimer();
            delayTimer.Interval = TimeSpan.FromSeconds(0.5);
            delayTimer.Tick += DelayTimer_Tick_RefreashConclusion;
            IsInitial = true;
            _ = iniByArgs();
            _ = Initial();
        }

        public async Task iniByArgs()
        {
            IsEnable = false;
            try
            {
                if (ApiConfig.type == 0)
                {
                    IsSyncToKingdom = false;
                }
                else
                {
                    IsSyncToKingdom = true;

                    ConfigRequest configRequest = ApiConfig.type == 1 ? ApiConfig.welllogdata : ApiConfig.resultdata;
                    //先同时启动两个任务（不 await）
                    var paraStrTask = wellDataService.get_style_content_by_category(configRequest);
                    var wellListTask = wellDataService.get_all_meta_objects_by_objecttype_in_protobuf(
                        new string[] { "WellInformation" }
                    );

                    // 等待两个任务都完成
                    await Task.WhenAll(paraStrTask, wellListTask);

                    // 再取结果
                    ApiConfig2 = await paraStrTask;
                    WellIDandNameList = await wellListTask;

                    //异步删除参数文件
                    //_ = wellDataService.del_style_file(ApiConfig.welllogdata.ToDelConfigRequest());

                    if (ApiConfig.type == 1)
                    {
                        DownLoadDataVM = new DownLoadDataViewModel(true);
                        DownLoadDataVM.Wells = new ObservableCollection<WellCheckItem>();
                        foreach (var item in ApiConfig2.welllogdata)
                        {
                            WellCheckItem wellCheckItem = new WellCheckItem()
                            {
                                ID = item.wellId,
                                Name = Utils.GetWellNameOrUWIByWellID(item.wellId, WellIDandNameList),
                                IsChecked = true,
                            };
                            DownLoadDataVM.Wells.Add(wellCheckItem);
                          
                            foreach (var log in item.curveOptions)
                            {
                                var res = wellCheckItem.Children.FirstOrDefault(o => o.ID == log.datasetId);
                                if (res == null)
                                {
                                    wellCheckItem.Children.Add(new WellCheckItem()
                                    {
                                        Name = log.dataSetName,
                                        ID = log.datasetId,
                                        IsChecked = true,
                                    });
                                }
                            }

                            foreach (var child in wellCheckItem.Children)
                            {
                                foreach (var log in item.curveOptions)
                                {
                                    if (child.ID == log.datasetId)
                                    {
                                        WellCheckItem logItem = new WellCheckItem()
                                        {
                                            Name = log.name,
                                            IsChecked = true,
                                        };
                                        child.Children.Add(logItem);
                                    }
                                }
                            }
                        }
                    }
                    else if (ApiConfig.type == 2)
                    {
                        DownLoadDataVM = new DownLoadDataViewModel(false);
                        DownLoadDataVM.Wells = new ObservableCollection<WellCheckItem>();
                        foreach (var item in ApiConfig2.resultdata.wellIds)
                        {
                            WellCheckItem wellCheckItem = new WellCheckItem()
                            {
                                ID = item,
                                Name = Utils.GetWellNameOrUWIByWellID(item, WellIDandNameList),
                                IsChecked = true,
                            };
                            DownLoadDataVM.Wells.Add(wellCheckItem);
                        }
                    }
                    DownLoadDataVM.WebProjectName = ApiConfig.projectname;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("iniByArgs failed !" + ex.StackTrace + ex.Message);
            }
            finally
            {
                IsEnable = true;
            }
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
                        _isLoadingKingdomWellSubsets = true;
                        KindomData = new ProjectResponse();
                        KingdomWellSubsets = new ObservableCollection<WellSubsetOption>();
                        SelectedKingdomWellSubset = null;
                        _isLoadingKingdomWellSubsets = false;
                        ProgressValue = 0;
                        KingdomAPI.Instance.SetProjectPath(_ProjectPath);

                        var config = ConfigManager.LoadConfig(_ProjectPath);
                        if (config != null)
                        {
                            DBUserName = config.Username;
                            DBPassword = config.Password;
                        }
                        else
                        {
                            DBUserName = "";
                            DBPassword = "";
                        }
                    }
                    catch (Exception ex)
                    {
                        _isLoadingKingdomWellSubsets = false;
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

        private bool _IsRememberPassword = true;
        public bool IsRememberPassword
        {
            get
            {
                return _IsRememberPassword;
            }
            set
            {

                SetProperty(ref _IsRememberPassword, value, nameof(IsRememberPassword));

            }
        }

        private string _WebProjectName;
        public string WebProjectName
        {
            get
            {
                return _WebProjectName;
            }
            set
            {

                SetProperty(ref _WebProjectName, value, nameof(WebProjectName));

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



        private bool _IsSyncToKingdom = true;
        public bool IsSyncToKingdom
        {
            get
            {
                return _IsSyncToKingdom;
            }
            set
            {

                SetProperty(ref _IsSyncToKingdom, value, nameof(IsSyncToKingdom));

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
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke(new Action(() => ProgressValue = value), DispatcherPriority.Background);
                    return;
                }

                SetProperty(ref _ProgressValue, value, nameof(ProgressValue));

            }
        }

        private class SyncProgressStep
        {
            public string Name { get; set; }
            public double Start { get; set; }
            public double End { get; set; }
        }

        private class SyncProgressStepWeight
        {
            public string Name { get; set; }
            public double Weight { get; set; }
        }

        private List<SyncProgressStep> _syncProgressSteps = new List<SyncProgressStep>();
        private SyncProgressStep _currentSyncProgressStep;

        private void InitializeSyncProgressPlan()
        {
            _syncProgressSteps = new List<SyncProgressStep>();
            List<SyncProgressStepWeight> stepWeights = new List<SyncProgressStepWeight>();

            if (IsSyncWellHeader)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellHeader", Weight = 1 });
            if (IsSyncWellFormation)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellFormation", Weight = 2 });
            if (IsSyncTrajectory)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellTrajs", Weight = 3 });
            if (IsSyncProduction)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellProduction", Weight = 2 });
            if (IsSyncIPProduction)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellTest", Weight = 2 });
            if (IsSyncWellLog)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellLogs", Weight = 5 });
            if (IsSyncConclusion)
                stepWeights.Add(new SyncProgressStepWeight { Name = "WellConclusions", Weight = 2 });

            if (stepWeights.Count == 0)
            {
                ProgressValue = 0;
                return;
            }

            double totalWeight = stepWeights.Sum(o => o.Weight);
            double currentStart = 0;
            for (int i = 0; i < stepWeights.Count; i++)
            {
                double stepSize = stepWeights[i].Weight * 100.0 / totalWeight;
                _syncProgressSteps.Add(new SyncProgressStep
                {
                    Name = stepWeights[i].Name,
                    Start = currentStart,
                    End = i == stepWeights.Count - 1 ? 100 : currentStart + stepSize
                });
                currentStart += stepSize;
            }

            ProgressValue = 0;
        }

        private void StartSyncProgressStep(string stepName)
        {
            _currentSyncProgressStep = _syncProgressSteps.FirstOrDefault(o => o.Name == stepName);
            if (_currentSyncProgressStep != null && ProgressValue < _currentSyncProgressStep.Start)
            {
                ProgressValue = _currentSyncProgressStep.Start;
            }
        }

        public void ReportCurrentStepProgress(double percent)
        {
            if (_currentSyncProgressStep == null)
            {
                return;
            }

            percent = Math.Max(0, Math.Min(100, percent));
            double value = _currentSyncProgressStep.Start + (_currentSyncProgressStep.End - _currentSyncProgressStep.Start) * percent / 100.0;
            if (value > ProgressValue)
            {
                ProgressValue = value;
            }
        }

        private void CompleteSyncProgressStep()
        {
            ReportCurrentStepProgress(100);
            _currentSyncProgressStep = null;
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
                RefreshSelectedWellsCount();
            }
        }

        private int _SelectedWellsCount;
        public int SelectedWellsCount
        {
            get
            {
                return _SelectedWellsCount;
            }
            set
            {
                SetProperty(ref _SelectedWellsCount, value, nameof(SelectedWellsCount));
            }
        }

        private void RefreshSelectedWellsCount()
        {
            SelectedWellsCount = KindomData?.Wells?.Count(o => o.IsChecked) ?? 0;
        }

        private bool _isLoadingKingdomWellSubsets;

        private ObservableCollection<WellSubsetOption> _KingdomWellSubsets = new ObservableCollection<WellSubsetOption>();
        public ObservableCollection<WellSubsetOption> KingdomWellSubsets
        {
            get
            {
                return _KingdomWellSubsets;
            }
            set
            {
                SetProperty(ref _KingdomWellSubsets, value, nameof(KingdomWellSubsets));
            }
        }

        private WellSubsetOption _SelectedKingdomWellSubset;
        public WellSubsetOption SelectedKingdomWellSubset
        {
            get
            {
                return _SelectedKingdomWellSubset;
            }
            set
            {
                if (value == null && KingdomWellSubsets != null && KingdomWellSubsets.Count > 0)
                {
                    var selectedSubset = _SelectedKingdomWellSubset ?? KingdomWellSubsets.FirstOrDefault();
                    _SelectedKingdomWellSubset = null;
                    SetProperty(ref _SelectedKingdomWellSubset, selectedSubset, nameof(SelectedKingdomWellSubset));
                    return;
                }

                if (_SelectedKingdomWellSubset == value)
                {
                    return;
                }

                SetProperty(ref _SelectedKingdomWellSubset, value, nameof(SelectedKingdomWellSubset));
                if (!_isLoadingKingdomWellSubsets && KindomData != null)
                {
                    Task.Run(() => LoadKingdomData());
                }
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

        private bool _IsShowKB = true;
        public bool IsShowKB
        {
            get
            {
                return _IsShowKB;
            }
            set
            {
                SetProperty(ref _IsShowKB, value, nameof(IsShowKB));
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

        private bool _IsSyncProduction = false;
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

        private bool _IsSyncIPProduction = false;
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

        private bool _IsSyncConclusion = false;
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
                   ConfigManager.SaveConfig(ProjectPath, DBUserName, DBPassword, IsRememberPassword);
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
                        LoadKingdomWellSubsets();
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



        private void LoadKingdomWellSubsets()
        {
            _isLoadingKingdomWellSubsets = true;
            try
            {
                var wellSubsets = KingdomAPI.Instance.GetWellSubsets();
                wellSubsets.Add(new WellSubsetOption
                {
                    Id = -1,
                    Name = "All Wells(No SubSet)",
                    IsLeftWells = true
                });
                wellSubsets.Add(new WellSubsetOption
                {
                    Id = 0,
                    Name = "All Wells",
                    IsNoSubset = true
                });

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    KingdomWellSubsets = new ObservableCollection<WellSubsetOption>(wellSubsets);
                    SelectedKingdomWellSubset = KingdomWellSubsets.FirstOrDefault();
                }));
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message);
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    KingdomWellSubsets = new ObservableCollection<WellSubsetOption>();
                    SelectedKingdomWellSubset = null;
                }));
            }
            finally
            {
                _isLoadingKingdomWellSubsets = false;
            }
        }

        private void LoadKingdomData()
        {
            try
            {
                IsEnable = false;
                KindomData = KingdomAPI.Instance.GetProjectData(SelectedKingdomWellSubset);
                if (KindomData == null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        DXMessageBox.Show("Kindom data loading failed！");
                    }));
                }
                else
                {
                    LoadConclusionFileNameObjAndTestUnits();

                    foreach (var item in KindomData.Wells)
                    {
                        item.PropertyChanged += Item_PropertyChanged1;
                    }

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
                if (sender is WellExport well)
                {
                    SelectedWellsCount += well.IsChecked ? 1 : -1;
                }
                else
                {
                    RefreshSelectedWellsCount();
                }
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
            try
            {
                obj.ConclusionSetting = ViewModelSource.Create(() => new ConclusionFileNameObjConclusionSetting());
                List<string> ConclusionNames = KingdomAPI.Instance.GetConclusionNames(KindomData, obj);
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
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("RefreshConclusionMappingItems failed !" + ex.StackTrace + ex.Message);
            }
        }



        public PbViewMetaObjectList WellIDandNameList = null;
        /// <summary>
        /// 同步
        /// </summary>
        /// <returns></returns>
        public async Task SyncCommandAction()
        {

            if(KindomData == null)
            {
                DXMessageBox.Show("Please load kingdom project data first!");
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

            Stopwatch stopwatch = Stopwatch.StartNew();
            IsEnable = false;
            try
            {

                ProgressValue = 0;
                InitializeSyncProgressPlan();
                LogManagerService.Instance.Log($"Kindom Data Synchronization start.");

                #region 读取井列表
                WellDataRequest wellDataRequest = new WellDataRequest();
                wellDataRequest.Items = new List<WellItemRequest>();
                KindomData.Wells.ForEach(well =>
                {
                    if (well.IsChecked == true)
                    {
                        WellItemRequest item = new WellItemRequest()
                        {
                            WellName = well.Uwi,
                            Alias = well.WellName,
                            WellNumber = well.WellNumber,
                            WellType = 0,
                            WellTrajectoryType = 0,
                        };
                        if (IsShowWellHeaderXY)
                        {
                            item.WellheadX = well.SurfaceX;
                            item.WellheadY = well.SurfaceY;
                            if (KingdomAPI.Instance.IsXYFeet)
                            {
                                item.WellheadX = item.WellheadX.ToMeters();
                                item.WellheadY = item.WellheadY.ToMeters();
                            }
                        }
                        if (IsShowKB)
                        {
                            item.Kb = well.Kb;
                            if (KingdomAPI.Instance.IsDepthFeet)
                            {
                                item.Kb = item.Kb.ToMeters();
                            }
                        }

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

                #endregion

                if (IsSyncWellHeader)
                {
                    StartSyncProgressStep("WellHeader");
                    int wellHeaderBatchSize = AdvancedSettingsConfig.GetWellHeaderBatchSize();
                    int wellHeaderCount = wellDataRequest.Items.Count;
                    if (wellHeaderCount > wellHeaderBatchSize)
                    {
                        int batchCount = (wellHeaderCount + wellHeaderBatchSize - 1) / wellHeaderBatchSize;
                        LogManagerService.Instance.Log($"WellHeader({wellHeaderCount}) start synchronize by {batchCount} batches, batch size:{wellHeaderBatchSize}.");
                        for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
                        {
                            WellDataRequest batchRequest = new WellDataRequest()
                            {
                                OverWriteFlag = wellDataRequest.OverWriteFlag,
                                Items = wellDataRequest.Items.Skip(batchIndex * wellHeaderBatchSize).Take(wellHeaderBatchSize).ToList()
                            };
                            var res = await wellDataService.batch_create_well_header(batchRequest);
                            if (res != null)
                            {

                            }
                            int syncedCount = Math.Min((batchIndex + 1) * wellHeaderBatchSize, wellHeaderCount);
                            ReportCurrentStepProgress(syncedCount * 100.0 / wellHeaderCount);
                            LogManagerService.Instance.Log($"WellHeader batch {batchIndex + 1}/{batchCount}({batchRequest.Items.Count}) synchronized. Synced {syncedCount}/{wellHeaderCount}");
                        }
                    }
                    else
                    {
                        var res = await wellDataService.batch_create_well_header(wellDataRequest);
                        if (res != null)
                        {

                        }
                    }
                    CompleteSyncProgressStep();
                    LogManagerService.Instance.Log($"WellHeader({wellHeaderCount}) synchronize over！");
                }

               
                WellIDandNameList = await wellDataService.get_all_meta_objects_by_objecttype_in_protobuf(new string[] { "WellInformation" });
                //井分层
                if (IsSyncWellFormation)
                {
                    StartSyncProgressStep("WellFormation");
                    int wellFormationCount = await Task.Run(() => KingdomAPI.Instance.GetWellFormation(KindomData, WellIDandNameList, this));
                    CompleteSyncProgressStep();
                    LogManagerService.Instance.Log($"WellFormation({wellFormationCount}) synchronize over.");

                }

                #region 井轨迹
                if (IsSyncTrajectory)
                {
                    StartSyncProgressStep("WellTrajs");
                    int wellTrajectoryCount = await Task.Run(() => KingdomAPI.Instance.GetWellTrajs(KindomData, WellIDandNameList, this));
                    CompleteSyncProgressStep();
                    LogManagerService.Instance.Log($"WellTrajs({wellTrajectoryCount}) synchronize over.");
                }
                #endregion

                #region 井产量
                if (IsSyncProduction)
                {
                    StartSyncProgressStep("WellProduction");
                    LogManagerService.Instance.Log($"Well Production Datas start synchronize！");

                    await KingdomAPI.Instance.CreateWellProductionDataToWeb(KindomData, WellIDandNameList, IsShowOil, IsShowGas, IsShowWater, this);
                  
                    //List<WellDailyProductionData> AllwellProductionDatas = KingdomAPI.Instance.GetWellProductionData(KindomData, WellIDandNameList, IsShowOil,IsShowGas,IsShowWater);

                    //if (AllwellProductionDatas.Count > 0)
                    //{
                    //    int AllwellTrajsCount = AllwellProductionDatas.Count;
                    //    List<WellProductionDataRequest> tempList = new List<WellProductionDataRequest>();
                    //    WellProductionDataRequest wellTrajRequest = null;
                    //    for (int i = 0; i < AllwellTrajsCount; i++)
                    //    {
                    //        if (i % 3 == 0)
                    //        {
                    //            wellTrajRequest = new WellProductionDataRequest();
                    //            tempList.Add(wellTrajRequest);
                    //            wellTrajRequest.Items.Add(AllwellProductionDatas[i]);
                    //        }
                    //        else
                    //        {
                    //            wellTrajRequest.Items.Add(AllwellProductionDatas[i]);
                    //        }
                    //    }
                    //    for (int i = 0; i < tempList.Count; i++)
                    //    {
                    //        var res4 = await wellDataService.batch_create_well_production_with_meta_infos(tempList[i]);
                    //        if (res4 != null)
                    //        {

                    //        }

                    //        LogManagerService.Instance.Log($"Well Production Datas synchronize ({(i + 1) * 3}/{AllwellTrajsCount})");
                    //        ProgressValue = 30 + ((i + 1) * 3 * 20) / AllwellTrajsCount;
                    //    }

                    //    LogManagerService.Instance.Log($"Well Production Datas synchronize ({AllwellTrajsCount}/{AllwellTrajsCount}) synchronize over！");
                    //}
                    //else
                    //{
                    //    LogManagerService.Instance.Log($"Well Production Data Count is 0");
                    //}

                    LogManagerService.Instance.Log($"Well Production Datas synchronize over！");
                    CompleteSyncProgressStep();

                }
                #endregion

                #region 试油试气

                if (IsSyncIPProduction)
                {
                    StartSyncProgressStep("WellTest");

                    (List<WellGasTestData>, List<WellOilTestData>) AllwellTestDatas = KingdomAPI.Instance.GetWellGasTestData(KindomData, WellIDandNameList, UnitMappingItems);

                    if (AllwellTestDatas.Item1.Count > 0)
                    {
                        LogManagerService.Instance.Log($"Well Gas Test Data start synchronize！");
                        int AllCount = AllwellTestDatas.Item1.Count;
                        List<WellGasTestRequest> tempList = new List<WellGasTestRequest>();
                        WellGasTestRequest wellTrajRequest = null;
                        for (int i = 0; i < AllCount; i++)
                        {
                            if (i == 0)
                            {
                                wellTrajRequest = new WellGasTestRequest();
                                tempList.Add(wellTrajRequest);
                                wellTrajRequest.Items.Add(AllwellTestDatas.Item1[i]);
                            }
                            else
                            {
                                var newItem = AllwellTestDatas.Item1[i];
                                bool isExist = false;
                                foreach (var RequestItem in tempList)
                                {
                                    var FirstItem = RequestItem.Items.FirstOrDefault();
                                    if (CompareMataInfo(FirstItem, newItem))//单位相同的一起处理
                                    {
                                        RequestItem.Items.Add(newItem);
                                        isExist = true;
                                    }
                                }

                                if(!isExist)
                                {
                                    wellTrajRequest = new WellGasTestRequest();
                                    tempList.Add(wellTrajRequest);
                                    wellTrajRequest.Items.Add(AllwellTestDatas.Item1[i]);
                                }

                            }
                        }
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            var res4 = await wellDataService.batch_create_well_gas_pressure_test_with_meta_infos(tempList[i]);
                            if (res4 != null)
                            {

                            }
                        }
                        LogManagerService.Instance.Log($"Well Gas Test Data synchronize synchronize over！");
                    }
                    else
                    {
                        LogManagerService.Instance.Log($"Well Gas Test Data Count is 0");
                    }

                    ReportCurrentStepProgress(50);

                    if (AllwellTestDatas.Item2.Count > 0)
                    {
                        LogManagerService.Instance.Log($"Well Oil Test Data start synchronize！");
                        int AllCount = AllwellTestDatas.Item2.Count;
                        List<WellOilTestDataRequset> tempList = new List<WellOilTestDataRequset>();
                        WellOilTestDataRequset wellTrajRequest = null;
                        for (int i = 0; i < AllCount; i++)
                        {
                            if (i == 0)
                            {
                                wellTrajRequest = new WellOilTestDataRequset();
                                tempList.Add(wellTrajRequest);
                                wellTrajRequest.Items.Add(AllwellTestDatas.Item2[i]);
                            }
                            else
                            {
                                var newItem = AllwellTestDatas.Item2[i];
                                bool isExist = false;
                                foreach (var RequestItem in tempList)
                                {
                                    var FirstItem = RequestItem.Items.FirstOrDefault();
                                    if (CompareMataInfo(FirstItem, newItem))//单位相同的一起处理
                                    {
                                        RequestItem.Items.Add(newItem);
                                        isExist = true;
                                    }
                                }
                                if (!isExist)
                                {
                                    wellTrajRequest = new WellOilTestDataRequset();
                                    tempList.Add(wellTrajRequest);
                                    wellTrajRequest.Items.Add(AllwellTestDatas.Item2[i]);
                                }
                            }

                        }
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            var res4 = await wellDataService.batch_create_well_oil_test_with_meta_infos(tempList[i]);
                            if (res4 != null)
                            {

                            }
                        }
                        LogManagerService.Instance.Log($"Well Oil Test Data synchronize synchronize over！");
                    }
                    else
                    {
                        LogManagerService.Instance.Log($"Well Oil Test Data Count is 0");
                    }
                    CompleteSyncProgressStep();
                }
                #endregion

                #region  井曲线 
                if (IsSyncWellLog)
                {
                    StartSyncProgressStep("WellLogs");
                    LogManagerService.Instance.Log($"WellLogs start synchronize！");
                    string resdataSetID = SelectedLogDataSet?.Id;

                    await Task.Run(() => KingdomAPI.Instance.CreateWellLogsToWeb(KindomData, WellIDandNameList, resdataSetID,this));

                    LogManagerService.Instance.Log($"WellLogs synchronize over！");
                    CompleteSyncProgressStep();

                }
                #endregion


                #region 解释结论
                if (IsSyncConclusion)
                {
                    StartSyncProgressStep("WellConclusions");
                    LogManagerService.Instance.Log($"WellConclusions start synchronize！");
                    Dictionary<string, CreatePayzoneRequest> requests = KingdomAPI.Instance.CreateWellConclusionsToWeb(KindomData, WellIDandNameList, ConclusionSettingVM.ConclusionFileNameObjItems.ToList());
                     var listRequests = requests.Values.ToList();
                    int allConclusionsCount = requests.Count;

  
                    for (int i = 0; i < listRequests.Count; i++)
                    {
                        if (listRequests[i].Items.Count > 0)
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
                        }
                           
                        LogManagerService.Instance.Log($"Intervals synchronize ({(i + 1)}/{allConclusionsCount})");
                        if (allConclusionsCount > 0)
                        {
                            ReportCurrentStepProgress((i + 1) * 100.0 / allConclusionsCount);
                        }
                    }
                    CompleteSyncProgressStep();
                }
   
                #endregion



                ProgressValue = 100;
                LogManagerService.Instance.Log($"Kindom data synchronize to web over!.");
                DXMessageBox.Show("Kindom data synchronize to web over!");

            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message + ex.StackTrace);
                DXMessageBox.Show("Data synchronize failed：" + ex.Message);
                return;
            }
            finally
            {
                stopwatch.Stop();
                LogManagerService.Instance.Log($"Kindom data synchronize to web elapsed time: {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}.");
                IsEnable = true;
            }
        }

        private async Task Sync2CommandAction()
        {

            if (KindomData == null)
            {
                DXMessageBox.Show("Please load kingdom project data first!");
                return;
            }

            if (DownLoadDataVM == null || DownLoadDataVM.Wells.Count == 0)
            {
                DXMessageBox.Show("Please select wells to synchronize!");
                return;
            }

            if (DownLoadDataVM.IsDownloadWellLog)
            {
                foreach (var item in DownLoadDataVM.Wells)
                {
                    List<string> curvenames = new List<string>();
                    foreach (var dataSet in item.Children)
                    {
                       foreach (var curve in dataSet.Children)
                        {
                            if (curve.IsChecked == true)
                            {
                                if (curvenames.Contains(curve.Name))
                                {
                                    DXMessageBox.Show($"Well {item.Name} has duplicate curve name {curve.Name}, please uncheck the repeat welllog which in different dataset!");
                                    return;
                                }
                                curvenames.Add(curve.Name);
                            }

                        }
                    }
                }
            }



            IsEnable = false;

            try
            {
                ProgressValue = 0;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                if (DownLoadDataVM.IsDownloadWellLog)
                {
                    List<WellLogData> getWellLogRequest = ApiConfig2.welllogdata;
                    bool res = await KingdomAPI.Instance.CreateWellLogsToKindom(getWellLogRequest, DownLoadDataVM.Wells, this);
                }
                else
                {
                    ResultData resultdata = ApiConfig2.resultdata;
                    await KingdomAPI.Instance.CreateWellIntervalsToKindom(resultdata,DownLoadDataVM.Wells, this);
                }
                ProgressValue = 100;
                stopwatch.Stop();

                var elapsed = stopwatch.Elapsed;
                string elapsedMsg = stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff");
                LogManagerService.Instance.Log($"Web data synchronize to kindom over!" + elapsedMsg);
                DXMessageBox.Show("Web data synchronize to kindom over!");
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log(ex.Message + ex.Message);
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


        private bool CompareMataInfo(WellGasTestData data1, WellGasTestData data2)
        {
            List<MetaInfo> a = data1.MetaInfoList;
            List<MetaInfo> b = data2.MetaInfoList;
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].MeasureId != b[i].MeasureId || a[i].UnitId != b[i].UnitId || a[i].DisplayName != b[i].DisplayName || a[i].PropertyName != b[i].PropertyName)
                    return false;
            }
            return true;
        }
        private bool CompareMataInfo(WellOilTestData data1, WellOilTestData data2)
        {
            List<MetaInfo> a = data1.MetaInfoList;
            List<MetaInfo> b = data2.MetaInfoList;
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].MeasureId != b[i].MeasureId || a[i].UnitId != b[i].UnitId || a[i].DisplayName != b[i].DisplayName || a[i].PropertyName != b[i].PropertyName)
                    return false;
            }
            return true;
        }

        
    }
}
