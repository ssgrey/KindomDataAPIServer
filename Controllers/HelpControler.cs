using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace KindomDataAPIServer.Controllers
{
    [RoutePrefix("api/help")]
    public class HelpController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetApiInfo()
        {           
            var apiInfo = new
            {
                Application = "WPF WebAPI 服务器 (.NET 4.8)",
                Version = "1.0.0",
                APIs = new List<object>
                {
                    new {
                        Method = "GET",
                        Url = "/api/directory/list",
                        Description = "获取目录结构（两级深度）",
                        Parameters = new { path = "可选，目录路径，默认为用户目录" }
                    },
                    new {
                        Method = "GET",
                        Url = "/api/directory/drives",
                        Description = "获取系统驱动器信息"
                    },
                    new {
                        Method = "GET",
                        Url = "/api/directory/specialfolders",
                        Description = "获取系统特殊文件夹路径"
                    },
                    new {
                        Method = "GET",
                        Url = "/api/help",
                        Description = "获取API帮助信息"
                    }
                },
                Examples = new List<string>
                {
                    "http://localhost:5000/api/directory/list",
                    "http://localhost:5000/api/directory/list?path=C:\\",
                    "http://localhost:5000/api/directory/drives"
                }
            };

            return Ok(apiInfo);
        }
    }

}
