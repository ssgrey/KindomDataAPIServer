using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.Logger;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
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
using Tet.GeoSymbol;
using Tet.GeoSymbol.UI;
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;
using UnitType = KindomDataAPIServer.Models.UnitType;

namespace KindomDataAPIServer.ViewModels
{
    public class SyncKindomDataViewModel : BindableBase
    {
       public ConclusionSettingViewModel ConclusionSettingVM { set; get; }


        IDataWellService wellDataService = null;

        public ICommand SyncCommand { get; set; }
        public ICommand NewLogDataSetCommand { get; set; }
        public ICommand ConclusionSettingCommand { get; set; }
        
        public SyncKindomDataViewModel()
        {
            wellDataService = ServiceLocator.GetService<IDataWellService>();
            SyncCommand = new DevExpress.Mvvm.AsyncCommand(SyncCommandAction);
            NewLogDataSetCommand = new DevExpress.Mvvm.AsyncCommand(NewLogDataSetCommandAction);
            ConclusionSettingCommand = new DevExpress.Mvvm.DelegateCommand(ConclusionSettingCommandAction);
            ConclusionSettingVM = ViewModelSource.Create(() => new ConclusionSettingViewModel());
            _ = Initial();
        }



        private async Task Initial()
        {
            var res7 = await wellDataService.get_sys_unit();
            if (res7 != null)
            {
                Utils.UnitTypes = res7;

                foreach (var unit in Utils.UnitTypes)
                {
                    foreach (var unit2 in unit.UnitInfoList)
                    {
                        unit2.MeasureID = unit.UnitTypeID;
                    }
                }
            }
            var res8 = await wellDataService.get_log_dic();
            if (res8 != null)
            {
                Utils.LogDicts = res8;
                LogDicts = Utils.LogDicts;
            }

           await UpdateLogDataSets();
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

        private bool _IsSyncLogType = true;
        public bool IsSyncLogType
        {
            get
            {
                return _IsSyncLogType;
            }
            set
            {
                SetProperty(ref _IsSyncLogType, value, nameof(IsSyncLogType));
            }
        }

        private bool _IsSyncLogUnit = true;
        public bool IsSyncLogUnit
        {
            get
            {
                return _IsSyncLogUnit;
            }
            set
            {
                SetProperty(ref _IsSyncLogUnit, value, nameof(IsSyncLogUnit));
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


        string[] _displayCatalogs = new string[0];
        public string[] DisplayCatalogs
        {
            get { return _displayCatalogs; }
            set { _displayCatalogs = value; }
        }
        private void ConclusionSettingCommandAction()
        {
            if (KindomData == null)
                return;
            ConclusionSettingView conclusionSettingView = new ConclusionSettingView(this);
            conclusionSettingView.ShowDialog();
        }


        public PbViewMetaObjectList WellIDandNameList = null;

        public async Task SyncCommandAction()
        {

            List<SymbolMappingDto> SymbolMapping = new List<SymbolMappingDto>();
            foreach (var ConclusionMappingItem in ConclusionSettingVM.ConclusionMappingItems)
            {
                SymbolMappingDto temp = new SymbolMappingDto();
                temp.Color = Utils.ColorToInt(ConclusionMappingItem.Color);
                temp.ConclusionName = ConclusionMappingItem.PolygonName;
                temp.SymbolLibraryCode = ConclusionMappingItem.SymbolLibraryCode;
                if(string.IsNullOrEmpty(ConclusionMappingItem.SymbolLibraryCode))
                {
                    DXMessageBox.Show("Please set all conclusion item code");
                    return;
                }
                SymbolMapping.Add(temp);
            }

            if(SelectedLogDataSet == null)
            {
                DXMessageBox.Show("Please select a logDataSet!");
                return;
            }

            IsEnable = false;
            try
            {

                ProgressValue = 0;

                if (IsSyncWellHeader)
                {
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

                    var res = await wellDataService.batch_create_well_header(wellDataRequest);
                    if (res != null)
                    {

                    }
                    LogManagerService.Instance.Log($"WellHeader({wellDataRequest.Items.Count}) synchronize over！");
                }

                ProgressValue = 10;

                WellIDandNameList = await wellDataService.get_all_meta_objects_by_objecttype_in_protobuf(new string[] { "WellInformation" });


                PbWellFormationList pbWellFormationList = KingdomAPI.Instance.GetWellFormation(KindomData, WellIDandNameList);

                var tsk3 = await wellDataService.batch_create_well_formation(pbWellFormationList);

                if (tsk3 != null)
                {
                }
                ProgressValue = 20;
                LogManagerService.Instance.Log($"WellFormation({pbWellFormationList.Datas.Count}) synchronize over！");


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
                ProgressValue = 50;

                #endregion

                #region 井产量


                LogManagerService.Instance.Log($"Well Production Datas start synchronize！");

                List<WellDailyProductionData> AllwellProductionDatas = KingdomAPI.Instance.GetWellProductionData(KindomData, WellIDandNameList);

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


                #endregion

                #region 试油试气

                (List<WellGasTestData>, List<WellOilTestData>) AllwellTestDatas = KingdomAPI.Instance.GetWellGasTestData(KindomData, WellIDandNameList);

                if (AllwellTestDatas.Item1.Count>0)
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

                #endregion

                #region  井曲线 

                LogManagerService.Instance.Log($"WellLogs start synchronize！");
                string resdataSetID = SelectedLogDataSet.Id;

                await KingdomAPI.Instance.CreateWellLogsToWeb(KindomData,  WellIDandNameList, resdataSetID, IsSyncLogType, IsSyncLogUnit);
                ProgressValue = 80;
                #endregion


                #region 解释结论
                LogManagerService.Instance.Log($"WellConclusions start synchronize！");

                //List<SymbolMappingDto> SymbolMapping = new List<SymbolMappingDto>();
                //SymbolMappingDto symbolMappingDto = new SymbolMappingDto();
                //symbolMappingDto.Color = Utils.ColorToInt(Colors.Red);
                //symbolMappingDto.ConclusionName = "1";
                //symbolMappingDto.SymbolLibraryCode = "44C0010";
                //SymbolMapping.Add(symbolMappingDto);


                List<DatasetItemDto> Conclusions = KingdomAPI.Instance.GetWellConclusion(KindomData, WellIDandNameList, SymbolMapping);

                if (Conclusions!=null && Conclusions.Count > 0)
                {


                    string NewConclusionName = string.IsNullOrEmpty(ConclusionSettingVM.NewConclusionName) ? "一次解释" : ConclusionSettingVM.NewConclusionName;
                    int AllwellTrajsCount = Conclusions.Count;
                    List<CreatePayzoneRequest> tempList = new List<CreatePayzoneRequest>();
                    CreatePayzoneRequest wellTrajRequest = null;
                    for (int i = 0; i < AllwellTrajsCount; i++)
                    {
                        if (i % 3 == 0)
                        {
                            wellTrajRequest = new CreatePayzoneRequest();
                           
                            if (ConclusionSettingVM.ExplanationType == ExplanationType.Payzon)
                            {
                                wellTrajRequest.DatasetType = 1;
                            }
                            else if (ConclusionSettingVM.ExplanationType == ExplanationType.Lithology)
                            {
                                wellTrajRequest.DatasetType = 2;
                            }
                            else if (ConclusionSettingVM.ExplanationType == ExplanationType.SedimentaryFacies)
                            {
                                wellTrajRequest.DatasetType = 3;
                            }
                            wellTrajRequest.DatasetName = NewConclusionName;
                            wellTrajRequest.SymbolMapping = SymbolMapping;

                            tempList.Add(wellTrajRequest);
                            wellTrajRequest.Items.Add(Conclusions[i]);
                        }
                        else
                        {
                            wellTrajRequest.Items.Add(Conclusions[i]);
                        }
                    }
                    for (int i = 0; i < tempList.Count; i++)
                    {
                        if (ConclusionSettingVM.ExplanationType == ExplanationType.Payzon)
                        {
                            var res4 = await wellDataService.batch_create_well_payzone_with_meta_infos(tempList[i]);
                        }
                        else if (ConclusionSettingVM.ExplanationType == ExplanationType.Lithology)
                        {
                            var res5 = await wellDataService.batch_create_well_lithology_with_meta_infos(tempList[i]);
                        }
                        else if (ConclusionSettingVM.ExplanationType == ExplanationType.SedimentaryFacies)
                        {
                            var res6 = await wellDataService.batch_create_well_facies_with_meta_infos(tempList[i]);
                        }

                        LogManagerService.Instance.Log($"WellConclusions synchronize ({(i + 1) * 3}/{AllwellTrajsCount})");
                        ProgressValue = 80 + ((i + 1) * 3 * 20) / AllwellTrajsCount;
                    }

                    LogManagerService.Instance.Log($"WellConclusions synchronize ({AllwellTrajsCount}/{AllwellTrajsCount}) synchronize over！");
                }
                else
                {
                    LogManagerService.Instance.Log($"WellConclusions Count is 0");
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
