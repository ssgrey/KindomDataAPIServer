using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public enum FileWriteMode
    {
        Ignore,
        Overwrite,
        Rename
    }
    public class SettingItemSource
    {
        private static Dictionary<string, FileWriteMode> _FileWriteModes;
        /// <summary>
        /// 线样式
        /// </summary>
        public static Dictionary<string, FileWriteMode> FileWriteModes
        {
            get
            {
                if (_FileWriteModes == null)
                {
                    _FileWriteModes = new Dictionary<string, FileWriteMode>();
                    _FileWriteModes.Add("Ignore", FileWriteMode.Ignore);
                    _FileWriteModes.Add("Overwrite", FileWriteMode.Overwrite);
                    _FileWriteModes.Add("Rename", FileWriteMode.Rename);

                }
                return _FileWriteModes;
            }
        }
    }
}
