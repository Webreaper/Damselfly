using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Damselfly.Core.DbModels.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Damselfly.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly IEmailSender _emailSender;
    private readonly UserManager<AppIdentityUser> _userManager;

    public ForgotPasswordModel(UserManager<AppIdentityUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty] public InputModel Input { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if ( ModelState.IsValid )
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            // TODO: Hack
            if ( !emailConfirmed )
                emailConfirmed = true;

            if ( user == null || !emailConfirmed )
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToPage("./ForgotPasswordConfirmation");

            // For more information on how to enable account confirmation and password reset please 
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                null,
                new { area = "Identity", code },
                Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Reset Damselfly Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        return Page();
    }

    public class InputModel
    {
        [Required] [EmailAddress] public string Email { get; set; }
    }
}