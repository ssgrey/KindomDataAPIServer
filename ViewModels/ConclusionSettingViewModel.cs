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
        //public virtual bool? IsPayzon { get; set; } = true;//测井解释
        //public virtual bool? IsLithology { get; set; } = false;//岩性 
        //public virtual bool? IsSedimentaryFacies { get; set; } = false;//沉积相
        public virtual ExplanationType ExplanationType { get; set; } =  ExplanationType.Payzon;

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
        //protected void OnIsLithologyChanged()
        //{
        //    if (IsLithology == true)
        //    {
        //        NewConclusionName = "Lithology";
        //    }
        //}
        //protected void OnIsSedimentaryFaciesChanged()
        //{
        //    if (IsSedimentaryFacies == true)
        //    {
        //        NewConclusionName = "Facies";
        //    }
        //}

        public virtual string NewConclusionName { get; set; } 

        public virtual ObservableCollection<ConclusionMappingItem> ConclusionMappingItems { get; set; }


        public ConclusionSettingViewModel()
        {
            ConclusionMappingItems = new ObservableCollection<ConclusionMappingItem>();
            this.NewConclusionName = "Payzone";
        }

    }
}
