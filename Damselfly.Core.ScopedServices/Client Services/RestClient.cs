using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Damselfly.Core.Models;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices.ClientServices;

/// <summary>
/// This class is to manage the fact that in Blazor WASM it's essential that
/// ReferenceHandler.Preserve is enabled, and case insensitivity is enabled.
/// If not, JSON payloads won't be serialized correctly, and will create
/// infinitely deep cycles.
/// This should be added as a singleton so that one single jsonOptions object
/// is created and used everywhere.
/// </summary>
public class RestClient
{
    /// <summary>
    /// Helper method to set consistent JSON serialization options
    /// from everywhere. This should be used in both the client
    /// and server, e.g:
    ///   builder.Services.AddControllersWithViews()
    ///      .AddJsonOptions(o => { RestClient.SetJsonOptions(o.JsonSerializerOptions); });
    /// </summary>
    /// <param name="opts"></param>
    public static void SetJsonOptions(JsonSerializerOptions opts)
    {
        opts.ReferenceHandler = ReferenceHandler.Preserve;
        opts.PropertyNameCaseInsensitive = true;
    }

    private readonly JsonSerializerOptions jsonOptions;
    private readonly HttpClient _restClient;
    private readonly ILogger<RestClient> _logger;

    public RestClient( HttpClient client, ILogger<RestClient> logger )
    {
        _logger = logger;
        _restClient = client;
        jsonOptions = new JsonSerializerOptions();

        SetJsonOptions(jsonOptions);
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

