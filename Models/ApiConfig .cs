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

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }
    }
}
