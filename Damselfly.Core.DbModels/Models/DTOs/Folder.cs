using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Damselfly.Core.Models;

public class Folder
{
    [Key]
    public int FolderId { get; set; }
    public string Path { get; set; }

    public int? ParentId { get; set; }
    public virtual Folder? Parent { get; set; }

    public DateTime? FolderScanDate { get; set; }
    [JsonIgnore]
    public ICollection<Folder> Children { get; set; }

    [JsonIgnore]
    public virtual List<Image> Images { get; } = new List<Image>();

    public override string ToString()
    {
        return $"{Path} [{FolderId}] {MetaData?.ToString()}";
    }

    [NotMapped]
    public string Name { get { return System.IO.Path.GetFileName(Path); } }

    [NotMapped]
    public FolderMetadata MetaData { get; set; }

    [NotMapped]
    public IEnumerable<Folder> Subfolders
    {
        get
        {
            var thisId = new[] { this };

            if ( Children != null )
                return Children.SelectMany(x => x.Subfolders).Concat( thisId);

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
    public bool HasSubFolders {  get { return Children != null && Children.Any(); } }
}

