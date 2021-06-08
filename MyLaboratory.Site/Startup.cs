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
                .AddRepositories();

            services.AddSingleton<IConfiguration>(Configuration);
            services.AddControllersWithViews();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.LoginPath = "/Account/Login"; // 로그인이 필요한 페이지의 경우 자동으로 리디렉션.
                    options.LogoutPath = "/Account/Logout"; // 만료 시간이 되면 쿠키가 사라져서 자동으로 로그아웃 페이지로 이동한다.
                    options.AccessDeniedPath = "/Account/Denied"; // 엑세스가 거부되었을 때의 경로이다.
                    options.Events = new CookieAuthenticationEvents()
                    {
                        OnSigningIn = async context => // 로그인 버튼 눌렀을 때 (SignInAsync 생성 중)
                        {
                            var principal = context.Principal;
                            if (principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                            {
                                if (principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value == "bob") // 만일 NameIdentitfier가 bob이면
                                {
                                    var claimsIdentity = principal.Identity as ClaimsIdentity;
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin")); // 관리자 권한으로 승격한다.
                                }
                            }
                            await Task.CompletedTask;
                        },
                        OnSignedIn = async context => // 로그인 버튼 눌렀을 때 (SignInAsync 생성 끝) 
                        {
                            await Task.CompletedTask;
                        },
                        OnValidatePrincipal = async context =>
                        {
                            await Task.CompletedTask;
                        }
                    };
                });
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
                        .RequireAuthorization(); // Apply Authorization to all controller
            });
        }
    }
}
