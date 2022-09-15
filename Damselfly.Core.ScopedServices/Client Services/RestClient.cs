using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Damselfly.Core.ScopedServices.ClientServices;

/// <summary>
///     This class is to manage the fact that in Blazor WASM it's essential that
///     ReferenceHandler.Preserve is enabled, and case insensitivity is enabled.
///     If not, JSON payloads won't be serialized correctly, and will create
///     infinitely deep cycles.
///     This should be added as a singleton so that one single jsonOptions object
///     is created and used everywhere.
/// </summary>
public class RestClient
{
    public static readonly JsonSerializerOptions JsonOptions = SetJsonOptions(new JsonSerializerOptions());

    private readonly HttpClient _restClient;

    public RestClient(HttpClient client)
    {
        _restClient = client;
    }

    public AuthenticationHeaderValue AuthHeader
    {
        get => _restClient.DefaultRequestHeaders.Authorization;
        set => _restClient.DefaultRequestHeaders.Authorization = value;
    }

    /// <summary>
    ///     Helper method to set consistent JSON serialization options
    ///     from everywhere. This should be used in both the client
    ///     and server, e.g:
    ///     builder.Services.AddControllersWithViews()
    ///     .AddJsonOptions(o => { RestClient.SetJsonOptions(o.JsonSerializerOptions); });
    /// </summary>
    /// <param name="opts"></param>
    public static JsonSerializerOptions SetJsonOptions(JsonSerializerOptions opts)
    {
        opts.ReferenceHandler = ReferenceHandler.Preserve;
        opts.PropertyNameCaseInsensitive = true;
        opts.PropertyNamingPolicy = null;
        return opts;
    }

    private Exception GetRestException(Exception ex, string requestUrl)
    {
        if ( ex is JsonException )
        {
            if ( ex.Message.Contains("'<' is an invalid start of a value") )
                return new ArgumentException($"Possible 404 / Page Not Found exception for {requestUrl}", ex);
            if ( ex.Message.Contains("A possible object cycle") )
                return new ArgumentException($"Object cycle exception for {requestUrl}", ex);
        }

        return ex;
    }

    public async Task<T?> CustomGetFromJsonAsync<T>(string? requestUri)
    {
        try
        {
            return await _restClient.GetFromJsonAsync<T>(requestUri, JsonOptions);
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPostAsync(string? requestUri)
    {
        try
        {
            var msg = await _restClient.PostAsync(requestUri, null);
            return msg;
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPostAsJsonAsync<PostObj>(string? requestUri, PostObj obj)
    {
        try
        {
            var msg = await _restClient.PostAsJsonAsync(requestUri, obj, JsonOptions);
            return msg;
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<RetObj?> CustomPostAsJsonAsync<PostObj, RetObj>(string? requestUri, PostObj obj)
    {
        try
        {
            var response = await _restClient.PostAsJsonAsync(requestUri, obj, JsonOptions);
            return await response.Content.ReadFromJsonAsync<RetObj>(JsonOptions);
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<RetObj?> CustomPutAsJsonAsync<PostObj, RetObj>(string? requestUri, PostObj obj)
    {
        try
        {
            var response = await _restClient.PutAsJsonAsync(requestUri, obj, JsonOptions);
            return await response.Content.ReadFromJsonAsync<RetObj>(JsonOptions);
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPutAsJsonAsync<T>(string? requestUri, T obj)
    {
        try
        {
            return await _restClient.PutAsJsonAsync(requestUri, obj, JsonOptions);
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomDeleteAsync(string? requestUri)
    {
        try
        {
            return await _restClient.DeleteAsync(requestUri);
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPatchAsJsonAsync<T>(string? requestUri, T obj)
    {
        try
        {
            return await _restClient.PatchAsJsonAsync(requestUri, obj, JsonOptions);
        }
        catch ( Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }
}