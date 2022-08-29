using System;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace ChrisSaintyExample.Client.Services;

public interface IAuthService
{
    Task<LoginResult> Login(LoginModel loginModel);
    Task Logout();
    Task<RegisterResult> Register(RegisterModel registerModel);
}