using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IAuthService
{
    Task<LoginResult> Login(LoginModel loginModel);
    Task Logout();
    Task<RegisterResult> Register(RegisterModel registerModel);
    Task<bool> CheckCurrentFirebaseUserIsInRole(string[] roles);
    Task<string> GetCurrentUserEmail();
}