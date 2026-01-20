using DevExpress.Mvvm;
using KindomDataAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.ViewModels
{
    public enum ExplanationType
    {
        Payzon,
        Lithology,
        SedimentaryFacies
    }

    public class ConclusionSettingViewModel
    {
        public ConclusionSettingViewModel()
        {
            ConclusionMappingItems = new ObservableCollection<ConclusionMappingItem>();
            ConclusionFileNameObjItems = new List<ConclusionFileNameObj>();
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


        public virtual List<ConclusionFileNameObj> ConclusionFileNameObjItems { get; set; }

        public virtual bool IsCheckAllConclusionFileNameObj { get; set; } = true;

        protected void OnIsCheckAllConclusionFileNameObjChanged()
        {
            foreach (var item in ConclusionFileNameObjItems)
            {
                item.IsChecked = IsCheckAllConclusionFileNameObj;
            }
        }
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

        public string FileName { get; set; }

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

        public List<string> Columns { get; set; } = new List<string>();
    }

}
