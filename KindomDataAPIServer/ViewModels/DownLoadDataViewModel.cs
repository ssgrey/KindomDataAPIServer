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
                UnsubscribeWells(_Wells);
                SetProperty(ref _Wells, value, nameof(Wells));
                SubscribeWells(_Wells);
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

        private void SubscribeWells(ObservableCollection<WellCheckItem> wells)
        {
            if (wells == null)
            {
                return;
            }

            wells.CollectionChanged += Wells_CollectionChanged;
            foreach (var item in wells)
            {
                item.PropertyChanged += WellItem_PropertyChanged;
            }
        }

        private void UnsubscribeWells(ObservableCollection<WellCheckItem> wells)
        {
            if (wells == null)
            {
                return;
            }

            wells.CollectionChanged -= Wells_CollectionChanged;
            foreach (var item in wells)
            {
                item.PropertyChanged -= WellItem_PropertyChanged;
            }
        }

        private void Wells_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (WellCheckItem item in e.OldItems)
                {
                    item.PropertyChanged -= WellItem_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (WellCheckItem item in e.NewItems)
                {
                    item.PropertyChanged += WellItem_PropertyChanged;
                }
            }

            RefreshSelectedWellsCount();
        }

        private void WellItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WellCheckItem.IsChecked))
            {
                RefreshSelectedWellsCount();
            }
        }

        private void RefreshSelectedWellsCount()
        {
            SelectedWellsCount = Wells?.Count(o => o.IsChecked == true) ?? 0;
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
                RefreshSelectedWellsCount();
            }
        }

        private FileWriteMode _FileWriteMode = FileWriteMode.Overwrite;
        public FileWriteMode FileWriteMode
        {
            get
            {
                return _FileWriteMode;
            }
            set
            {

                SetProperty(ref _FileWriteMode, value, nameof(FileWriteMode));
            }
        }
        
        public DelegateCommand DownloadToKingdomCommand => new DelegateCommand(() =>
        {

        });


    }
}
