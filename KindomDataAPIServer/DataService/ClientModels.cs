using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.DataService
{
    public class AppConfig
    {
        public string ApiBaseUrl { get; set; } = "http://localhost:5000/api";
        public int RequestTimeoutSeconds { get; set; } = 30;
        public string AuthToken { get; set; }
    }


    public class ResponseModel<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public T Data { get; set; }
        public int Code { get; set; }
    }

}
