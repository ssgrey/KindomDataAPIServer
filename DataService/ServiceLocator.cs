using Autofac;
using Microsoft.Owin.Hosting.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KindomDataAPIServer.DataService
{
    public class ServiceLocator
    {
        private static IContainer _container;

        public static void ConfigureServices()
        {
            var builder = new ContainerBuilder();

            // 1. 注册你的单例 ApiClient
            // 注意：这里注册的是实例，如果需要让 Autofac 管理生命周期，可以改为注册类型。
            ApiClient apiClient = new ApiClient();


            builder.RegisterInstance(apiClient).As<IApiClient>().SingleInstance();
            //builder.RegisterType<ApiClient>().As<IApiClient>().SingleInstance();
            // 2. 注册其他服务
            builder.RegisterType<WellDataService>().As<IDataWellService>().SingleInstance(); // 单例
            // builder.RegisterType<OtherService>().As<IOtherService>().InstancePerDependency(); // 瞬态
            _container = builder.Build();
        }

        public static T GetService<T>() where T : class
        {
            return _container.Resolve<T>();
        }

    }
}
