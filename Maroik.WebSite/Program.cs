using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Services;
using Maroik.WebSite.Common;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Filters;
using Maroik.WebSite.Services;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#region ServerSettings
ServerSetting.DomainName = builder.Configuration.GetSection("ServerSetting")["DomainName"];
ServerSetting.MaxLoginAttempt = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting")["MaxLoginAttempt"]);
ServerSetting.SessionExpireMinutes = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting")["SessionExpireMinutes"]);
ServerSetting.SmtpUserName = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpUserName"];
ServerSetting.SmtpPassword = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpPassword"];
ServerSetting.SmtpHost = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpHost"];
ServerSetting.SmtpPort = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpPort"]);
ServerSetting.SmtpSSL = Convert.ToBoolean(builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpSSL"]);
ServerSetting.FromEmail = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["fromEmail"];
ServerSetting.FromFullName = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["fromFullName"];
ServerSetting.IsDefault = Convert.ToBoolean(builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["IsDefault"]);
ServerSetting.NoticeMaturityDateDay = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting")["NoticeMaturityDateDay"]);
#endregion

#region AddMariaDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
      //options.UseLazyLoadingProxies().UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")), providerOptions => providerOptions.EnableRetryOnFailure())); // 이거하니 Excel Export 안 됨
      options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")), providerOptions => providerOptions.EnableRetryOnFailure()).EnableSensitiveDataLogging());
#endregion

#region AddRepositories

#region DB
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IIncomeRepository, IncomeRepository>();
builder.Services.AddScoped<IExpenditureRepository, ExpenditureRepository>();
builder.Services.AddScoped<IFixedIncomeRepository, FixedIncomeRepository>();
builder.Services.AddScoped<IFixedExpenditureRepository, FixedExpenditureRepository>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IAttachedFileRepository, AttachedFileRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
#endregion

#region LOCAL
builder.Services.AddScoped<IMailRepository, MailRepository>();
#endregion

#endregion

#region AddSession
builder.Services.AddDistributedMemoryCache(); // Session Storage

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(ServerSetting.SessionExpireMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Domain = ServerSetting.DomainName.Replace("https://", "").Replace("/", "").ToString();
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
#endregion

#region AddAntiforgery
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Domain = ServerSetting.DomainName.Replace("https://", "").Replace("/", "").ToString();
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
#endregion

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddResponseCaching(); // For HTTP Header Setting 

#region 도메인 있을 때, 사용
//builder.Services.AddHsts(options => // https://blog.elmah.io/the-asp-net-core-security-headers-guide/
//{
//    options.IncludeSubDomains = true;
//    options.MaxAge = TimeSpan.FromDays(365);
//    options.Preload = true;
//});
#endregion

#region Multi-Language
builder.Services.AddLocalization(opt => { opt.ResourcesPath = "Resources"; });
builder.Services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(
    opt =>
    {
        List<CultureInfo> supportedCultures =
        [
                        new CultureInfo("en-US"),
                        new CultureInfo("ko-KR")
        ];
        opt.DefaultRequestCulture = new RequestCulture("en-US");
        opt.SupportedCultures = supportedCultures;
        opt.SupportedUICultures = supportedCultures;
    });
#endregion

builder.Services.AddControllersWithViews(options =>
{
    _ = options.Filters.Add(typeof(AuthorizationFilter)); // An instance
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // Globally enables token validation for all requests except for GET, HEAD, OPTIONS, and TRACE
});

WebApplication app = builder.Build();
//1.Exception / error handling

//앱이 개발환경에서 실행되는 경우
//개발자 예외 페이지 미들웨어가(UseDeveloperExceptionPage) 가 앱 런타임 오류를 보고합니다.
//데이터베이스 오류페이지 미들웨어가 데이터베이스 런타임 오류를 보고합니다.
//앱이 프로덕션 환경에서 실행되는 경우
//예외 처리기 미들웨어(UseExceptionHandler)는 다음 미들웨어에서 발생한 예외를 포착합니다.
//HSTS(HTTP Strict Transport Security Protocol) 미들웨어(UseHsts)는 Strict-Transport - Security헤더를 추가합니다.

//if (!app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/Exception/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always,
});

app.UseExceptionHandler("/Exception/Error");

#region When you have server domain, uncomment
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//app.UseHsts();

//app.UseRewriter(new RewriteOptions()
//   .AddRedirectToWwwPermanent()
//   .AddRedirectToHttpsPermanent()
//);
#endregion

#region When you don't have server domain, uncomment
app.UseHttpsRedirection(); // 2. HTTPS 리디렉션 미들웨어 (UseHttpsRedirection)는 HTTP 요청을 HTTPS로 리디렉션합니다.
                           //app.UseStaticFiles(); // 3. 정적 파일 미들웨어 (UseStaticFiles)는 정적 파일을 리턴하고 추가 요청 처리를 단락시킵니다.
                           // app.UseCookiePolicy(); 4. 쿠키 정책 미들웨어 (UseCookiePolicy)는 앱을 EU 일반 데이터 보호 규정 (GDPR) 규정에 따릅니다.
#endregion

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
        bool isAccountSessionExist = context.Context.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out _); // 로그인이 되어 계정 세션이 생겼는지 확인

        if (context.Context.Request.Path.StartsWithSegments("/upload/Forum") || context.Context.Request.Path.StartsWithSegments("/upload/Management/Profile/Avatar") || context.Context.Request.Path.ToString().Contains("/upload/Management/Profile/default-avatar.jpg")) // (~/upload/Forum) [게시물] (~/upload/Management/Profile/Avatar) [댓글 게시판 이미지] 게시판 파일들은 모두 접근 허용
        {

        }
        else if (isAccountSessionExist == false && !context.Context.Request.Path.StartsWithSegments("/anonymous")) // 로그인이 안 된 상태인가? 그렇다면 ~/anonymous 자원에만 접근 허용
        {
            context.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized; // 권한 없음 
            context.Context.Response.ContentLength = 0; // 내용 길이 없음
            context.Context.Response.Body = Stream.Null; // 내용 없음
            context.Context.Response.Redirect("/"); // 로그인 화면으로
        }
        else if (isAccountSessionExist == true)
        {
            _ = context.Context.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);
            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

            if (loginedAccount.Role == Role.Admin && !context.Context.Request.Path.StartsWithSegments("/admin")) // 로그인이 되었고 권한이 Admin인가? 그렇다면 ~/admin 자원에만 접근 허용
            {
                #region Profile Image 이거나, Profile 아바타 파일 접근 허용
                if (context.Context.Request.Path.ToString().Contains("/upload/Management/Profile/default-avatar.jpg") || context.Context.Request.Path.ToString().Contains($"/upload/Management/Profile/Avatar/"))
                {

                }
                #endregion

                #region 그 외 접근 차단
                else
                {
                    context.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized; // 권한 없음 
                    context.Context.Response.ContentLength = 0; // 내용 길이 없음
                    context.Context.Response.Body = Stream.Null; // 내용 없음
                    context.Context.Response.Redirect("/"); // 로그인 화면으로
                }
                #endregion

            }
            else if (loginedAccount.Role == Role.User && !context.Context.Request.Path.StartsWithSegments("/user")) // 로그인이 되었고 권한이 User인가? 그렇다면 ~/user 자원에만 접근 허용
            {
                #region Profile Image 이거나, Profile 아바타 파일 접근 허용
                if (context.Context.Request.Path.ToString().Contains("/upload/Management/Profile/default-avatar.jpg") || context.Context.Request.Path.ToString().Contains($"/upload/Management/Profile/Avatar/"))
                {

                }
                #endregion

                #region 그 외 접근 차단
                else
                {
                    context.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized; // 권한 없음 
                    context.Context.Response.ContentLength = 0; // 내용 길이 없음
                    context.Context.Response.Body = Stream.Null; // 내용 없음
                    context.Context.Response.Redirect("/"); // 로그인 화면으로
                }
                #endregion
            }
            else
            {

            }
        }
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

    #region Security Headers [https://blog.elmah.io/the-asp-net-core-security-headers-guide/]
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Xss-Protection", "1; mode=block");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; style-src 'self' 'unsafe-inline';"); // [https://csper.io/blog/no-more-unsafe-inline]
    #endregion
    await next();
});
#endregion
#endregion

//app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); }); //9.엔드 포인트 라우팅 미들웨어(MapRazorPages 를 포함하는 UseEndPoints)를 사용하여 요청 파이프라인에 Razor Pages 엔드포인트를 추가합니다.

#region Multi-Language
string[] supportedCultures = new[] { "en-US", "ko-KR" };
RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
#endregion

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=DashBoard}/{action=AnonymousIndex}/{id?}");

app.Run();