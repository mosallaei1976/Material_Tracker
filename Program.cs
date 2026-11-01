using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Material_Tracker;
using Material_Tracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<IJsonBackupService, JsonBackupService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

await builder.Build().RunAsync();
