using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Utils;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers;

//[Authorize(Policy = PolicyDefinitions.s_IsLoggedIn)]
[ApiController]
[Route("/api/logs")]
[AuthorizeFireBase(RoleDefinitions.s_AdminRole)]
public class LogController : ControllerBase
{
    private readonly ILogger<LogController> _logger;

    public LogController(ILogger<LogController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/api/logs/{page}")]
    public async Task<LogEntryResponse> Get(int page)
    {
        return await GetLogLines(page);
    }

    private Task<LogEntryResponse> GetLogLines(int page)
    {
        const int pageSize = 500;
        var response = new LogEntryResponse();

        var logDir = new DirectoryInfo(Logging.LogFolder);
        var file = logDir.GetFiles("*.log")
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault();

        if ( file != null )
        {
            response.LogEntries = new List<LogEntry>();
            response.LogFileName = file.Name;

            try
            {
                var reader = new ReverseLineReader(file.FullName);

                response.LogEntries = reader.Skip(page * pageSize)
                    .Take(pageSize)
                    .Select(x => CreateLogEntry(x))
                    .ToList();
            }
            catch ( Exception ex )
            {
                _logger.LogError($"Exception reading logs: {ex}");
            }
        }

        return Task.FromResult(response);
    }

    // TODO: Use a regex here
    private LogEntry CreateLogEntry(string s)
    {
        var e = new LogEntry();
        if ( !string.IsNullOrWhiteSpace(s) && s.StartsWith('[') )
            try
            {
                var parts = s.Split(']');
                if ( parts.Length > 1 )
                {
                    e.Entry = parts[1];

                    var parts2 = parts[0].Substring(1).Split('-');
                    e.Date = parts2[0];
                    e.Thread = parts2[1];
                    e.Level = parts2[2];
                }
            }
            catch ( Exception )
            {
                // Don't log - we'll get an infinite loop!
            }

        return e;
    }
}