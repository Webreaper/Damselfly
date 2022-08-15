using System;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class BaseClientService
{
    // WASM: TODO: Use [Inject] here?
    protected HttpClient httpClient;

    public BaseClientService(HttpClient client)
    {
        httpClient = client;
    }
}

