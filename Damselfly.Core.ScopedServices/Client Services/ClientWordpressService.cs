using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientWordpressService : IWordpressService
{
    private readonly RestClient httpClient;

    public ClientWordpressService(RestClient client)
    {
        httpClient = client;
    }

    public async Task UploadImagesToWordpress(List<Image> images)
    {
        await httpClient.CustomPostAsJsonAsync("/api/wordpress", images);
    }
}