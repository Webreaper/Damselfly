using Blazored.LocalStorage;
using BlazorPanzoom;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Client.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudExtensions.Services;
using Radzen;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Syncfusion.Blazor;

namespace Damselfly.Web.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        SyncfusionLicence.RegisterSyncfusionLicence();

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        var levelSwitch = new LoggingLevelSwitch();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .Filter.ByExcluding(Matching.FromSource("Microsoft"))
            .Filter.ByExcluding(Matching.FromSource("System"))
            .Enrich.WithProperty("InstanceId", Guid.NewGuid().ToString("n"))
            .WriteTo.BrowserHttp(
                $"{builder.HostEnvironment.BaseAddress}ingest",
                controlLevelSwitch: levelSwitch)
            .CreateLogger();

        builder.Logging.AddProvider(new SerilogLoggerProvider());

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var httpClientBuilder = builder.Services.AddHttpClient("DamselflyAPI",
            client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

        // Supply HttpClient instances that include access tokens when making requests to the server project
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DamselflyAPI"));
        builder.Services.AddMemoryCache(x => x.SizeLimit = 500);

        builder.Services.AddAuthorizationCore(config => config.SetupPolicies(builder.Services));

        builder.Services.AddMudServices();
        builder.Services.AddMudExtensions();
        builder.Services.AddSyncfusionBlazor();
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddBlazorPanzoomServices();

        builder.Services.AddScoped<ContextMenuService>();
        builder.Services.AddScoped<RestClient>();

        builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
        builder.Services.AddScoped<IAuthService, ClientAuthService>();

        builder.Services.AddDamselflyUIServices();

        levelSwitch.MinimumLevel = LogEventLevel.Warning;

        var app = builder.Build();

        var cachedData = app.Services.GetRequiredService<ICachedDataService>();
        await cachedData.InitialiseData();

        var configService = app.Services.GetRequiredService<ClientConfigService>();
        await configService.InitialiseCache();

        await app.RunAsync();
    }
}