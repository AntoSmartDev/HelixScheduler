using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DemoWebOptions>(
    builder.Configuration.GetSection(DemoWebOptions.SectionName));

var app = builder.Build();

app.MapGet("/config", (IOptions<DemoWebOptions> options) =>
{
    return Results.Ok(new { apiBaseUrl = options.Value.ApiBaseUrl });
});

app.MapGet("/search", () => Results.Redirect("/search.html"));

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
