using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace Damselfly.Core.Models;

public class Folder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int FolderId { get; set; }

    public string? Path { get; set; }

    public int? ParentId { get; set; }
    public virtual Folder? Parent { get; set; }

    public DateTime? FolderScanDate { get; set; }

    [JsonIgnore] // This JsonIgnore prevents circular references when serializing the Image entity
    public ICollection<Folder> Children { get; } = new List<Folder>();

    [JsonIgnore] // This JsonIgnore prevents circular references when serializing the Image entity
    public virtual List<Image> Images { get; } = new();

    [NotMapped] 
    public string Name => System.IO.Path.GetFileName(Path);

    [NotMapped] 
    public FolderMetadata MetaData { get; set; }

    [NotMapped]
    public IEnumerable<Folder> Subfolders
    {
        get
        {
            var thisId = new[] { this };

            if ( Children != null )
                return Children.SelectMany(x => x.Subfolders).Concat(thisId);

            return thisId;
        }
    }

    [NotMapped]
    public IEnumerable<Folder> ParentFolders
    {
        get
        {
            if ( Parent != null )
                return Parent.ParentFolders.Concat(new[] { Parent });

            return Enumerable.Empty<Folder>();
        }
    }

    [NotMapped] 
    public bool HasSubFolders => Children != null && Children.Any();

    public override string ToString()
    {
        return $"{Path} [{FolderId}] {MetaData?.ToString()}";
    }
}