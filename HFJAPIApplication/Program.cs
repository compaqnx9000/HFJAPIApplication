using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace HFJAPIApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //这里添加Nlog
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                //测试Nlog日志输出
                logger.Debug("init main");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Stopped program because of exception");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
            //CreateWebHostBuilder(args).Build().Run();
        }

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = environment == EnvironmentName.Development;

            if (isDevelopment)
            {
                return WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>().UseNLog();
                //return WebHost.CreateDefaultBuilder(args).UseUrls("http://*:5000").UseStartup<Startup>();
            }
            else
            {
                var configuration = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory)
                                        .AddJsonFile("host.json")
                                        .Build();

                return WebHost.CreateDefaultBuilder(args)
                    .UseConfiguration(configuration)
                        .UseStartup<Startup>().UseNLog();
            }

        }
    }
}
