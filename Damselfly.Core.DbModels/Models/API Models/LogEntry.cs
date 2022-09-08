using System;
using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class LogEntry
{
    public string Date { get; set; }
    public string Level { get; set; }
    public string Entry { get; set; }
    public string Thread { get; set; }
}

public class LogEntryResponse
{
    public string LogFileName { get; set; }
    public ICollection<LogEntry> LogEntries { get; set; }
}
