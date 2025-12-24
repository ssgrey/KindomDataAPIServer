using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Utils.Drawing;
using KindomDataAPIServer.Common;
using KindomDataAPIServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Tet.Transport.Protobuf.Metaobjs;
using Tet.Transport.Protobuf.Well;

namespace KindomDataAPIServer.DataService
{
    public class WellDataService : IDataWellService
    {
        private readonly IApiClient _apiClient;

        public WellDataService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<WellExport>> GetWells()
        {
            try
            {
                return await _apiClient.GetAsync<List<WellExport>>("wells");
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"获取所有井数据失败: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetUserInfos()
        {
            try
            {
                return await _apiClient.GetAsync<string>("uaa/api/user/get_user_info");
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"获取所有井数据失败: {ex.Message}");
                throw;
            }
        }

        public async Task<WellOperationResult> batch_create_well_header(WellDataRequest wellDataRequest)
         {
            try
            {
                return await _apiClient.PostAsync<WellDataRequest, WellOperationResult>("dp/api/well_manager/batch_create_well_header", wellDataRequest);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"批量创建井头信息失败: {ex.Message}");
                throw;
            }

        }

        public async Task<string> create_well_log_set(string datasetName)
        {
            try
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("datasetName", datasetName);
                param.Add("datasetType", "continuous");
                return await _apiClient.PostAsync<Dictionary<string, object>, string>("dp/api/well_log/create_well_log_set", param);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"batch_create_well_header failed: {ex.Message}");
                throw;
            }

        }

        public async Task<List<LogSetInfo>> get_dataset_list(string datasetType = "continuous")
        {
            try
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("datasetType", datasetType);
                return await _apiClient.PostAsync<Dictionary<string, object>, List<LogSetInfo>>("dp/api/well_log/get_dataset_list", param);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"get_dataset_list failed: {ex.Message}");
                throw;
            }

        }
        

        public async Task<WellOperationResult>  batch_create_well_formation(PbWellFormationList pbWellFormationList)
        {
            try
            {
                string flag = "OverWrite";
                var url = _apiClient.BuildUrl("dp/api/welldata/batch_create_well_formation");
                var formData = new MultipartFormDataContent();
                using (var stream = ProtoHelper.ToMemoryStream(pbWellFormationList))
                {
                    var streamContent = new StreamContent(stream);
                    var stringContent = new StringContent(flag);
                    //formData.Add(streamContent, "proto_creation_data", "data.pbf");
                    formData.Add(streamContent, "File", "data.pbf");
                    formData.Add(stringContent, "overwrite_flag");
                    var httpResult = await _apiClient.Client.PostAsync(url, formData);
                    var responseContent = await httpResult.Content.ReadAsStringAsync();

                    if (httpResult.IsSuccessStatusCode)
                    {
                        var result = JsonHelper.ConvertFrom<WellOperationResult>(responseContent);
                            return result;                   
                    }
                    else
                    {
                        throw new HttpRequestException($"HTTP请求失败: {responseContent}");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"批量创建井段信息失败: {ex.Message}");
                throw ex;
            }
        }

        public async Task<WellOperationResult> batch_create_well_log(PbWellLogCreateList pbWellLogCreateList)
        {
            try
            {
                string flag = "OverWrite";
                var url = _apiClient.BuildUrl("dp/api/well_log/batch_create_well_log");
                var formData = new MultipartFormDataContent();
                using (var stream = ProtoHelper.ToMemoryStream(pbWellLogCreateList))
                {
                    var streamContent = new StreamContent(stream);
                    var stringContent = new StringContent(flag);
                    formData.Add(streamContent, "proto_creation_data", "data.pbf");
                    formData.Add(stringContent, "overwrite_flag");
                    var httpResult = await _apiClient.Client.PostAsync(url, formData);
                    var responseContent = await httpResult.Content.ReadAsStringAsync();

                    if (httpResult.IsSuccessStatusCode)
                    {
                        var result = JsonHelper.ConvertFrom<WellOperationResult>(responseContent);
                        return result;
                    }
                    else
                    {
                        throw new HttpRequestException($"HTTP请求失败: {responseContent}");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"批量创建井段信息失败: {ex.Message}");
                throw ex;
            }
        }
        /// <summary>
        /// request ["WellInformation"]
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PbViewMetaObjectList> get_all_meta_objects_by_objecttype_in_protobuf(string[] request)
        {
            try
            {
                var url = _apiClient.BuildUrl("dp/api/dpobjects/get_all_meta_objects_by_objecttype_in_protobuf");
                var json = JsonHelper.ToJson(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResult = await _apiClient.Client.PostAsync(url, content);
                if (httpResult.IsSuccessStatusCode)
                {
                    Stream stream = await httpResult.Content.ReadAsStreamAsync();
                    PbViewMetaObjectList obj = ProtoHelper.FromStream<PbViewMetaObjectList>(stream);
                    return obj;
                }
                else
                {
                    throw new HttpRequestException($"HTTP请求失败: {httpResult.StatusCode}");
                }

                return null;
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"get_all_meta_objects_by_objecttype_in_protobuf filed: {ex.Message}");
                throw;
            }
        }


    }
}
