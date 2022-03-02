using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.DBAbstractions;
using Humanizer;
using Damselfly.Core.DbModels;
using Damselfly.Core.Utils.Constants;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// Our actual EF Core model describing a collection of images, lenses, 
    /// </summary>
    public class ImageContext : BaseDBModel, IDataProtectionKeyContext
    {
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<ImageMetaData> ImageMetaData { get; set; }
        public DbSet<Hash> Hashes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ImageTag> ImageTags { get; set; }
        public DbSet<ImageObject> ImageObjects { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<ImageClassification> ImageClassifications { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketEntry> BasketEntries { get; set; }
        public DbSet<Lens> Lenses { get; set; }
        public DbSet<FTSTag> FTSTags { get; set; }
        public DbSet<ExportConfig> DownloadConfigs { get; set; }
        public DbSet<ConfigSetting> ConfigSettings { get; set; }
        public DbSet<ExifOperation> KeywordOperations { get; set; }
        public DbSet<CloudTransaction> CloudTransactions { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public async Task<IQueryable<Image>> ImageSearch(string query, bool IncludeAITags)
        {
            return await base.ImageSearch<Image>(Images, query, IncludeAITags);
        }

        /// <summary>
        /// Called when the model is created by EF, this describes the key
        /// relationships between the objects
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Many to many via ImageTags
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

            // One to ImageObjects
            var io = modelBuilder.Entity<ImageObject>();

            io.HasOne(p => p.Image)
                .WithMany(p => p.ImageObjects)
                .HasForeignKey(p => p.ImageId);

            modelBuilder.Entity<Image>()
                .HasOne(img => img.Classification)
                .WithOne()
                .HasForeignKey<ImageClassification>(i => i.ClassificationId);

            modelBuilder.Entity<BasketEntry>()
                .HasOne(a => a.Image)
                .WithMany(b => b.BasketEntries);

            modelBuilder.Entity<Image>()
                .HasOne(img => img.MetaData)
                .WithOne(meta => meta.Image)
                .HasForeignKey<ImageMetaData>(i => i.ImageId);

            modelBuilder.Entity<ImageTag>().HasIndex(x => new { x.ImageId, x.TagId }).IsUnique();
            modelBuilder.Entity<Image>().HasIndex(p => new { p.FileName, p.FolderId }).IsUnique();
            modelBuilder.Entity<Image>().HasIndex(x => new { x.FolderId });
            modelBuilder.Entity<Image>().HasIndex(x => x.LastUpdated);
            modelBuilder.Entity<Image>().HasIndex(x => x.FileName);
            modelBuilder.Entity<Image>().HasIndex(x => x.FileLastModDate);
            modelBuilder.Entity<Image>().HasIndex(x => x.SortDate);
            modelBuilder.Entity<Folder>().HasIndex(x => x.FolderScanDate);
            modelBuilder.Entity<Folder>().HasIndex(x => x.Path);
            modelBuilder.Entity<Person>().HasIndex(x => x.State);
            modelBuilder.Entity<Tag>().HasIndex(x => new { x.Keyword }).IsUnique();

            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.ImageId);
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.DateTaken);
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.ThumbLastUpdated);
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.AILastUpdated);
            modelBuilder.Entity<ImageMetaData>().HasIndex(x => x.Rating);
            modelBuilder.Entity<ExifOperation>().HasIndex(x => new { x.ImageId, x.Text });
            modelBuilder.Entity<ExifOperation>().HasIndex(x => x.TimeStamp);
            modelBuilder.Entity<BasketEntry>().HasIndex(x => new { x.ImageId, x.BasketId }).IsUnique();
            modelBuilder.Entity<CloudTransaction>().HasIndex(x => new { x.Date, x.TransType });
            modelBuilder.Entity<Hash>().HasIndex(x => x.MD5ImageHash);
            modelBuilder.Entity<Hash>().HasIndex(x => new { x.PerceptualHex1, x.PerceptualHex2, x.PerceptualHex3, x.PerceptualHex4 } );
            modelBuilder.Entity<ImageClassification>().HasIndex(x => new { x.Label }).IsUnique();

            AddSpecialisationIndexes(modelBuilder);

            RoleDefinitions.OnModelCreating(modelBuilder);
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
        public int FileSizeBytes { get; set; }
        public DateTime FileCreationDate { get; set; }
        public DateTime FileLastModDate { get; set; }

        // Date used for search query orderby
        public DateTime SortDate { get; set; }

        // Damselfy state metadata
        public DateTime LastUpdated { get; set; }

        public virtual ImageMetaData MetaData { get; set; }
        public virtual Hash Hash { get; set; }

        // An image can appear in many baskets
        public virtual List<BasketEntry> BasketEntries { get; } = new List<BasketEntry>();
        // An image can have many tags
        public virtual List<ImageTag> ImageTags { get; } = new List<ImageTag>();

        // Machine learning fields
        public int? ClassificationId { get; set; }
        public virtual ImageClassification Classification { get; set; }
        public double ClassificationScore { get; set; }

        public virtual List<ImageObject> ImageObjects { get; } = new List<ImageObject>();

        public override string ToString()
        {
            return $"{FileName} [{ImageId}]";
        }

        [NotMapped]
        public string FullPath {  get { return Path.Combine(Folder.Path, FileName);  } }

        [NotMapped]
        public string RawImageUrl {  get { return $"/rawimage/{ImageId}"; } }
        [NotMapped]
        public string DownloadImageUrl { get { return $"/dlimage/{ImageId}"; } }

        public void FlagForMetadataUpdate() { this.LastUpdated = DateTime.UtcNow; }
    }

    /// <summary>
    /// Metadata associated with an image. Also, an optional lens and camera. 
    /// </summary>
    public class ImageMetaData
    {
        [Key]
        public int MetaDataId { get; set; }

        [Required]
        public virtual Image Image { get; set; }
        public int ImageId { get; set; }

        public DateTime DateTaken { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Rating { get; set; } // 1-5, stars
        public string Caption { get; set; }
        public string Copyright { get; set; }
        public string Credit { get; set; }
        public string Description { get; set; }
        public string ISO { get; set; }
        public string FNum { get; set; }
        public string Exposure { get; set; }
        public bool FlashFired { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int? CameraId { get; set; }
        public virtual Camera Camera { get; set; }

        public int? LensId { get; set; }
        public virtual Lens Lens { get; set; }

        public string DominantColor { get; set; }
        public string AverageColor { get; set; }

        // The date that this metadata was read from the image
        // If this is older than Image.LastUpdated, the image
        // will be re-indexed
        public DateTime LastUpdated { get; set; }

        // Date the thumbs were last created. If this is null
        // the thumbs will be regenerated
        public DateTime? ThumbLastUpdated { get; set; }

        // Date we last performed face/object/image recognition
        // If this is null, AI will be reprocessed
        public DateTime? AILastUpdated { get; set; }

        /// <summary>
        /// Temporary workaround for the fact that EFCore.BulkExtensions doesn't support joined
        /// updates in its BatchUpdateAsync method. So we just execute the raw SQL directly.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="folderId"></param>
        /// <param name="updateField"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static async Task<int> UpdateFields(ImageContext db, Folder folder, string updateField, string newValue)
        {
            string sql = $@"UPDATE ImageMetaData SET {updateField} = {newValue} FROM (SELECT i.ImageId, i.FolderId FROM Images i where i.FolderId = {folder.FolderId}) AS imgs WHERE imgs.ImageID = ImageMetaData.ImageID";

            try
            {
                return await db.Database.ExecuteSqlRawAsync(sql);
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception updating Metadata Field {updateField}: {ex.Message}");
                return 0;
            }
        }
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
        public enum TagTypes
        {
            IPTC = 0,
            Classification = 1
        };

        [Key]
        public int TagId { get; set; }
        [Required]
        public string Keyword { get; set; }

        public TagTypes TagType { get; set; }
        public bool Favourite { get; set; }

        public DateTime TimeStamp { get; private set; } = DateTime.UtcNow;

        public virtual List<ImageTag> ImageTags { get; } = new List<ImageTag>();
        public virtual List<ImageObject> ImageObjects { get; } = new List<ImageObject>();

        public override string ToString()
        {
            return $"{TagType}: {Keyword} [{TagId}]";
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
    /// An image classification detected via ML
    /// </summary>
    public class ImageClassification
    {
        [Key]
        public int ClassificationId { get; set; }

        public string Label { get; set; }

        public override string ToString()
        {
            return $"{Label} [{ClassificationId}]";
        }
    }

    /// <summary>
    /// One image can have a number of objects each with a name.
    /// </summary>
    public class ImageObject
    {
        public enum ObjectTypes
        {
            Object = 0,
            Face = 1
        };

        public enum RecognitionType
        {
            Manual = 0,
            Emgu = 1,
            Accord = 2,
            Azure = 3,
            MLNetObject = 4,
            ExternalApp = 5
        };

        [Key]
        public int ImageObjectId { get; set; }

        [Required]
        public int ImageId { get; set; }
        public virtual Image Image { get; set; }

        [Required]
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }

        public string Type { get; set; } = ObjectTypes.Object.ToString();
        public RecognitionType RecogntionSource { get; set; }
        public double Score { get; set; }
        public int RectX { get; set; }
        public int RectY { get; set; }
        public int RectWidth { get; set; }
        public int RectHeight { get; set; }

        public int? PersonId { get; set; }
        public virtual Person Person { get; set; }

        public override string ToString()
        {
            return GetTagName();
        }

        public bool IsFace {  get { return Type == ObjectTypes.Face.ToString();  } }

        public string GetTagName( bool includeScore = false )
        {
            string ret = "Unidentified Object";

            if( IsFace )
            {
                if (Person != null && Person.Name != "Unknown")
                {
                    return $"{Person.Name.Transform(To.TitleCase)}";
                }
                else
                    ret = "Unidentified face";
            }
            else if (Type == ObjectTypes.Object.ToString() && Tag != null)
            {
                ret = $"{Tag.Keyword.Transform(To.SentenceCase)}";
            }

            if ( includeScore && Score > 0 )
            {
                ret += $" ({Score:P0})";
            }

            return ret;
        }
    }

    /// <summary>
    /// A person
    /// </summary>
    public class Person
    {
        public enum PersonState
        {
            Unknown = 0,
            Identified = 1
        };

        [Key]
        public int PersonId { get; set; }

        [Required]
        public string Name { get; set; } = "Unknown";

        public PersonState State { get; set; } = PersonState.Unknown;
        public string AzurePersonId { get; set; }

        public override string ToString()
        {
            return $"{PersonId}=>{Name} [{State}, AzureID: {AzurePersonId}]";
        }
    }

    /// <summary>
    /// Transaction Count for Cloud services - to keep approximate track of usage
    /// </summary>
    public class CloudTransaction
    {
        public enum TransactionType
        {
            Unknown = 0,
            AzureFace = 1
        };

        [Key]
        public int CloudTransactionId { get; set; }

        public TransactionType TransType { get; set; }
        public DateTime Date { get; set; }
        public int TransCount { get; set; }

        public override string ToString()
        {
            return $"{Date}: {TransCount}";
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

        public int? UserId { get; set; }
        public virtual AppIdentityUser User { get; set; }

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
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

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
            Caption = 1,
            Face = 2,
            Description = 3,
            Rating = 4,
            Copyright = 5
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

        public int? UserId { get; set; }
        public virtual AppIdentityUser User { get; set; }
    }

    /// <summary>
    /// A search query, with a set of parameters. By saving these to the DB, we can have 'quick
    /// search' type functionality (or 'favourite' searches).
    /// </summary>
    public class SearchQuery
    {
        public enum SortOrderType
        {
            Ascending,
            Descending
        };

        public enum GroupingType
        {
            None,
            Folder,
            Date
        };

        public enum FaceSearchType
        {
            Faces,
            NoFaces,
            IdentifiedFaces,
            UnidentifiedFaces
        }

        public enum OrientationType
        {
            Landscape,
            Portrait
        }

        public string SearchText { get; set; } = string.Empty;
        public bool TagsOnly { get; set; } = false;
        public bool IncludeAITags { get; set; } = true;
        public bool UntaggedImages { get; set; } = false;
        public int? MaxSizeKB { get; set; } = null;
        public int? MinSizeKB { get; set; } = null;
        public int? CameraId { get; set; } = null;
        public int? LensId { get; set; } = null;
        public int? Month { get; set; } = null;
        public int? MinRating { get; set; } = null;
        public Image SimilarTo { get; set; } = null;
        public Folder Folder { get; set; } = null;
        public Tag Tag { get; set; } = null;
        public Person Person { get; set; } = null;
        public DateTime? MaxDate { get; set; } = null;
        public DateTime? MinDate { get; set; } = null;
        public FaceSearchType? FaceSearch { get; set; } = null;
        public OrientationType? Orientation { get; set; } = null;

        public GroupingType Grouping { get; set; } = GroupingType.None;
        public SortOrderType SortOrder { get; set; } = SortOrderType.Descending;

        public override string ToString()
        {
            return $"Filter: T={SearchText}, F={Folder?.FolderId}, Max={MaxDate}, Min={MinDate}, Max={MaxSizeKB}KB, Rating={MinRating}, Min={MinSizeKB}KB, Tags={TagsOnly}, Grouping={Grouping}, Sort={SortOrder}, Face={FaceSearch}, Person={Person?.Name}, SimilarTo={SimilarTo?.ImageId}";
        }
    }

    /// <summary>
    /// Config associated with an export or download
    /// </summary>
    public class ExportConfig : IExportSettings
    {
        public int ExportConfigId { get; set; }
        public string Name { get; set; }
        public ExportType Type { get; set; } = ExportType.Download;
        public ExportSize Size { get; set; } = ExportSize.FullRes;
        public bool KeepFolders { get; set; }
        public string WatermarkText { get; set; }

        public int MaxImageSize => MaxSize(Size);
        public string SizeDesc() => SizeDesc(Size);

        public static string SizeDesc(ExportSize size) => $"{size.Humanize()}" + (size == ExportSize.FullRes ? "" : $" (max {MaxSize(size)}x{MaxSize(size)})");
        public static int MaxSize( ExportSize size ) =>
            size switch
            {
                ExportSize.ExtraLarge => 1920,
                ExportSize.Large => 1600,
                ExportSize.Medium => 1024,
                ExportSize.Small => 800,
                _ => int.MaxValue,
            };
    }

    /// <summary>
    /// Store hashes for an image.
    /// </summary>
    public class Hash
    {
        [Key]
        public int HashId { get; set; }

        [Required]
        public virtual Image Image { get; set; }
        public int ImageId { get; set; }

        // The MD5 image hash. 
        public string MD5ImageHash { get; set; }

        // Four slices of the perceptual hash (split to allow
        // us to precalculate matches so we only have to calc
        // hamming distance on a subset of images.
        public string PerceptualHex1 { get; set; }
        public string PerceptualHex2 { get; set; }
        public string PerceptualHex3 { get; set; }
        public string PerceptualHex4 { get; set; }

        [NotMapped]
        public ulong PerceptualHashValue
        {
            get { return (ulong)Convert.ToInt64(PerceptualHash, 16); }
        }

        public double SimilarityTo( Hash other )
        {
            double similarity = HashExtensions.Similarity(PerceptualHashValue, other.PerceptualHashValue);

            Logging.LogVerbose($"Hash similarity {PerceptualHash} vs {other.PerceptualHash} = {similarity:P1} ({PerceptualHashValue} v {other.PerceptualHashValue})");

            return similarity;
        }

        /// <summary>
        /// Property accessor to set and get the sliced perceptual hash via a single Hex has string.
        /// </summary>
        [NotMapped]
        public string PerceptualHash
        {
            get
            {
                return PerceptualHex1 + PerceptualHex2 + PerceptualHex3 + PerceptualHex4;
            }

            set
            {
                var fullHex = value.PadLeft(16, '0');

                var chunks = fullHex.Chunk(4).Select(x => new string(x)).ToArray();

                PerceptualHex1 = chunks[0];
                PerceptualHex2 = chunks[1];
                PerceptualHex3 = chunks[2];
                PerceptualHex4 = chunks[3];
            }
        }
    }

    /// <summary>
    /// Config associated with an export or download
    /// </summary>
    public class ConfigSetting
    {
        public int ConfigSettingId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public int? UserId { get; set; }
        public virtual AppIdentityUser User { get; set; }

        public override string ToString()
        {
            return $"Setting: {Name} = {Value}";
        }
    }
}