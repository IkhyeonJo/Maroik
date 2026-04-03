using Microsoft.OpenApi;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Client.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IClamavClient, ClamavClient>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Maroik.FileStorage", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder => { _ = policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("./swagger/v1/swagger.json", "Maroik.FileStorage");
    c.DocumentTitle = "Maroik.FileStorage";
    c.RoutePrefix = string.Empty;
});

app.UseRouting();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();