using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models;

public class LoginResult
{
    public bool Successful { get; set; }
    public string Error { get; set; }
    public string Token { get; set; }
}

public class LoginModel
{
    [Required] public string Email { get; set; }

    [Required] public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class RegisterModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
        MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
}

public class RegisterResult
{
    public bool Successful { get; set; }
    public IEnumerable<string> Errors { get; set; }
}

public class UserModel
{
    public string Email { get; set; }
    public bool IsAuthenticated { get; set; }
}