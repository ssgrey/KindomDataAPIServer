using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace KindomDataAPIServer.DataService
{
    public interface IApiClient
    {
        HttpClient Client { set; get; }

        Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> parameters = null);
        Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task<TResponse> PostAsync<TResponse>(string endpoint);

        // 事件
        event EventHandler<string> RequestStarted;
        event EventHandler<string> RequestCompleted;
        event EventHandler<string> RequestFailed;

        // 配置相关
        void SetHeaders(string token, string projID);
        void SetHeaders_case_id(string case_id,string tetlocale);
        
        void SetBaseUrl(string baseUrl);
        void SetTimeout(TimeSpan timeout);

        string BuildUrl(string endpoint, Dictionary<string, string> parameters = null);

    }
}
