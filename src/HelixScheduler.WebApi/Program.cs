using HelixScheduler.Infrastructure.SqlServer;
using HelixScheduler.WebApi.Extensions;
using HelixScheduler.WebApi.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoWeb", policy =>
        policy.WithOrigins("https://localhost:7040")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    HelixScheduler.WebApi.Management.ManagementOpenApiConfiguration.Configure(options);
});
builder.Services.AddHelixSchedulerInfrastructureSqlServer(builder.Configuration);
builder.Services.AddHelixSchedulerWebApi();

var app = builder.Build();
var exposeOpenApi = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Expose");

// Configure the HTTP request pipeline.
if (exposeOpenApi)
{
    app.MapOpenApi();
}

await app.UseHelixSchedulerDemoSeedAsync();

app.UseHttpsRedirection();

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseCors("DemoWeb");
app.UseAuthorization();

app.MapGet("/", () =>
    Results.Content(
        """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>HelixScheduler WebApi</title>
          <style>
            body { font-family: "Segoe UI", Tahoma, sans-serif; background: #f6f3ee; color: #2f2924; padding: 32px; }
            h1 { margin: 0 0 8px; }
            ul { padding-left: 18px; }
            a { color: #2f2924; }
          </style>
        </head>
        <body>
          <h1>HelixScheduler WebApi</h1>
          <p>Service is running.</p>
          <ul>
            <li><a href="/openapi/v1.json">OpenAPI JSON (when enabled)</a></li>
            <li><a href="/health">Health</a></li>
          </ul>
        </body>
        </html>
        """,
        "text/html"));

app.MapControllers();

app.Run();
