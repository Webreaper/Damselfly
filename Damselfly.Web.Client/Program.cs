using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Damselfly.Web.Client;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using MudBlazor.Services;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.ClientServices;

namespace Damselfly.Web.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var httpClientBuilder = builder.Services.AddHttpClient("DamselflyAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

        // WASM: TODO: 
        //httpClientBuilder.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

        // Supply HttpClient instances that include access tokens when making requests to the server project
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DamselflyAPI"));
        builder.Services.AddMemoryCache(x => x.SizeLimit = 500);

        builder.Services.AddApiAuthorization();
        builder.Services.AddAuthorizationCore(config => config.SetupPolicies(builder.Services));

        builder.Services.AddMudServices();

        builder.Services.AddScoped<ContextMenuService>();
        builder.Services.AddSingleton<RestClient>();

        builder.Services.AddDamselflyUIServices();

        await builder.Build().RunAsync();
    }
}

