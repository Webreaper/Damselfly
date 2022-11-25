using System;
using System.ComponentModel.DataAnnotations;
using Damselfly.Core.Constants;
using FluentValidation;

namespace Damselfly.Core.DbModels.Models;

public class AzureSettings
{
    [Url] public string Endpoint { get; set; }

    public string ApiKey { get; set; }
    public AzureDetection DetectionType { get; set; }
    public bool UsingFreeTier { get; set; } = true;
}

public class AzureSettingsValidator : AbstractValidator<AzureSettings>
{
    public AzureSettingsValidator()
    {
        RuleFor(p => p.Endpoint).Must(x => IsValidUrl(x))
            .WithMessage("Endpoint must be a full URL")
            .When(az => !string.IsNullOrEmpty(az.Endpoint));
        RuleFor(p => p.ApiKey).NotEmpty()
            .WithMessage("You must enter a valid API Key")
            .When(az => !string.IsNullOrEmpty(az.ApiKey));
    }

    private bool IsValidUrl(string url)
    {
        return Uri.IsWellFormedUriString(url, UriKind.Absolute);
    }
}