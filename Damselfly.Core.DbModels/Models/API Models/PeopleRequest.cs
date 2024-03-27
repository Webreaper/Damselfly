using Damselfly.Core.Models;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class PeopleRequest
{
    public string? SearchText { get; set; }
    public Person.PersonState? State { get; set; }
    public int Start { get; set; }
    public int Count { get; set; } = 30;
}