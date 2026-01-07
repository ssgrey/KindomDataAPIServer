using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.Logger;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
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
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;

namespace KindomDataAPIServer.ViewModels
{
    public class SyncKindomDataViewModel : BindableBase
    {
        IDataWellService wellDataService = null;

        public void BrowseProjectPath()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Kindom Project File|*.tks";
            if (dialog.ShowDialog() == true)
            {
                ProjectPath = dialog.FileName;
            }
        }

        public ICommand SyncCommand { get; set; }
        public ICommand LoadSyncCommand { get; set; }
        public SyncKindomDataViewModel()
        {
            wellDataService = ServiceLocator.GetService<IDataWellService>();
            SyncCommand = new DevExpress.Mvvm.AsyncCommand(SyncCommandAction);
            LoadSyncCommand = new DevExpress.Mvvm.AsyncCommand(LoadSyncCommandAction);
        }


        #region Properties
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
                }
                else
                {
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
                ProgressValue = 0;
                KindomData = new ProjectResponse();
                bool res = KingdomAPI.Instance.LoadByUser(LoginName);
                if (!res)
                {
                    DXMessageBox.Show("load failed, please try again！");
                }
                else
                {
                    LoadKingdomData();
                }                     
            }
            else
            {
                DXMessageBox.Show("The username cannot be empty！");
            }
        });

        private async Task LoadSyncCommandAction()
        {

            await Task.Run(() =>
            {

            });
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

        private void LoadKingdomData()
        {
            try
            {
                IsEnable = false;
                KindomData = KingdomAPI.Instance.GetProjectData();
                LogManagerService.Instance.Log( $"Project {ProjectPath} Kindom data loading successful！");
            }
            catch (Exception ex)
            {
                string res = ex.Message;
                if (ex.InnerException != null)
                {
                    res += ex.InnerException.Message;
                }
                DXMessageBox.Show("Kindom data loading failed：" + res);
                return;
            }
            finally
            {
                IsEnable = true;
            }
        }

        PbViewMetaObjectList WellIDandNameList = null;




        public void CommandAction()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsEnable = false;
            });
            try
            {
                LogManagerService.Instance.Log($"Kindom wellHeader start synchronize.");

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
                            Country = well.Country,
                            Region = well.Region,
                            Districts = well.Districts,
                            WellheadX = well.SurfaceX,
                            WellheadY = well.SurfaceY,
                            WellboreBottomX = well.BottomX,
                            WellboreBottomY = well.BottomY,
                            Longitude = well.Longitude,
                            Latitude = well.Latitude,

                        };
                        wellDataRequest.Items.Add(item);
                    }

                });
                var tsk = Task.Run(() => wellDataService.batch_create_well_header(wellDataRequest));
                tsk.Wait();
                if (tsk.Result !=null)
                {

                }
                LogManagerService.Instance.Log($"WellHeader({wellDataRequest.Items.Count}) synchronize over！");

                var tsk2 = Task.Run(() => wellDataService.get_all_meta_objects_by_objecttype_in_protobuf(new string[] { "WellInformation" }));
                tsk2.Wait();
                if (tsk2.Result != null)
                {
                    WellIDandNameList = tsk2.Result;                  
                }

                PbWellFormationList pbWellFormationList = KingdomAPI.Instance.GetWellFormation(KindomData, WellIDandNameList);

                var tsk3 = Task.Run(() => wellDataService.batch_create_well_formation(pbWellFormationList));
                tsk3.Wait();

                if (tsk3.Result != null)
                {
                }
                LogManagerService.Instance.Log($"WellFormation({pbWellFormationList.Datas.Count}) synchronize over！");


                PbWellLogCreateList wellLogs = KingdomAPI.Instance.GetWellLogs(KindomData,"", WellIDandNameList);

                if (wellLogs.LogList.Count > 0)
                {
                    var tsk4 = Task.Run(() => wellDataService.batch_create_well_log(wellLogs));
                    tsk4.Wait();

                    if (tsk4.Result != null)
                    {
                    }
                    LogManagerService.Instance.Log($"WellFormation({wellLogs.LogList.Count}) synchronize over！");
                }
                else
                {
                    LogManagerService.Instance.Log($"WellLog Count is 0 ！");
                }

            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Data synchronization failed：" + ex.Message);
                return;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsEnable = true;
                });
            }
        }

        public async Task SyncCommandAction()
        {
             IsEnable = false;

            try
            {
                LogManagerService.Instance.Log($"Kindom Data Synchronization start.");
                ProgressValue = 0;

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
                            Country = well.Country,
                            Region = well.Region,
                            Districts = well.Districts,
                            WellheadX = well.SurfaceX,
                            WellheadY = well.SurfaceY,
                            WellboreBottomX = well.BottomX,
                            WellboreBottomY = well.BottomY,
                            Longitude = well.Longitude,
                            Latitude = well.Latitude,

                        };
                        wellDataRequest.Items.Add(item);
                    }

                });

                if(wellDataRequest.Items.Count == 0)
                {
                    IsEnable = false;
                    DXMessageBox.Show("The number of wells selected cannot be 0");
                    return;
                }

                var res = await wellDataService.batch_create_well_header(wellDataRequest);
                if (res != null)
                {
                    
                }
                ProgressValue = 10;
                LogManagerService.Instance.Log($"WellHeader({wellDataRequest.Items.Count}) synchronize over！");

                WellIDandNameList = await wellDataService.get_all_meta_objects_by_objecttype_in_protobuf(new string[] { "WellInformation" });

                //LogManagerService.Instance.Log($"WellFormation start synchronize！");

                PbWellFormationList pbWellFormationList = KingdomAPI.Instance.GetWellFormation(KindomData, WellIDandNameList);

                var tsk3 = await wellDataService.batch_create_well_formation(pbWellFormationList);

                if (tsk3 != null)
                {
                }
                ProgressValue = 20;
                LogManagerService.Instance.Log($"WellFormation({pbWellFormationList.Datas.Count}) synchronize over！");

                string resdataSetID = "";
                var datasetInfos = await wellDataService.get_dataset_list();
                if (datasetInfos != null&& datasetInfos.Count>= 0)
                {
                    resdataSetID = datasetInfos[0].Id;
                }
                else
                {
                    resdataSetID = await wellDataService.create_well_log_set("KindomDataset");
                }

              
                LogManagerService.Instance.Log($"WellLogs start synchronize！");

                PbWellLogCreateList AllwellLogs = KingdomAPI.Instance.GetWellLogs(KindomData, resdataSetID, WellIDandNameList);

                if (AllwellLogs.LogList.Count > 0)
                {
                    int allCount = AllwellLogs.LogList.Count;
                    List<PbWellLogCreateList> tempList = new List<PbWellLogCreateList>();
                    PbWellLogCreateList createList = null;
                    for (int i = 0; i < allCount; i++)
                    {
                        if (i % 3 == 0)
                        {
                            createList = new PbWellLogCreateList();
                            tempList.Add(createList);
                            createList.LogList.Add(AllwellLogs.LogList[i]);
                        }
                        else
                        {
                           createList.LogList.Add(AllwellLogs.LogList[i]);
                        }
                    }
                    for (int i = 0; i < tempList.Count; i++)
                    {
                        var res4 = await wellDataService.batch_create_well_log(tempList[i]);
                        if (res4 != null)
                        {
                        }
                        
                        LogManagerService.Instance.Log($"welllog synchronize ({(i + 1) * 3}/{AllwellLogs.LogList.Count})");
                        ProgressValue = 20+ ((i + 1) * 3 *80)/AllwellLogs.LogList.Count;
                    }

                    LogManagerService.Instance.Log($"welllog synchronize ({AllwellLogs.LogList.Count}/{AllwellLogs.LogList.Count}) synchronize over！");
                }
                else
                {
                    LogManagerService.Instance.Log($"WellLog Count is 0");
                }
                ProgressValue = 100;
                LogManagerService.Instance.Log($"Kindom data synchronize over!.");
                DXMessageBox.Show("Kindom data synchronize over!");

            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Data synchronize failed：" + ex.Message + ex.Message);
                return;
            }
            finally
            {

                    IsEnable = true;
            }
        }
    }
}
