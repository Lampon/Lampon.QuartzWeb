using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lampon.QuartzWeb.Models;
using Lampon.QuartzWeb.QuartzCore;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace Lampon.QuartzWeb
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
            services.AddMvc();
            //var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ScheduleProfile>());
            Mapper.Initialize(x =>
            {
                x.AddProfile<ScheduleProfile>();
            });
            //new StartTask().Start();
            //var productDTO = configuration.CreateMapper().Map<ProductDTO>(productEntity);
            //Mapper.Initialize(cfg => cfg.CreateMap<Schedule, ScheduleEntity>());
        }
        public class ScheduleProfile : Profile
        {
            public ScheduleProfile()
            {
            }
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            //使用默认起始页
            app.UseDefaultFiles();//自动查找wwwroot 中的default.html index.html文件
            app.UseStaticFiles();
            loggerFactory.AddNLog();//添加NLog
            env.ConfigureNLog("NLog.config");//读取Nlog配置文件

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
