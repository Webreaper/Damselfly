using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace Damselfly.Core.Models;

public class UserFolderState
{
    public int FolderId { get; set; }
    public int UserId { get; set; }
    public bool Expanded { get; set; } = true; // Always default folders to 'expanded'
}