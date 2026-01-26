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
    public enum ExplanationType
    {
        Payzon,
        Lithology,
        SedimentaryFacies
    }

    public class FileNameObj : BindableBase
    {
        private string _FileName;
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {

                SetProperty(ref _FileName, value, nameof(FileName));
            }
        }

        private List<string> _Columns;
        public List<string> Columns
        {
            get
            {
                return _Columns;
            }
            set
            {

                SetProperty(ref _Columns, value, nameof(Columns));
            }
        }

    }

    public class ConclusionSettingViewModel
    {
        public event Action<ConclusionFileNameObj> ConclusionFileNameObjChanged;
        public ConclusionSettingViewModel()
        {
            ConclusionFileNameObjItems = new ObservableCollection<ConclusionFileNameObj>();
        }

        public virtual List<FileNameObj> ColumnNameDict { set; get; } = new List<FileNameObj>();

        public virtual ObservableCollection<ConclusionFileNameObj> ConclusionFileNameObjItems { get; set; }
        /// <summary>
        /// 当前解释结论映射关系
        /// </summary>
        public virtual ConclusionFileNameObj SelectedConclusionFileNameItem { get; set; }

        public virtual bool IsCheckAllConclusionFileNameObj { get; set; } = true;

        protected void OnIsCheckAllConclusionFileNameObjChanged()
        {
            foreach (var item in ConclusionFileNameObjItems)
            {
                item.IsChecked = IsCheckAllConclusionFileNameObj;
            }
        }

        public DelegateCommand AddCommand => new DelegateCommand(() =>
        {
            if (ColumnNameDict.Count == 0)
            {
                DXMessageBox.Show("There is no interval columns in current selected wells!");
                return;
            }
            ConclusionFileNameObj conclusionFileNameObj = new ConclusionFileNameObj();
            conclusionFileNameObj.PropertyChanged += ConclusionFileNameObj_PropertyChanged;
            conclusionFileNameObj.FileName = ColumnNameDict.FirstOrDefault();
            if (conclusionFileNameObj.FileName != null)
            {
                conclusionFileNameObj.ColumnName = conclusionFileNameObj.FileName.Columns.FirstOrDefault();
            }
            ConclusionFileNameObjItems.Add(conclusionFileNameObj);
            SelectedConclusionFileNameItem = conclusionFileNameObj;
        });

        private void ConclusionFileNameObj_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "ColumnName" && sender is ConclusionFileNameObj obj)
            {
                ConclusionFileNameObjChanged?.Invoke(obj);
            }
            else if (e.PropertyName == "FileName" && sender is ConclusionFileNameObj obj2)
            {
                obj2.ColumnName = obj2.FileName.Columns.FirstOrDefault();
            }
        }

        public DelegateCommand DeleteCommand => new DelegateCommand(() =>
        {
            if (SelectedConclusionFileNameItem != null)
            {
                ConclusionFileNameObjItems.Remove(SelectedConclusionFileNameItem);
            }
            
        });
    }

    public class ConclusionFileNameObjConclusionSetting
    {
        public virtual string GUID { get; set; }

        public ConclusionFileNameObjConclusionSetting()
        {
            GUID = Guid.NewGuid().ToString();
            ConclusionMappingItems = new ObservableCollection<ConclusionMappingItem>();
            this.NewConclusionName = "Payzone";
        }
        public virtual ExplanationType ExplanationType { get; set; } = ExplanationType.Payzon;

        protected void OnExplanationTypeChanged()
        {
            if (ExplanationType == ExplanationType.Payzon)
            {
                NewConclusionName = "Payzone";
            }
            else if (ExplanationType == ExplanationType.Lithology)
            {
                NewConclusionName = "Lithology";
            }
            else if (ExplanationType == ExplanationType.SedimentaryFacies)
            {
                NewConclusionName = "Facies";
            }
        }

        public virtual string NewConclusionName { get; set; }

        public virtual ObservableCollection<ConclusionMappingItem> ConclusionMappingItems { get; set; }

    }

    public class ConclusionFileNameObj : BindableBase
    {
        private bool _IsChecked = true;
        public bool IsChecked
        {
            get
            {
                return _IsChecked;
            }
            set
            {

                SetProperty(ref _IsChecked, value, nameof(IsChecked));
            }
        }


        private FileNameObj _FileName;
        public FileNameObj FileName
        {
            get
            {
                return _FileName;
            }
            set
            {

                SetProperty(ref _FileName, value, nameof(FileName));
            }
        }

        private string _ColumnName;
        public string ColumnName
        {
            get
            {
                return _ColumnName;
            }
            set
            {

                SetProperty(ref _ColumnName, value, nameof(ColumnName));
            }
        }

        private ConclusionFileNameObjConclusionSetting _ConclusionSetting;
        public ConclusionFileNameObjConclusionSetting ConclusionSetting
        {
            get
            {
                return _ConclusionSetting;
            }
            set
            {

                SetProperty(ref _ConclusionSetting, value, nameof(ConclusionSetting));
            }
        }
        

    }

}
