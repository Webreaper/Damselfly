using System;
using Damselfly.Core.ScopedServices;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.Utils;

public static class ServiceRegistrations
{
	public static IServiceCollection AddUserServices( this IServiceCollection services )
	{
        return services.AddScoped<UserFolderService>()
                       .AddScoped<UserService>()
                       .AddScoped<UserStatusService>()
                       .AddScoped<UserConfigService>()
                       .AddScoped<ViewDataService>()
                       .AddScoped<UserThemeService>()
                       .AddScoped<UserTagFavouritesService>();

    }
}

