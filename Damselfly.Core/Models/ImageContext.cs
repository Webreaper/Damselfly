using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Services;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// Our actual EF Core model describing a collection of images, lenses, 
    /// </summary>
    public class ImageContext : BaseDBModel
    {
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<ImageMetaData> ImageMetaData { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ImageTag> ImageTags { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketEntry> BasketEntries { get; set; }
        public DbSet<Lens> Lenses { get; set; }
        public DbSet<FTSTag> FTSTags { get; set; }
        public DbSet<ExportConfig> DownloadConfigs { get; set; }
        public DbSet<ConfigSetting> ConfigSettings { get; set; }
        public DbSet<ExifOperation> KeywordOperations { get; set; }

        public IQueryable<Image> ImageSearch(string query)
        {
            return base.ImageSearch<Image>(Images, query);
        }

        /// <summary>
        /// Called when the model is created by EF, this describes the key
        /// relationships between the objects
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var it = modelBuilder.Entity<ImageTag>();
            it.HasKey(x => new { x.ImageId, x.TagId });

            it.HasOne(p => p.Image)
                .WithMany(p => p.ImageTags)
                .HasForeignKey(p => p.ImageId)
                .OnDelete(DeleteBehavior.Cascade);

            it.HasOne(p => p.Tag)
                .WithMany(p => p.ImageTags)
                .HasForeignKey(p => p.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BasketEntry>()
                .HasOne(a => a.Image)
                .WithOne(b => b.BasketEntry)
                .HasForeignKey<BasketEntry>(e => e.ImageId);

            modelBuilder.Entity<ImageTag>().HasIndex(x => new { x.ImageId, x.TagId }).IsUnique();
            modelBuilder.Entity<Image>().HasIndex(x => new { x.FolderId });
            modelBuilder.Entity<Image>().HasIndex(x => x.LastUpdated);
            modelBuilder.Entity<Image>().HasIndex(x => x.FileLastModDate);
            modelBuilder.Entity<Folder>().HasIndex(x => x.FolderScanDate);
            modelBuilder.Entity<Folder>().HasIndex(x => x.Path);
            modelBuilder.Entity<Tag>().HasIndex(x => new { x.Keyword }).IsUnique();
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.ImageId);
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.DateTaken);
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.ThumbLastUpdated);
            modelBuilder.Entity<ExifOperation>().HasIndex(x => new { x.ImageId, x.Text });
            modelBuilder.Entity<ExifOperation>().HasIndex(x => x.TimeStamp);
        }
    }

    public class Folder
    {
        [Key]
        public int FolderId { get; set; }
        public string Path { get; set; }

        public int ParentFolderId { get; set; }
        public DateTime? FolderScanDate { get; set; }

        public virtual List<Image> Images { get; } = new List<Image>();

        public override string ToString()
        {
            return $"{Path} [{FolderId}]";
        }

        [NotMapped]
        public string Name { get { return System.IO.Path.GetFileName(Path); } }
    }

    /// <summary>
    /// An image, or photograph file on disk. Has a folder associated
    /// with it. There's a BasketEntry which, if it exists, indicates
    /// the picture is selected.
    /// It also has a many-to-many relationship with IPTC keyword tags; so
    /// a tag can apply to many images, and an image can have many tags.
    /// </summary>
    public class Image
    {
        [Key]
        public int ImageId { get; set; }

        public int FolderId { get; set; }
        public virtual Folder Folder { get; set; }

        // Image File metadata
        public string FileName { get; set; }
        public ulong FileSizeBytes { get; set; }
        public DateTime FileCreationDate { get; set; }
        public DateTime FileLastModDate { get; set; }

        // Damselfy state metadata
        public DateTime LastUpdated { get; set; }

        public virtual BasketEntry BasketEntry { get; set; }
        public virtual ImageMetaData MetaData { get; set; }
        public virtual IList<ImageTag> ImageTags { get; } = new List<ImageTag>();

        public override string ToString()
        {
            return $"{FileName} [{ImageId}]";
        }

        [NotMapped]
        public string FullPath {  get { return Path.Combine(Folder.Path, FileName);  } }

        [NotMapped]
        public string RawImageUrl {  get { return $"/rawimage/{ImageId}"; } }
    }

    /// <summary>
    /// Metadata associated with an image. Also, an optional lens and camera. 
    /// </summary>
    public class ImageMetaData
    {
        public enum Stars
        {
            One,
            Two,
            Three,
            Four,
            Five
        };


        [Key]
        public int MetaDataId { get; set; }

        [Required]
        public virtual Image Image { get; set; }
        public int ImageId { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public Stars Rating { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string ISO { get; set; }
        public string FNum { get; set; }
        public string Exposure { get; set; }
        public bool FlashFired { get; set; }
        public DateTime DateTaken { get; set; }

        public int? CameraId { get; set; }
        public virtual Camera Camera { get; set; }

        public int? LensId { get; set; }
        public virtual Lens Lens { get; set; }

        // The date that this metadata was read from the image
        public DateTime LastUpdated { get; set; }
        public DateTime? ThumbLastUpdated { get; set; }
    }

    /// <summary>
    /// A camera, which is associated with an image
    /// </summary>
    public class Camera
    {
        [Key]
        public int CameraId { get; set; }
        public string Model { get; set; }
        public string Make { get; set; }
        public string Serial { get; set; }
    }

    /// <summary>
    /// A lens, also associated with an image
    /// </summary>
    public class Lens
    {
        [Key]
        public int LensId { get; set; }
        public string Model { get; set; }
        public string Make { get; set; }
        public string Serial { get; set; }
    }

    /// <summary>
    /// A keyword tag. Primarily IPTC tags, these are sets of
    /// keywords that are used to associate metadata with an
    /// image.
    /// </summary>
    public class Tag
    {
        [Key]
        public int TagId { get; set; }
        [Required]
        public string Keyword { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; private set; } = DateTime.UtcNow;

        public virtual IList<ImageTag> ImageTags { get; } = new List<ImageTag>();

        public override string ToString()
        {
            return $"{Keyword} [{TagId}]";
        }

        public override int GetHashCode()
        {
            return Keyword.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Tag objTag = obj as Tag;

            if( objTag != null )
                return objTag.Keyword.Equals(this.Keyword, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }

    /// <summary>
    /// A Free-Text Search Tag. Separate from 'Tag' because EF doesn't currently support
    /// free-text search so we've had to roll our own a bit. This is used for results
    /// deserialization.
    /// </summary>
    public class FTSTag
    {
        [Key]
        public int FTSTagId { get; set; }
        public string Keyword { get; set; }
    }

    /// <summary>
    /// Many-to-many relationship table joining images and tags.
    /// </summary>
    public class ImageTag
    {
        [Key]
        public int ImageId { get; set; }
        public virtual Image Image { get; set; }

        [Key]
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }

        public override string ToString()
        {
            return $"{Image.FileName}=>{Tag.Keyword} [{ImageId}, {TagId}]";
        }

        public override bool Equals(object obj)
        {
            ImageTag objTag = obj as ImageTag;

            if (objTag != null)
                return objTag.ImageId.Equals(this.ImageId) && objTag.TagId.Equals( this.TagId );

            return false;
        }

        public override int GetHashCode()
        {
            return ImageId.GetHashCode() + '_' + TagId.GetHashCode();
        }
    }

    /// <summary>
    /// A saved basket of images - witha a description and saved date.
    /// </summary>
    public class Basket
    {
        [Key]
        public int BasketId { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public string Name { get; set; }

        public virtual List<BasketEntry> BasketEntries { get; } = new List<BasketEntry>();
    }

    /// <summary>
    /// A basket entry represents a persistent selection of an image. So if a basket entry
    /// exists, the image is in the basket. We can then perform operations on those entries
    /// (export, etc).
    /// </summary>
    public class BasketEntry
    {
        [Key]
        public int BasketEntryId { get; set; }
        public DateTime DateAdded { get; set; }

        [Required]
        public virtual Image Image { get; set; }
        public int ImageId { get; set; }

        [Required]
        public virtual Basket Basket { get; set; }
        public int BasketId { get; set; }

        public override string ToString()
        {
            return $"{Image.FileName} [{Image.ImageId} - added {DateAdded}]";
        }
    }

    /// <summary>
    /// Represents a pending operation to add or remove a keyword on an image
    /// </summary>
    public class ExifOperation
    {
        public enum OperationType
        {
            Add,
            Remove
        };

        public enum ExifType
        {
            Keyword = 0,
            Caption = 1
        };

        public enum FileWriteState
        {
            Pending = 0,
            Written = 1,
            Discarded = 9999,
            Failed = -1
        };

        [Key]
        public int ExifOperationId { get; set; }

        [Required]
        public virtual Image Image { get; set; }
        public int ImageId { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        public ExifType Type { get; set; }

        [Required]
        public OperationType Operation { get; set; } = OperationType.Add;

        public DateTime TimeStamp { get; internal set; }
        public FileWriteState State { get; set; } = FileWriteState.Pending;
    }

    /// <summary>
    /// A search query, with a set of parameters. By saving these to the DB, we can have 'quick
    /// search' type functionality (or 'favourite' searches).
    /// </summary>
    public class SearchQuery
    {
        public string SearchText { get; set; }
        public DateTime MaxDate { get; set; } = DateTime.MaxValue;
        public DateTime MinDate { get; set; } = DateTime.MinValue;
        public ulong MaxSizeKB { get; set; } = ulong.MaxValue;
        public ulong MinSizeKB { get; set; } = ulong.MinValue;
        public Folder Folder { get; set; } = null;
        public bool TagsOnly { get; set; } = false;

        public override string ToString()
        {
            return $"Filter: T={SearchText}, F={Folder?.FolderId}, Max={MaxDate}, Min={MinDate}, Max={MaxSizeKB}KB, Min={MinSizeKB}KB, Tags={TagsOnly}";
        }
    }

    /// <summary>
    /// Config associated with an export or download
    /// </summary>
    public class ExportConfig
    {
        public int ExportConfigId { get; set; }
        public string Name { get; set; }
        public ExportType Type { get; set; } = ExportType.Download;
        public ExportSize Size { get; set; } = ExportSize.FullRes;
        public string WatermarkText { get; set; }
    }

    /// <summary>
    /// Config associated with an export or download
    /// </summary>
    public class ConfigSetting
    {
        public int ConfigSettingId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return $"Setting: {Name} = {Value}";
        }
    }
}