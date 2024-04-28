using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

[ApiController]
[Route("/api/users")]
public class UserManagementController : ControllerBase
{
    private readonly ILogger<UserManagementController> _logger;
    private readonly IUserMgmtService _service;

    public UserManagementController(IUserMgmtService service, ILogger<UserManagementController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [AuthorizeFireBase]
    [Route("signedIn")]
    public async Task<IActionResult> SignedIn()
    {
        var user = HttpContext.User;
        var appUser = await _service.GetOrCreateUser(user);
        
        return Ok(new SignedInResponse { UserId = appUser.Id, UserName = appUser.UserName });
    }

}