using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Database.Repositories.Interface;
using MyLaboratory.Site.Database.Repositories.Class;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Database.Config
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMariaDbContext(this IServiceCollection services)
        {
            string connectionString = "server=mariadb;port=3306;userid=root;password=root;database=ikhyeondb;"; // publish
            //string connectionString = "server=localhost;port=3306;userid=root;password=root;database=ikhyeondb;"; // debugging
            services.AddDbContext<AppDbContext>(options =>
                  options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            return services;
        }
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IWorkerRepository, WorkerRepository>();
            return services;
        }

        public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
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
            return services;
        }
    }
}
