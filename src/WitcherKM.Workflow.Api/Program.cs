using WitcherKM.Workflow.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

    var licenseKey = builder.Configuration["MediatR:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
    {
        configuration.LicenseKey = licenseKey;
    }
});

var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new HealthResponse("ok")));

app.Run();

internal sealed record HealthResponse(string Status);
