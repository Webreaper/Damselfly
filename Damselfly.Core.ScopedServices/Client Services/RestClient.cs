using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.ClientServices;

public class RestClient
{
    private JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
                ReferenceHandler = ReferenceHandler.Preserve,
                PropertyNameCaseInsensitive = true };

    private readonly HttpClient _restClient;

    public RestClient( HttpClient client )
    {
        _restClient = client;
    }

    public async Task<T?> CustomGetFromJsonAsync<T>(string? requestUri)
    {
        try {
            return await _restClient.GetFromJsonAsync<T>(requestUri, jsonOptions);
        }
        catch( JsonException ex )
        {
            // WASM:
            if (ex.Message.Contains("'<' is an invalid start of a value"))
                throw new ArgumentException($"Possible 404 exception for {requestUri}");
            else
                throw;
        }
    }

    public async Task CustomPostAsJsonAsync<PostObj>(string? requestUri, PostObj obj)
    {
        await _restClient.PostAsJsonAsync<PostObj>(requestUri, obj, jsonOptions);
    }

    public async Task<RetObj?> CustomPostAsJsonAsync<PostObj, RetObj>(string? requestUri, PostObj obj)
    {
        var response = await _restClient.PostAsJsonAsync<PostObj>(requestUri, obj, jsonOptions);
        return await response.Content.ReadFromJsonAsync<RetObj>(jsonOptions);
    }

    public async Task<RetObj?> CustomPutAsJsonAsync<PostObj, RetObj>(string? requestUri, PostObj obj)
    {
        var response = await _restClient.PutAsJsonAsync<PostObj>(requestUri, obj, jsonOptions);
        return await response.Content.ReadFromJsonAsync<RetObj>(jsonOptions);
    }

    public async Task<HttpResponseMessage> CustomPutAsJsonAsync<T>(string? requestUri, T obj)
    {
        return await _restClient.PutAsJsonAsync<T>(requestUri, obj, jsonOptions);
    }

    public async Task<HttpResponseMessage> CustomDeleteAsync(string? requestUri)
    {
        return await _restClient.DeleteAsync(requestUri);
    }

    public async Task<HttpResponseMessage> CustomPatchAsJsonAsync<T>(string? requestUri, T obj)
    {
        return await _restClient.PatchAsJsonAsync<T>(requestUri, obj, jsonOptions);
    }
}

