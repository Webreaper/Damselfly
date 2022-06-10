using Damselfly.Core.Interfaces;
using Damselfly.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.ImageProcessing;

public static class ServiceRegistration
{
	public static IServiceCollection AddImageServices(this IServiceCollection services)
	{
		return services.AddSingleton<ImageProcessorFactory>()
					   .AddSingleton<IImageProcessorFactory>(x => x.GetRequiredService<ImageProcessorFactory>())
					   .AddSingleton<ImageProcessService>();
	}
}

