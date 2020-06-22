using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HFJAPIApplication.services;
using HFJAPIApplication.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HFJAPIApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //允许一个或多个具体来源:
            services.AddCors(options =>
            {
                // Policy 名稱 CorsPolicy 是自訂的，可以自己改
                //跨域规则的名称
                options.AddPolicy("AllowSameDomain", policy =>
                {
                    // 設定允許跨域的來源，有多個的話可以用 `,` 隔開 65356
                    policy.WithOrigins("http://127.0.0.1:4000", "http://localhost:4000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    //.AllowAnyOrigin()//允许所有来源的主机访问
                    .AllowCredentials();
                });
            });
            services.Configure<MongoSetting>(Configuration.GetSection("MongoSetting")).AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<ServiceUrls>(Configuration.GetSection("ServiceUrls")).AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<IMongoService, MongoService>();
            services.AddSingleton<IDamageAnalysisService, DamageAnalysisService>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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



            app.UseHttpsRedirection();//?

            app.UseCors("AllowSameDomain");//必须位于UserMvc之前 

            app.UseMvc();
        }
    }
}
