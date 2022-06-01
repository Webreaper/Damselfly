using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

public class Folder
{
    [Key]
    public int FolderId { get; set; }
    public string Path { get; set; }

    public int ParentFolderId { get; set; }
    public DateTime? FolderScanDate { get; set; }

    public Folder Parent { get; set; }
    public ICollection<Folder> Children { get; set; }

    public virtual List<Image> Images { get; } = new List<Image>();

    public override string ToString()
    {
        return $"{Path} [{FolderId}] {FolderItem?.ToString()}";
    }

    [NotMapped]
    public string Name { get { return System.IO.Path.GetFileName(Path); } }

    [NotMapped]
    public FolderListItem FolderItem { get; set; }
}

