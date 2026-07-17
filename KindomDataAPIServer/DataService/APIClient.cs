using KindomDataAPIServer.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KindomDataAPIServer.DataService
{
    public sealed class ApiClient : IApiClient, IDisposable
    {
        private const int MaxRetryCount = 3;
        private static int _requestSequence;

        #region 单例实现
        //private static readonly Lazy<ApiClient> _instance = new Lazy<ApiClient>(() => new ApiClient());
        //public static ApiClient Instance => _instance.Value;

        public ApiClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            // 初始化HttpClient
            Client = new HttpClient(handler);
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
        public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> parameters = null, string traceName = null)
        {
            string requestId = CreateRequestId();
            string operationLabel = BuildOperationLabel(requestId, traceName, "GET", endpoint);
            var totalStopwatch = Stopwatch.StartNew();
            try
            {
                OnRequestStarted($"{operationLabel} - 开始");

                var url = BuildUrl(endpoint, parameters);
                LogManagerService.Instance.LogDebug($"{operationLabel} {url} started.");
                for (int attempt = 1; attempt <= MaxRetryCount + 1; attempt++)
                {
                    try
                    {
                        var attemptStopwatch = Stopwatch.StartNew();
                        using (var response = await Client.GetAsync(url))
                        {
                            attemptStopwatch.Stop();
                            LogManagerService.Instance.LogDebug($"{operationLabel} {url}    {response.StatusCode} elapsed:{attemptStopwatch.Elapsed.TotalSeconds:F3}s");

                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                if (typeof(T) == typeof(string))
                                {
                                    totalStopwatch.Stop();
                                    OnRequestCompleted($"{operationLabel} - 成功 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s");
                                    return (T)(object)content;
                                }
                                var result = JsonHelper.ConvertFrom<ResponseModel<T>>(content);

                                if (result.Success)
                                {
                                    totalStopwatch.Stop();
                                    OnRequestCompleted($"{operationLabel} - 成功 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s");
                                    return result.Data;
                                }
                                else
                                {
                                    throw new ApiException(result.Message, result.Code);
                                }
                            }
                            else
                            {
                                if (ShouldRetryStatusCode(response.StatusCode) && attempt <= MaxRetryCount)
                                {
                                    await DelayForRetryAsync(operationLabel, attempt, response.StatusCode);
                                    continue;
                                }

                                throw new NonRetryableHttpRequestException($"HTTP请求失败: {response.StatusCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ShouldRetryException(ex) || attempt > MaxRetryCount)
                        {
                            throw;
                        }

                        await DelayForRetryAsync(operationLabel, attempt, ex);
                    }
                }

                throw new HttpRequestException($"GET {endpoint} failed after retries.");
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                OnRequestFailed($"{operationLabel} - 失败 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s: {ExceptionLogHelper.Format(ex)}");
                throw;
            }
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, string traceName = null)
        {
            string requestId = CreateRequestId();
            string operationLabel = BuildOperationLabel(requestId, traceName, "POST", endpoint);
            var totalStopwatch = Stopwatch.StartNew();
            try
            {
                OnRequestStarted($"{operationLabel} - 开始");

                var url = BuildUrl(endpoint);
                var json = JsonHelper.ToJson(data);
               //json = File.ReadAllText("tempjsonArgs2.txt");
                LogManagerService.Instance.LogDebug($"{operationLabel} {url} started.");
                for (int attempt = 1; attempt <= MaxRetryCount + 1; attempt++)
                {
                    try
                    {
                        var attemptStopwatch = Stopwatch.StartNew();
                        using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                        using (var response = await Client.PostAsync(url, content))
                        {
                            attemptStopwatch.Stop();
                            LogManagerService.Instance.LogDebug($"{operationLabel} {url}    {response.StatusCode} elapsed:{attemptStopwatch.Elapsed.TotalSeconds:F3}s");

                            if (response.IsSuccessStatusCode)
                            {
                                var responseContent = await response.Content.ReadAsStringAsync();
                                var result = JsonHelper.ConvertFrom<TResponse>(responseContent);

                                totalStopwatch.Stop();
                                OnRequestCompleted($"{operationLabel} - 成功 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s");
                                return result;

                            }
                            else
                            {
                                var responseContent = await response.Content.ReadAsStringAsync();
                                if (ShouldRetryStatusCode(response.StatusCode) && attempt <= MaxRetryCount)
                                {
                                    await DelayForRetryAsync(operationLabel, attempt, response.StatusCode);
                                    continue;
                                }

                                throw new NonRetryableHttpRequestException($"HTTP请求失败: {response.StatusCode} + {responseContent} + Request json+ {json}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ShouldRetryException(ex) || attempt > MaxRetryCount)
                        {
                            throw;
                        }

                        await DelayForRetryAsync(operationLabel, attempt, ex);
                    }
                }

                throw new HttpRequestException($"POST {endpoint} failed after retries.");
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                OnRequestFailed($"{operationLabel} - 失败 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s: {ExceptionLogHelper.Format(ex)}");
                throw;
            }
        }


        public async Task<TResponse> PostAsync<TResponse>(string endpoint, string traceName = null)
        {
            string requestId = CreateRequestId();
            string operationLabel = BuildOperationLabel(requestId, traceName, "POST", endpoint);
            var totalStopwatch = Stopwatch.StartNew();
            try
            {
                OnRequestStarted($"{operationLabel} - 开始");

                var url = BuildUrl(endpoint);
                LogManagerService.Instance.LogDebug($"{operationLabel} {url} started.");
                for (int attempt = 1; attempt <= MaxRetryCount + 1; attempt++)
                {
                    try
                    {
                        var attemptStopwatch = Stopwatch.StartNew();
                        using (var content = new StringContent(string.Empty, Encoding.UTF8, "application/json"))
                        using (var response = await Client.PostAsync(url, content))
                        {
                            attemptStopwatch.Stop();
                            LogManagerService.Instance.LogDebug($"{operationLabel} {url}    {response.StatusCode} elapsed:{attemptStopwatch.Elapsed.TotalSeconds:F3}s");
                            if (response.IsSuccessStatusCode)
                            {
                                var responseContent = await response.Content.ReadAsStringAsync();
                                var result = JsonHelper.ConvertFrom<TResponse>(responseContent);
                                totalStopwatch.Stop();
                                OnRequestCompleted($"{operationLabel} - 成功 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s");
                                return result;
                            }
                            else
                            {
                                var responseContent = await response.Content.ReadAsStringAsync();
                                if (ShouldRetryStatusCode(response.StatusCode) && attempt <= MaxRetryCount)
                                {
                                    await DelayForRetryAsync(operationLabel, attempt, response.StatusCode);
                                    continue;
                                }

                                throw new NonRetryableHttpRequestException($"HTTP请求失败: {response.StatusCode} + {responseContent}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ShouldRetryException(ex) || attempt > MaxRetryCount)
                        {
                            throw;
                        }

                        await DelayForRetryAsync(operationLabel, attempt, ex);
                    }
                }

                throw new HttpRequestException($"POST {endpoint} failed after retries.");
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                OnRequestFailed($"{operationLabel} - 失败 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s: {ExceptionLogHelper.Format(ex)}");
                throw;
            }
        }

        public async Task<TResponse> PostMultipartAsync<TResponse>(string endpoint, Func<MultipartFormDataContent> contentFactory, string traceName = null)
        {
            string requestId = CreateRequestId();
            string operationLabel = BuildOperationLabel(requestId, traceName, "POST", endpoint);
            var totalStopwatch = Stopwatch.StartNew();
            try
            {
                OnRequestStarted($"{operationLabel} - 开始");

                var url = BuildUrl(endpoint);
                LogManagerService.Instance.LogDebug($"{operationLabel} {url} started.");
                for (int attempt = 1; attempt <= MaxRetryCount + 1; attempt++)
                {
                    try
                    {
                        var attemptStopwatch = Stopwatch.StartNew();
                        using (var content = contentFactory())
                        using (var response = await Client.PostAsync(url, content))
                        {
                            attemptStopwatch.Stop();
                            LogManagerService.Instance.LogDebug($"{operationLabel} {url}    {response.StatusCode} elapsed:{attemptStopwatch.Elapsed.TotalSeconds:F3}s");

                            var responseContent = await response.Content.ReadAsStringAsync();
                            if (response.IsSuccessStatusCode)
                            {
                                var result = JsonHelper.ConvertFrom<TResponse>(responseContent);
                                totalStopwatch.Stop();
                                OnRequestCompleted($"{operationLabel} - 成功 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s");
                                return result;
                            }

                            if (ShouldRetryStatusCode(response.StatusCode) && attempt <= MaxRetryCount)
                            {
                                await DelayForRetryAsync(operationLabel, attempt, response.StatusCode);
                                continue;
                            }

                            throw new NonRetryableHttpRequestException($"HTTP请求失败: {response.StatusCode} + {responseContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ShouldRetryException(ex) || attempt > MaxRetryCount)
                        {
                            throw;
                        }

                        await DelayForRetryAsync(operationLabel, attempt, ex);
                    }
                }

                throw new HttpRequestException($"POST {endpoint} failed after retries.");
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                OnRequestFailed($"{operationLabel} - 失败 elapsed:{totalStopwatch.Elapsed.TotalSeconds:F3}s: {ExceptionLogHelper.Format(ex)}");
                throw;
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
        public void SetHeaders_case_id(string case_id, string tetlocale)
        {
            if (!string.IsNullOrEmpty(case_id))
            {
                if (Client.DefaultRequestHeaders.Contains("Case_id"))
                {
                    Client.DefaultRequestHeaders.Remove("Case_id");
                }
                Client.DefaultRequestHeaders.Add("Case_id", $"{case_id}");//Bearer 
            }
            else
            {
                Client.DefaultRequestHeaders.Remove("Case_id");
            }

            if (!string.IsNullOrEmpty(tetlocale))
            {
                if (Client.DefaultRequestHeaders.Contains("Tet-locale"))
                {
                    Client.DefaultRequestHeaders.Remove("Tet-locale");
                }
                Client.DefaultRequestHeaders.Add("Tet-locale", $"{tetlocale}");//Bearer 
            }
            else
            {
                Client.DefaultRequestHeaders.Remove("Tet-locale");
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
            return url;
        }

        private string CreateRequestId()
        {
            return "REQ-" + Interlocked.Increment(ref _requestSequence);
        }

        private string BuildOperationLabel(string requestId, string traceName, string method, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(traceName))
            {
                return $"[{requestId}] {method} {endpoint}";
            }

            return $"[{requestId}] {traceName} {method} {endpoint}";
        }

        private bool ShouldRetryStatusCode(HttpStatusCode statusCode)
        {
            var statusCodeValue = (int)statusCode;
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCodeValue == 429 ||
                   statusCodeValue >= 500;
        }

        private bool ShouldRetryException(Exception ex)
        {
            var currentException = ex;
            while (currentException != null)
            {
                if (currentException is NonRetryableHttpRequestException)
                {
                    return false;
                }

                currentException = currentException.InnerException;
            }

            return ex != null;
        }

        private async Task DelayForRetryAsync(string operationLabel, int retryAttempt, HttpStatusCode statusCode)
        {
            var delay = GetRetryDelay(retryAttempt);
            LogManagerService.Instance.Log($"{operationLabel} returned {statusCode}. Retry {retryAttempt}/{MaxRetryCount} after {delay.TotalSeconds} seconds.");
            await Task.Delay(delay);
        }

        private async Task DelayForRetryAsync(string operationLabel, int retryAttempt, Exception ex)
        {
            var delay = GetRetryDelay(retryAttempt);
            LogManagerService.Instance.Log($"{operationLabel} failed. Retry {retryAttempt}/{MaxRetryCount} after {delay.TotalSeconds} seconds. {ExceptionLogHelper.Format(ex)}");
            await Task.Delay(delay);
        }

        private TimeSpan GetRetryDelay(int retryAttempt)
        {
            switch (retryAttempt)
            {
                case 1:
                    return TimeSpan.FromSeconds(2);
                case 2:
                    return TimeSpan.FromSeconds(5);
                default:
                    return TimeSpan.FromSeconds(10);
            }
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

    internal class NonRetryableHttpRequestException : HttpRequestException
    {
        public NonRetryableHttpRequestException(string message) : base(message)
        {
        }
    }
}
