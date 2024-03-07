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
public class PeopleController( ImageRecognitionService _aiService, 
                                ILogger<PeopleController> _logger,
                                ImageCache _imageCache) : ControllerBase
{
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

    [HttpPut("/api/people/name")]
    public async Task UpdatePersonName( NameChangeRequest req )
    {
        await _aiService.UpdatePersonName( req );
        
        // TODO: Evict the image from the cache here?
    }

}