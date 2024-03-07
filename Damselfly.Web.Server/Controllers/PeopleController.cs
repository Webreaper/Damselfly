using Damselfly.Core.Constants;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
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

    [HttpGet("/api/person/{personId}")]
    public async Task<Person> GetPerson( int personId )
    {
        return await _aiService.GetPerson( personId );
    }

    [HttpGet("/api/people")]
    public async Task<List<Person>> Get()
    {
        var names = await _aiService.GetAllPeople();
        return names;
    }

    [HttpGet("/api/people/{searchText}")]
    public async Task<List<string>> Search(string searchText)
    {
        var names = await _aiService.GetPeopleNames(searchText);
        return names;
    }

    [HttpPut("/api/object/name")]
    public async Task UpdateName( NameChangeRequest req, [FromServices] ImageContext db )
    {
        var obj = db.ImageObjects
                    .Include(x => x.Person)
                    .FirstOrDefault(n => n.ImageObjectId == req.ObjectId);

        if (obj is not null)
        {
            await _aiService.UpdateName( obj, req.NewName, req.Merge );
        }
    }
}