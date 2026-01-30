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

        public DownLoadDataViewModel(bool isDownloadWellLog)
        {
            IsDownloadWellLog = isDownloadWellLog;
        }

        private List<WellCheckItem> _Wells;
        public List<WellCheckItem> Wells
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


        public DelegateCommand DownloadToKingdomCommand => new DelegateCommand(() =>
        {

        });


    }
}
