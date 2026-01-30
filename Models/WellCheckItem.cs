using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models
{
    public class WellCheckItem : BindableBase
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

        public string Name { get; set; }
        public string ID { get; set; }
    }
}
