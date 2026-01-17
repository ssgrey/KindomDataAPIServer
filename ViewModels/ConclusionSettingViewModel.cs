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
    public class ConclusionSettingViewModel
    {
        public virtual bool IsPayzon { get; set; } = true;//测井解释
        public virtual bool IsLithology { get; set; }//岩性 
        public virtual bool IsSedimentaryFacies { get; set; }//沉积相

        protected void OnIsPayzonChanged()
        {
            if (IsPayzon)
            {
                NewConclusionName = "Payzone";
            }
        }
        protected void OnIsLithologyChanged()
        {
            if (IsLithology)
            {
                NewConclusionName = "Lithology";
            }
        }
        protected void OnIsSedimentaryFaciesChanged()
        {
            if (IsSedimentaryFacies)
            {
                NewConclusionName = "Facies";
            }
        }

        public virtual string NewConclusionName { get; set; } 

        public virtual ObservableCollection<ConclusionMappingItem> PolygonList { get; set; }

        public ConclusionSettingViewModel()
        {
            PolygonList = new ObservableCollection<ConclusionMappingItem>();
            this.NewConclusionName = "Payzone";
        }

    }
}
