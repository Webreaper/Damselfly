using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationWithClientSideBlazor.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
public class AccountsController : ControllerBase
{
    private readonly IAuthService _authService;

    public AccountsController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RegisterModel model)
    {
        var response = await _authService.Register( model );

        return Ok(response);
    }
}