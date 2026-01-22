using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models.Settings
{
    public class UnitMappingItem : BindableBase
    {
        private string _PropName;
        public string PropName
        {
            get { return _PropName; }
            set
            {
                SetValue(ref _PropName, value, nameof(PropName));
            }
        }

        private string _KindomUnitName;
        public string KindomUnitName
        {
            get { return _KindomUnitName; }
            set
            {
                SetValue(ref _KindomUnitName, value, nameof(KindomUnitName));
            }
        }

        private UnitInfo _NewUnit;
        public UnitInfo NewUnit
        {
            get { return _NewUnit; }
            set
            {
                SetValue(ref _NewUnit, value, nameof(NewUnit));
            }
        }


        private List<UnitInfo> _UnitInfos;
        public List<UnitInfo> UnitInfos
        {

            get
            {
                return _UnitInfos;
            }
            set
            {
                SetProperty(ref _UnitInfos, value, nameof(UnitInfos));
            }
        }
    }
}
