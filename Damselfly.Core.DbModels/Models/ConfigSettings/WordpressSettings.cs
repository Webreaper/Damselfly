using System;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Damselfly.Core.Models
{
    public class WordpressSettings
    {
        [Url]
        public string URL { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class WordpressSettingsValidator : AbstractValidator<WordpressSettings>
    {
        public WordpressSettingsValidator()
        {
            RuleFor(p => p.URL).Must(x => IsValidUrl(x))
                               .WithMessage("URL must be a full URL")
                               .When(wp => !string.IsNullOrEmpty(wp.URL));
            RuleFor(p => p.UserName).NotEmpty()
                                    .WithMessage("You must enter a Email address")
                                    .When(wp => !string.IsNullOrEmpty(wp.URL));
            RuleFor(p => p.Password).NotEmpty()
                                    .WithMessage("You must enter a Password")
                                    .When(wp => !string.IsNullOrEmpty(wp.URL));

            // RuleFor(p => p.Address).SetValidator(new AddressValidator());
        }

        private bool IsValidUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }
    }
}
