using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models
{
    

    public class ApiConfig
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("tetproj")]
        public string Tetproj { get; set; }

        [JsonProperty("tetprojname")]
        public string TetprojName { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("actiontype")]
        public int ActionType { get; set; } = 0;//0:上传数据 1:下载井曲线数据 2:下载解释结论数据
    }
}
