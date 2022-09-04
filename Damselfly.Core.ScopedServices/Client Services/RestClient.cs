using System;
using System.Net.Http.Headers;
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
    public static JsonSerializerOptions SetJsonOptions(JsonSerializerOptions opts)
    {
        opts.ReferenceHandler = ReferenceHandler.Preserve;
        opts.PropertyNameCaseInsensitive = true;
        return opts;
    }

    public static readonly JsonSerializerOptions JsonOptions = SetJsonOptions(new JsonSerializerOptions());

    public AuthenticationHeaderValue AuthHeader
    {
        get { return _restClient.DefaultRequestHeaders.Authorization; }
        set { _restClient.DefaultRequestHeaders.Authorization = value; }
    }

    private readonly HttpClient _restClient;
    private readonly ILogger<RestClient> _logger;

    public RestClient( HttpClient client, ILogger<RestClient> logger )
    {
        _logger = logger;
        _restClient = client;
    }

    private Exception GetRestException( Exception ex, string requestUrl )
    {
        if( ex is JsonException )
        {
            // WASM:
            if (ex.Message.Contains("'<' is an invalid start of a value"))
                return new ArgumentException($"Possible 404 / Page Not Found exception for {requestUrl}", ex);
            else if (ex.Message.Contains("A possible object cycle"))
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
        catch( Exception ex )
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
        catch (Exception ex)
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPostAsJsonAsync<PostObj>(string? requestUri, PostObj obj)
    {
        try
        {
            var msg = await _restClient.PostAsJsonAsync<PostObj>(requestUri, obj, JsonOptions);
            return msg;
        }
        catch (Exception ex)
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<RetObj?> CustomPostAsJsonAsync<PostObj, RetObj>(string? requestUri, PostObj obj)
    {
        try
        { 
            var response = await _restClient.PostAsJsonAsync<PostObj>(requestUri, obj, JsonOptions);
            return await response.Content.ReadFromJsonAsync<RetObj>(JsonOptions);
        }
        catch (Exception ex)
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<RetObj?> CustomPutAsJsonAsync<PostObj, RetObj>(string? requestUri, PostObj obj)
    {
        try
        {
            var response = await _restClient.PutAsJsonAsync<PostObj>(requestUri, obj, JsonOptions);
            return await response.Content.ReadFromJsonAsync<RetObj>(JsonOptions);
        }
        catch (Exception ex)
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPutAsJsonAsync<T>(string? requestUri, T obj)
    {
        try
        { 
            return await _restClient.PutAsJsonAsync<T>(requestUri, obj, JsonOptions);
        }
        catch(Exception ex )
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomDeleteAsync(string? requestUri)
    {
        try { 
            return await _restClient.DeleteAsync(requestUri);
        }
        catch (Exception ex)
        {
            throw GetRestException(ex, requestUri);
        }
    }

    public async Task<HttpResponseMessage> CustomPatchAsJsonAsync<T>(string? requestUri, T obj)
    {
        try { 
            return await _restClient.PatchAsJsonAsync<T>(requestUri, obj, JsonOptions);
        }
        catch (Exception ex)
        {
            throw GetRestException(ex, requestUri);
        }
    }
}

