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
using Damselfly.Core.Utils.Images;

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

            modelBuilder.Entity<Folder>()
                .HasMany(x => x.Children)
                .WithOne(x => x.Parent)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

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


    
    
  
}