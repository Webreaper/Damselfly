using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new BooleanResultModel { Result = true});
        }

        [HttpGet]
        [Route("Authenticated")]
        [Authorize]
        public IActionResult Authenticated()
        {
            var user = HttpContext.User;
            return Ok(new BooleanResultModel { Result = true});
        }

        [HttpGet]
        [Route("Admin")]
        [AuthorizeFireBase(RoleDefinitions.s_AdminRole)]
        public IActionResult Admin()
        {
            var user = HttpContext.User;
            return Ok(new BooleanResultModel { Result = true});
        }
    }
}
