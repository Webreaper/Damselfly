using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class ImageMoveRequest
{
    public Folder Destination { get; set; }
    public ICollection<int> ImageIDs { get; set; }
    public bool Move { get; set; }
}

