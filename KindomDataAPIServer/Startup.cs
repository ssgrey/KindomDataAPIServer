using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using System.Web.Http;
using System.Web.Http.Cors;

[assembly: OwinStartup(typeof(KindomDataAPIServer.Startup))]
namespace KindomDataAPIServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 配置Web API
            var config = new HttpConfiguration();

            // 启用CORS
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // 配置路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // 配置JSON格式化
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.Indent = true;
           
            // 启用帮助页面
            //config.SetDocumentationProvider(
            //    new System.Web.Http.Description.XmlDocumentationProvider(
            //        System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin", "WpfWebApiServer.XML")));

            // 使用Web API
            app.UseWebApi(config);
        }
    }
}
