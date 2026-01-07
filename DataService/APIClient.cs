using KindomDataAPIServer.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.DataService
{
    public sealed class ApiClient : IApiClient, IDisposable
    {
        #region 单例实现
        //private static readonly Lazy<ApiClient> _instance = new Lazy<ApiClient>(() => new ApiClient());
        //public static ApiClient Instance => _instance.Value;

        public ApiClient()
        {
            // 初始化HttpClient
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 设置默认超时时间
            Client.Timeout = TimeSpan.FromSeconds(300);

            // 从配置文件加载设置
           // LoadConfiguration();
        }
        #endregion

        #region 私有字段和属性

        public HttpClient Client { set;  get; }

        //public readonly HttpClient Client;
        private string _baseUrl = "http://localhost:5000/api";
        private string _authToken;
        private string _projectID;
        #endregion

        #region 事件
        public event EventHandler<string> RequestStarted;
        public event EventHandler<string> RequestCompleted;
        public event EventHandler<string> RequestFailed;
        #endregion

        #region 公共方法
        public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> parameters = null)
        {
            try
            {
                OnRequestStarted($"GET {endpoint}");

                var url = BuildUrl(endpoint, parameters);
                var response = await Client.GetAsync(url);
                LogManagerService.Instance.LogDebug(url + "    " + response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonHelper.ConvertFrom<ResponseModel<T>>(content);

                    if (result.Success)
                    {
                        OnRequestCompleted($"GET {endpoint} - 成功");
                        return result.Data;
                    }
                    else
                    {
                        throw new ApiException(result.Message, result.Code);
                    }
                }
                else
                {
                    throw new HttpRequestException($"HTTP请求失败: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OnRequestFailed($"GET {endpoint} - 失败: {ex.Message}");
                throw;
            }
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                OnRequestStarted($"POST {endpoint}");

                var url = BuildUrl(endpoint);
                var json = JsonHelper.ToJson(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await Client.PostAsync(url, content);
                LogManagerService.Instance.LogDebug(url + "    " + response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonHelper.ConvertFrom<TResponse>(responseContent);

                        OnRequestCompleted($"POST {endpoint} - 成功");
                        return result;
                   
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP请求失败: {response.StatusCode} + {responseContent}");
                }
            }
            catch (Exception ex)
            {
                OnRequestFailed($"POST {endpoint} - 失败: {ex.Message}");
                throw ex;
            }
        }

        #endregion

        #region 配置方法
        public void SetHeaders(string authToken, string projID)
        {
            _authToken = authToken;

            if (!string.IsNullOrEmpty(authToken))
            {
                if (Client.DefaultRequestHeaders.Contains("Authorization"))
                {
                    Client.DefaultRequestHeaders.Remove("Authorization");
                }
                Client.DefaultRequestHeaders.Add("Authorization", $"{authToken}");//Bearer 
            }
            else
            {
                Client.DefaultRequestHeaders.Remove("Authorization");
            }

            _projectID = projID;

            if (!string.IsNullOrEmpty(_projectID))
            {
                if (Client.DefaultRequestHeaders.Contains("tetproj"))
                {
                    Client.DefaultRequestHeaders.Remove("tetproj");
                }
                Client.DefaultRequestHeaders.Add("tetproj", $"{projID}");
            }
            else
            {
                Client.DefaultRequestHeaders.Remove("tetproj");
            }
        }


        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public void SetTimeout(TimeSpan timeout)
        {
            Client.Timeout = timeout;
        }
        #endregion

        #region 私有辅助方法
        public string BuildUrl(string endpoint, Dictionary<string, string> parameters = null)
        {
            var url = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            if (parameters != null && parameters.Count > 0)
            {
                var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                url += $"?{queryString}";
            }
            LogManagerService.Instance.LogDebug(url);
            return url;
        }

        //private void LoadConfiguration()
        //{
        //    try
        //    {
        //        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json");
        //        if (File.Exists(configPath))
        //        {
        //            var json = File.ReadAllText(configPath);
        //            var config = JsonHelper.ConvertFrom<AppConfig>(json);

        //            if (!string.IsNullOrEmpty(config.ApiBaseUrl))
        //                _baseUrl = config.ApiBaseUrl;

        //            if (!string.IsNullOrEmpty(config.AuthToken))
        //                SetAuthToken(config.AuthToken);

        //            _httpClient.Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds);
        //        }
        //    }
        //    catch
        //    {
        //        // 配置加载失败，使用默认值
        //    }
        //}

        private void OnRequestStarted(string operation)
        {
            RequestStarted?.Invoke(this, operation);
        }

        private void OnRequestCompleted(string operation)
        {
            RequestCompleted?.Invoke(this, operation);
        }

        private void OnRequestFailed(string operation)
        {
            RequestFailed?.Invoke(this, operation);
        }
        #endregion

        #region IDisposable实现
        private bool _disposed = false;


        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Client?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ApiClient()
        {
            Dispose(false);
        }
        #endregion
    }

    public class ApiException : Exception
    {
        public int ErrorCode { get; }

        public ApiException(string message, int errorCode = 0) : base(message)
        {
            ErrorCode = errorCode;
        }

        public ApiException(string message, Exception innerException, int errorCode = 0)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
