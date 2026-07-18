using KindomDataAPIServer.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace KindomDataAPIServer.DataService
{
    public static class UserCredentialApi
    {
        private const string ApiTokenConfigKey = "api-token";
        private const string Endpoint = "http://10.10.4.160:30104/tet/uaa/api/user/get_user_info_by_id_list";

        private static readonly HttpClient Client = CreateHttpClient();

        public static void GetUserInfoByIdList()
        {
            GetUserInfoByIdList(new long[] { 1 });
        }

        public static void GetUserInfoByIdList(IEnumerable<long> userIdList)
        {
            var client = ServiceLocator.GetService<IApiClient>();
            string Endpoint = client.BuildUrl("uaa/api/user/get_user_info_by_id_list");

            string apiToken = ConfigurationManager.AppSettings[ApiTokenConfigKey];
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                throw new ConfigurationErrorsException($"Missing appSettings key '{ApiTokenConfigKey}'.");
            }

            var requestBody = new UserInfoByIdListRequest
            {
                UserIdList = userIdList == null ? new List<long> { 0 } : userIdList.ToList()
            };

            string json = JsonHelper.ToJson(requestBody);

            using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                request.Headers.Add("api-token", apiToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = Client.SendAsync(request).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        throw new HttpRequestException($"HTTP request failed: {response.StatusCode} + {responseContent}");
                    }

                    IEnumerable<string> authorizationValues;
                    if (!response.Headers.TryGetValues("Authorization", out authorizationValues))
                    {
                        throw new InvalidOperationException("Response header 'Authorization' was not found.");
                    }

                    string authorization = authorizationValues.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(authorization))
                    {
                        throw new InvalidOperationException("Response header 'Authorization' was empty.");
                    }

                    if (client.Client.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        client.Client.DefaultRequestHeaders.Remove("Authorization");
                    }
                    client.Client.DefaultRequestHeaders.Add("Authorization", authorization);
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        private class UserInfoByIdListRequest
        {
            [JsonProperty("userIdList")]
            public List<long> UserIdList { get; set; }
        }
    }
}
