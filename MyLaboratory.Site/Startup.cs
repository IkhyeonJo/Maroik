using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLaboratory.Site.Models;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Database;
using MyLaboratory.Site.Database.Repositories.Interface;
using MyLaboratory.Site.Database.Repositories.Class;
using MyLaboratory.Site.Database.Config;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MyLaboratory.Site
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
            services.AddMariaDbContext()
                .AddRepositories()
                .AddCookieAuthentication();

            services.AddSingleton<IConfiguration>(Configuration);
            services.AddResponseCaching(); // For HTTP Header Setting 
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            #region Login
            app.UseAuthentication();
            app.UseAuthorization();
            #endregion

            #region Default HTTP Header Setting
            //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
            app.UseResponseCaching();

            #region 모든 페이지에 다음과 같은 코드를 적용한다. 로그아웃 후 뒤로가기하면 로그인 했던 페이지가 나오기 때문에 캐쉬 Disable 적용

            //<META http-equiv="Expires" content="-1">
            //<META http-equiv="Pragma" content="no-cache">
            //<META http-equiv="Cache-Control" content="No-Cache">

            //아래 코드는 위의 Html Header에 들어가는 META 데이터 내용과 같다.

            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        //Public = true,
                        //MaxAge = TimeSpan.FromSeconds(10),
                        NoCache = true, // For resolve logout back button problem
                        NoStore = true // For resolve logout back button problem
                    };
                context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                    new string[] { "Accept-Encoding" };

                await next();
            });
            #endregion
            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
                        .RequireAuthorization(); // Apply Authorization to all controller
            });
        }
    }
}
