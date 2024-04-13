using Damselfly.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers
{
    [Authorize(Policy = PolicyDefinitions.s_IsAdmin)]
    [ApiController]
    [Route("albums")]
    public class AlbumsController : ControllerBase
    {
        
    }
}
