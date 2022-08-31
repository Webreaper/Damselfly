using Damselfly.Core.DbModels;
using System.Collections.Generic;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/users")]
public class UserManagementController : ControllerBase
{
    private readonly IUserMgmtService _service;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(IUserMgmtService service, ILogger<UserManagementController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/api/users/roles")]
    public async Task<ICollection<ApplicationRole>> GetAllRoles()
    {
        return await _service.GetRoles();
    }

    [HttpGet("/api/users")]
    public async Task<ICollection<AppIdentityUser>> GetAllUsers()
    {
        return await _service.GetUsers();
    }

    [HttpPut("/api/users")]
    public async Task<IdentityResult> CreateUser(UserRequest request )
    {
        return await _service.CreateNewUser( request.User, request.Password, request.Roles );
    }

    [HttpPost("/api/users")]
    public async Task<IdentityResult> UpdateUser(UserRequest request)
    {
        if (request.Roles != null && request.Roles.Any())
            return await _service.UpdateUserAsync(request.User, request.Roles);
        else
            return await _service.SetUserPasswordAsync(request.User, request.Password);
        }
}

