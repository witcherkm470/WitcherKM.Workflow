using Microsoft.EntityFrameworkCore;
using WitcherKM.Workflow.Application;
using WitcherKM.Workflow.Domain;
using WitcherKM.Workflow.Infrastructure;
using WitcherKM.Workflow.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);

builder.Services.AddControllers();
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "workflow.db")}"));
builder.Services.AddScoped<IWorkflowStore, WorkflowStore>();
builder.Services.AddSingleton<IExternalImageFetcher, ExternalImageDownloader>();
builder.Services.AddScoped<IImageStorage>(provider =>
    new LocalImageStorage(dataDirectory, provider.GetRequiredService<IWorkflowStore>(),
        provider.GetRequiredService<IExternalImageFetcher>()));
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Database.MigrateAsync();

app.UseMiddleware<WorkflowExceptionMiddleware>();
app.MapControllers();
app.Run();
