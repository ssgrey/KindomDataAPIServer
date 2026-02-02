using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;

namespace KindomDataAPIServer.ViewModels
{

    public class DownLoadDataViewModel : BindableBase
    {
        private bool _IsDownloadWellLog = true;
        public bool IsDownloadWellLog
        {
            get
            {
                return _IsDownloadWellLog;
            }
            set
            {

                SetProperty(ref _IsDownloadWellLog, value, nameof(IsDownloadWellLog));

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
        
        public DownLoadDataViewModel(bool isDownloadWellLog)
        {
            IsDownloadWellLog = isDownloadWellLog;
        }

        private ObservableCollection<WellCheckItem> _Wells;
        public ObservableCollection<WellCheckItem> Wells
        {
            get
            {
                return _Wells;
            }
            set
            {
                SetProperty(ref _Wells, value, nameof(Wells));
            }
        }

        private bool _IsCheckAllWell = true;
        public bool IsCheckAllWell
        {
            get
            {
                return _IsCheckAllWell;
            }
            set
            {

                SetProperty(ref _IsCheckAllWell, value, nameof(IsCheckAllWell));
                if (Wells != null)
                {
                    foreach (var item in Wells)
                    {
                        item.IsChecked = _IsCheckAllWell;
                    }
                }
            }
        }


        public DelegateCommand DownloadToKingdomCommand => new DelegateCommand(() =>
        {

        });


    }
}
