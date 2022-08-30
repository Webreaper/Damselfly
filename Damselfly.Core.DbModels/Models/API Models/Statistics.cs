using System;
namespace Damselfly.Core.DbModels.Models;

public class Statistics
{
    public int TotalImages { get; set; }
    public int TotalFolders { get; set; }
    public int TotalTags { get; set; }
    public int PendingThumbs { get; set; }
    public int PendingImages { get; set; }
    public int PendingAIScans { get; set; }
    public int PendingKeywordOps { get; set; }
    public int PendingKeywordImages { get; set; }
    public long TotalImagesSizeBytes { get; set; }
    public string AzureMonthlyTransactions { get; set; }
    public int ObjectsRecognised { get; set; }
    public int PeopleFound { get; set; }
    public int PeopleIdentified { get; set; }
    public string OperatingSystem { get; set; }
}

