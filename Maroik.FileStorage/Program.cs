using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Services;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.FileStorage.Contracts;
using Maroik.FileStorage.Infrastructure;
using Maroik.FileStorage.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Authentication;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureEndpointDefaults(listenOptions =>
    {
        serverOptions.Limits.MinRequestBodyDataRate = new MinDataRate(100, TimeSpan.FromSeconds(10));
        serverOptions.Limits.MinResponseDataRate = new MinDataRate(100, TimeSpan.FromSeconds(10));
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
        serverOptions.ConfigureHttpsDefaults(listenOptions =>
        {
            listenOptions.SslProtocols = SslProtocols.Tls12;
        });
    });
});

#region ServerSettings
ServerSetting.MaxLoginAttempt = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting")["MaxLoginAttempt"]);
#endregion

// Add services to the container.

builder.Services.AddControllers();

JwtTokenConfig jwtTokenConfig = builder.Configuration.GetSection("jwtTokenConfig").Get<JwtTokenConfig>();
builder.Services.AddSingleton(jwtTokenConfig);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtTokenConfig.Issuer,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtTokenConfig.Secret)),
        ValidAudience = jwtTokenConfig.Audience,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddSingleton<IJwtAuthManager, JwtAuthManager>();
builder.Services.AddHostedService<JwtRefreshTokenCache>();

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
#endregion

#region LOCAL
builder.Services.AddScoped<IUserRepository, UserRepository>();
#endregion

#endregion

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Maroik.FileStorage", Version = "v1" });

    OpenApiSecurityScheme securityScheme = new()
    {
        Name = "Maroik.FileStorage",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer", // must be lower case
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
                    {securityScheme, Array.Empty<string>()}
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => { _ = builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

#region When you have server domain, uncomment
//app.UseRewriter(new RewriteOptions()
//   .AddRedirectToWwwPermanent()
//   .AddRedirectToHttpsPermanent()
//);
#endregion

#region When you don't have server domain, uncomment
//app.UseHttpsRedirection();
#endregion

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("./swagger/v1/swagger.json", "Maroik.FileStorage");
    c.DocumentTitle = "Maroik.FileStorage";
    c.RoutePrefix = string.Empty;
});

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();