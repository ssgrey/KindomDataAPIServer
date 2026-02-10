using KindomDataAPIServer.Common;
using KindomDataAPIServer.KindomAPI;
using KindomDataAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace KindomDataAPIServer.Controllers
{
    [RoutePrefix("api/well")]
    public class WellController : ApiController
    {
        [HttpGet]
        public IHttpActionResult GetAllAuthorsByProject([FromUri] string projectPath)
        {
            var stopwatch = Stopwatch.StartNew();
            List<string> Authors = new List<string>();
            try
            {

                if (string.IsNullOrEmpty(projectPath))
                {
                    return BadRequest("工程路径不能为空");
                }

                Authors = KingdomAPI.Instance.GetProjectAuthors();
                return Ok(Authors);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"GetAllAuthorsByProject异常：{ex.Message}\n{ex.StackTrace}");
                return InternalServerError(ex);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        [HttpGet]
        public IHttpActionResult GetAllWellByProject([FromUri] string projectPath)
        {
            var stopwatch = Stopwatch.StartNew();
            WellRequest request = new WellRequest();
            request.ProjectPath = projectPath;
            ProjectResponse response = new ProjectResponse();
            try
            {
                // 验证请求参数
                if (request == null)
                {
                    response.ErrorMessage = "请求参数不能为空";
                    return BadRequest(response.ErrorMessage);
                }

                if (string.IsNullOrEmpty(request.ProjectPath))
                {
                    response.ErrorMessage = "工程路径不能为空";
                    return BadRequest(response.ErrorMessage);
                }

                response = KingdomAPI.ExportProject(request.ProjectPath, request.LoginName);
                response.Success = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = $"查询失败: {ex.Message}";
                LogManagerService.Instance.Log($"GetAllWellByProject异常：{ex.Message}\n{ex.StackTrace}");
                return InternalServerError(ex);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        [HttpPost]
        public IHttpActionResult GetAllWellByProjectAndAuthor([FromBody] WellRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            ProjectResponse response = new ProjectResponse();
            try
            {
                // 验证请求参数
                if (request == null)
                {
                    response.ErrorMessage = "请求参数不能为空";
                    return BadRequest(response.ErrorMessage);
                }

                if (string.IsNullOrEmpty(request.ProjectPath))
                {
                    response.ErrorMessage = "工程路径不能为空";
                    return BadRequest(response.ErrorMessage);
                }

                response = KingdomAPI.ExportProject(request.ProjectPath, request.LoginName);
                response.Success = true;
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                response.ErrorMessage = $"没有访问权限: {ex.Message}";
                return Unauthorized();
            }
            catch (Exception ex)
            {
                response.ErrorMessage = $"查询失败: {ex.Message}";
                return InternalServerError(ex);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

    }
}
