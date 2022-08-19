using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.ClientServices;

public static class JsonSerialiser
{
    private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
                ReferenceHandler = ReferenceHandler.Preserve,
                PropertyNameCaseInsensitive = true };

    public static async Task<T?> CustomGetFromJsonAsync<T>(this HttpClient httpClient, string? requestUri )
    {
        return await httpClient.GetFromJsonAsync<T>(requestUri, jsonOptions);
    }

    public static async Task<RetObj?> CustomPostAsJsonAsync<PostObj, RetObj>(this HttpClient httpClient, string? requestUri, PostObj obj)
    {
        var response = await httpClient.PostAsJsonAsync<PostObj>(requestUri, obj, jsonOptions);
        return await response.Content.ReadFromJsonAsync<RetObj>(jsonOptions);
    }

    public static async Task<RetObj?> CustomPutAsJsonAsync<PostObj, RetObj>(this HttpClient httpClient, string? requestUri, PostObj obj)
    {
        var response = await httpClient.PutAsJsonAsync<PostObj>(requestUri, obj, jsonOptions);
        return await response.Content.ReadFromJsonAsync<RetObj>(jsonOptions);
    }

    public static async Task<HttpResponseMessage> CustomPutAsJsonAsync<T>(this HttpClient httpClient, string? requestUri, T obj)
    {
        return await httpClient.PutAsJsonAsync<T>(requestUri, obj, jsonOptions);
    }
}

