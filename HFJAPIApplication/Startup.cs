using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HFJAPIApplication.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;

namespace HFJAPIApplication
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(option => option.EnableEndpointRouting = false)
                            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                            .AddNewtonsoftJson(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddControllers();
            //double dis1 = MyCore.Utils.Translate.GetDistance(30.70087139, 103.078125, 30.69951923, 103.078125 );
            //double dis2 = MyCore.Utils.Translate.GetDistance(30.70087139, 103.078125, 30.70087139, 103.07677284);
            //double dis3 = Utils.Translate.GetDistance(33.27967, 110.74078, 33.24, 110.72);
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddTransient<IMongoService, MongoService>();
            services.AddSingleton<IDamageAnalysisService, DamageAnalysisService>();
            services.AddTransient<ITestService, TestService>();

            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService,
               HostedService>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            
        }
    }
}
