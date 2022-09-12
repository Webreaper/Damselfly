using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Radzen;
using MudBlazor.Services;
using Damselfly.Shared.Utils;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.ClientServices;
using Syncfusion.Blazor;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace Damselfly.Web.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        var levelSwitch = new LoggingLevelSwitch();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy( levelSwitch )
            .Enrich.WithProperty( "InstanceId", Guid.NewGuid().ToString( "n" ) )
            .WriteTo.BrowserHttp(
                        endpointUrl: $"{builder.HostEnvironment.BaseAddress}ingest",
                        controlLevelSwitch: levelSwitch )
            .CreateLogger();

        builder.Logging.AddProvider( new SerilogLoggerProvider() );

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var httpClientBuilder = builder.Services.AddHttpClient("DamselflyAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

        // Supply HttpClient instances that include access tokens when making requests to the server project
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DamselflyAPI"));
        builder.Services.AddMemoryCache(x => x.SizeLimit = 500);

        builder.Services.AddAuthorizationCore(config => config.SetupPolicies(builder.Services));

        builder.Services.AddMudServices();
        builder.Services.AddSyncfusionBlazor(options => { options.IgnoreScriptIsolation = true; });
        builder.Services.AddBlazoredLocalStorage();

        builder.Services.AddScoped<ContextMenuService>();
        builder.Services.AddSingleton<RestClient>();

        builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
        builder.Services.AddScoped<IAuthService, AuthService>();

        builder.Services.AddDamselflyUIServices();

        SyncfusionLicence.RegisterSyncfusionLicence();

        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;

        var app = builder.Build();

        var cachedData = app.Services.GetRequiredService<ICachedDataService>();
        await cachedData.InitialiseData();

        await app.RunAsync();
    }
}

