using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Damselfly.Web.Extensions;

/// <summary>
/// from https://www.meziantou.net/bind-parameters-from-the-query-string-in-blazor.htm
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class QueryStringParameterAttribute : Attribute
{
    public QueryStringParameterAttribute()
    {
    }

    public QueryStringParameterAttribute(string name)
    {
        Name = name;
    }

    /// <summary>Name of the query string parameter. It uses the property name by default.</summary>
    public string Name { get; }
}

// Requires Microsoft.AspNetCore.WebUtilities to edit the query string
// <PackageReference Include="Microsoft.AspNetCore.WebUtilities" Version="2.2.0" />
public static class QueryStringParameterExtensions
{
    // Apply the values from the query string to the current component
    public static void SetParametersFromQueryString<T>(this T component, NavigationManager navigationManager)
        where T : ComponentBase
    {
        if (!Uri.TryCreate(navigationManager.Uri, UriKind.RelativeOrAbsolute, out var uri))
            throw new InvalidOperationException("The current url is not a valid URI. Url: " + navigationManager.Uri);

        // Parse the query string
        Dictionary<string, StringValues> queryString = QueryHelpers.ParseQuery(uri.Query);

        // Enumerate all properties of the component
        foreach (var property in GetProperties<T>())
        {
            // Get the name of the parameter to read from the query string
            var parameterName = GetQueryStringParameterName(property);
            if (parameterName == null)
                continue; // The property is not decorated by [QueryStringParameterAttribute]

            if (queryString.TryGetValue(parameterName, out var value))
            {
                // Convert the value from string to the actual property type
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(component, convertedValue);
            }
        }
    }

    // Apply the values from the component to the query string
    public static void UpdateQueryString<T>(this T component, NavigationManager navigationManager)
        where T : ComponentBase
    {
        if (!Uri.TryCreate(navigationManager.Uri, UriKind.RelativeOrAbsolute, out var uri))
            throw new InvalidOperationException("The current url is not a valid URI. Url: " + navigationManager.Uri);

        // Fill the dictionary with the parameters of the component
        Dictionary<string, StringValues> parameters = QueryHelpers.ParseQuery(uri.Query);
        foreach (var property in GetProperties<T>())
        {
            var parameterName = GetQueryStringParameterName(property);
            if (parameterName == null)
                continue;

            var value = property.GetValue(component);
            if (value is null)
            {
                parameters.Remove(parameterName);
            }
            else
            {
                var convertedValue = ConvertToString(value);
                parameters[parameterName] = convertedValue;
            }
        }

        // Compute the new URL
        var newUri = uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);
        foreach (var parameter in parameters)
        {
            foreach (var value in parameter.Value)
            {
                newUri = QueryHelpers.AddQueryString(newUri, parameter.Key, value);
            }
        }

        navigationManager.NavigateTo(newUri);
    }

    private static object ConvertValue(StringValues value, Type type)
    {
        return Convert.ChangeType(value[0], type, CultureInfo.InvariantCulture);
    }

    private static string ConvertToString(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static PropertyInfo[] GetProperties<T>()
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static string GetQueryStringParameterName(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<QueryStringParameterAttribute>();
        if (attribute == null)
            return null;

        return attribute.Name ?? property.Name;
    }
}
