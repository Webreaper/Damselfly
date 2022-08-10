using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/people")]
public class PeopleController : ControllerBase
{
    private readonly ImageRecognitionService _aiService;

    private readonly ILogger<PeopleController> _logger;

    public PeopleController(ImageRecognitionService service, ILogger<PeopleController> logger)
    {
        _aiService = service;
        _logger = logger;
    }

    [HttpGet("/api/people/{searchText}")]
    public async Task<List<string>> Get( string searchText )
    {
        // Union the search term with the results from the DB. Order them so that the
        // closest matches to the start of the string come first.
        var names = _aiService.GetCachedPeople().Where(x => x.Name.StartsWith(searchText.Trim(), StringComparison.OrdinalIgnoreCase))
                                           .Select(x => x.Name)
                                           .Distinct()
                                           .OrderBy(x => x.ToUpper().IndexOf(searchText.ToUpper()))
                                           .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                                           .ToList();
        return names;
    }
}

