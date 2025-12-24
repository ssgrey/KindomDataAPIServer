using DevExpress.Mvvm;
using Smt.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models
{

    public class ProjectResponse: KindomResponseBase
    {
        public string ProjectPath { get; set; }
        public string MapUnit { get; set; }
        public string VerticalUnit { get; set; }
        public List<WellExport> Wells { get; set; } = new List<WellExport>();
        public List<CheckNameExport> LogNames { get; set; } = new List<CheckNameExport>();
        public List<CheckNameExport> FormationNames { get; set; } = new List<CheckNameExport>();

        
        private bool _AllWellsChecked = true;
        public bool AllWellsChecked
        {
            get
            {
                return _AllWellsChecked;
            }
            set
            {
                SetProperty(ref _AllWellsChecked, value, nameof(AllWellsChecked));
                if(Wells != null)
                {
                    foreach(var well in Wells)
                    {
                        well.IsChecked = value;
                    }
                }
            }
        }

        private bool _AllWellLogsChecked = true;
        public bool AllWellLogsChecked
        {
            get
            {
                return _AllWellLogsChecked;
            }
            set
            {
                SetProperty(ref _AllWellLogsChecked, value, nameof(AllWellLogsChecked));
                if (LogNames != null)
                {
                    foreach (var item in LogNames)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }

        private bool _AllWellFormationChecked = true;
        public bool AllWellFormationChecked
        {
            get
            {
                return _AllWellFormationChecked;
            }
            set
            {
                SetProperty(ref _AllWellFormationChecked, value, nameof(AllWellFormationChecked));
                if (FormationNames != null)
                {
                    foreach (var item in FormationNames)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }
    }

    public class WellExport : BindableBase
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

        public int BoreholeId { get; set; }
        public string BoreholeName { get; set; }
        public int WellId { get; set; }
        public string WellName { get; set; }
        public string WellNumber { get; set; }
        public string Uwi { get; set; }
        public double SurfaceX { get; set; }
        public double SurfaceY { get; set; }
        public double BottomX { get; set; }
        public double BottomY { get; set; }
        public string Country { get; set; }
        /// <summary>
        /// 区域
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// 区县  county
        /// </summary>
        public string Districts { get; set; }
        public string MapUnit { get; set; }
        public string VerticalUnit { get; set; }
        public List<DigitalLogExport> DigitalLogs { get; set; } = new List<DigitalLogExport>();
        public double Longitude { get;  set; }
        public double Latitude { get;  set; }
    }

    public class DigitalLogExport
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int NameId { get; set; }
        public double? SampleRate { get; set; }
        public double? StartDepth { get; set; }
        public int? Count { get; set; }

        public List<float> Values { get; set; } 
    }


    public class CheckNameExport : BindableBase
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
    }
}
