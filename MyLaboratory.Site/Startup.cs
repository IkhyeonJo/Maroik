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
using MyLaboratory.Site.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyLaboratory.Site.Data;
using System.Net;
using System.IO;
using MyLaboratory.Site.Filters;
using MyLaboratory.Site.Contracts;

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
            #region AddMariaDbContext
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                  //options.UseLazyLoadingProxies().UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), providerOptions => providerOptions.EnableRetryOnFailure())); // Excel
                  options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), providerOptions => providerOptions.EnableRetryOnFailure()));
            #endregion

            #region AddRepositories
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IWorkerRepository, WorkerRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
            #endregion

            #region AddSession
            services.AddDistributedMemoryCache(); // Session Storage

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // 서버 세션 만료 시간은 클라이언트 세션 만료 시간보다 2배 커야한다. 
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            #endregion

            services.AddSingleton<IConfiguration>(Configuration);
            services.AddResponseCaching(); // For HTTP Header Setting 
            //services.AddControllersWithViews();

            services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new GlobalActionFilter()); // An instance
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //1.Exception / error handling

            //앱이 개발환경에서 실행되는 경우
            //개발자 예외 페이지 미들웨어가(UseDeveloperExceptionPage) 가 앱 런타임 오류를 보고합니다.
            //데이터베이스 오류페이지 미들웨어가 데이터베이스 런타임 오류를 보고합니다.
            //앱이 프로덕션 환경에서 실행되는 경우
            //예외 처리기 미들웨어(UseExceptionHandler)는 다음 미들웨어에서 발생한 예외를 포착합니다.
            //HSTS(HTTP Strict Transport Security Protocol) 미들웨어(UseHsts)는 Strict-Transport - Security헤더를 추가합니다.

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Exception/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            app.UseHttpsRedirection(); // 2. HTTPS 리디렉션 미들웨어 (UseHttpsRedirection)는 HTTP 요청을 HTTPS로 리디렉션합니다.
            //app.UseStaticFiles(); // 3. 정적 파일 미들웨어 (UseStaticFiles)는 정적 파일을 리턴하고 추가 요청 처리를 단락시킵니다.
            // app.UseCookiePolicy(); 4. 쿠키 정책 미들웨어 (UseCookiePolicy)는 앱을 EU 일반 데이터 보호 규정 (GDPR) 규정에 따릅니다.


            app.UseRouting(); // 5. 요청을 라우팅하기위한 미들웨어 라우팅 (UseRouting)

            // app.UseRequestLocalization();
            // app.UseCors();


            #region Login
            app.UseAuthentication(); // 6.인증 미들웨어(UseAuthentication)는 보안 자원에 대한 액세스가 허용되기 전에 사용자 인증을 시도합니다.
            app.UseAuthorization(); // 7. 권한 미들웨어 (UseAuthorization)는 사용자가 보안 자원에 액세스 할 수있는 권한을 부여합니다.
            app.UseSession(); // 8. 세션 미들웨어 (UseSession)는 세션 상태를 설정하고 유지합니다. 앱이 세션 상태를 사용하는 경우 쿠키 정책 미들웨어 이후 및 MVC 미들웨어 전에 세션 미들웨어를 호출하십시오.
            #endregion

            #region 권한 없이 wwwroot에 접근하려고 하면 초기 화면으로 돌려보냄
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    bool isAccountSessionExist = context.Context.Session.TryGetValue("AccountSession", out _); // 로그인이 되어 계정 세션이 생겼는지 확인

                    if (isAccountSessionExist == false && !context.Context.Request.Path.StartsWithSegments("/adminlte")) // 로그인이 안 된 상태인가? 그렇다면 ~/adminlte 자원에만 접근 허용
                    {
                        context.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized; // 권한 없음 
                        context.Context.Response.ContentLength = 0; // 내용 길이 없음
                        context.Context.Response.Body = Stream.Null; // 내용 없음
                        context.Context.Response.Redirect("/"); // 로그인 화면으로
                    }
                    //else if (true) // 로그인이 되었고 권한이 Admin인가? 그렇다면 ~/AdminResources 자원에만 접근 허용
                    //{

                    //}
                    //else if (true) // 로그인이 되었고 권한이 User인가? 그렇다면 ~/UserResources 자원에만 접근 허용
                    //{

                    //}
                }
            });
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

            //app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); }); //9.엔드 포인트 라우팅 미들웨어(MapRazorPages 를 포함하는 UseEndPoints)를 사용하여 요청 파이프라인에 Razor Pages 엔드포인트를 추가합니다.

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Account}/{action=Login}/{id?}");
            });
        }
    }
}
