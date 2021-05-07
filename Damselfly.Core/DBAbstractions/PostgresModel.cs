using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// Postgres database specialisation. Assumes a Database path is set
    /// at construction.
    /// </summary>
    public class PostgresModel : IDataBase
    {
        /// <summary>
        /// The Postgres-specific initialisation.
        /// </summary>
        /// <param name="options"></param>
        public void Configure(DbContextOptionsBuilder options)
        {
            const string user = "markotway";
            const string pw = "password";

            string dataSource = $"User ID={user};Password={pw};Host=localhost;Port=5432;Database=Damselfly;Pooling=true;";
            options.UseNpgsql(dataSource, b => b.MigrationsAssembly("Damselfly.Migrations.Postgres"));
        }

        /// <summary>
        /// Enable Postgres performance improvements
        /// </summary>
        /// <param name="db"></param>
        private void IncreasePerformance(BaseDBModel db)
        {
            // Nothing yet for postgres
        }

        public void FlushDBWriteCache(BaseDBModel db)
        {
            // 
        }

        /// <summary>
        /// Postgres specific initialisation. Run the migrations, and 
        /// always run a VACUUM to optimise the DB at startup.
        /// </summary>
        /// <param name="db"></param>
        public void Init( BaseDBModel db )
        {
            try
            {
                Logging.Log("Running Postgres DB migrations...");
                db.Database.Migrate();
            }
            catch( Exception ex )
            {
                Logging.LogWarning("Migrations failed - creating DB. Exception: {0}", ex.Message);
                db.Database.EnsureCreated();
            }

            // Always rebuild the FTS table at startup
            FullTextTags(true);

            IncreasePerformance(db);
        }

        /// <summary>
        /// Postgres bulk insert uses EF Extensions BulkIndex.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public bool BulkInsert<T>(BaseDBModel db, DbSet <T> collection, List<T> itemsToSave ) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                return true;
            }

            collection.AddRange(itemsToSave);

            int ret = db.SaveChanges("BulkSave");

            return ret == itemsToSave.Count;
        }

        /// <summary>
        /// Postgres bulk update uses EF Extensions BulkUpdate.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public bool BulkUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                return true;
            }

            collection.UpdateRange(itemsToSave);

            int ret = db.SaveChanges("BulkSave");

            return ret == itemsToSave.Count;
        }

        /// <summary>
        /// Postgres bulk delete uses EF Extensions BulkDelete.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToDelete"></param>
        /// <returns></returns>
        public bool BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be deleted.");
                return true;
            }

            collection.RemoveRange(itemsToDelete);

            int ret = db.SaveChanges("BulkSave");

            return ret == itemsToDelete.Count;
        }

        public IQueryable<T> Search<T>(string query, DbSet<T> collection) where T : class
        {
            // Figure out FTS in Postgres
            // TODO: Implement with a Like Query?
            throw new NotImplementedException();
        }

        public IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query) where T : class
        {
            // Figure out FTS in postgres
            // TODO: Implement with a Like Query?
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageKeywords">A dictionary of images to keywords. Each image
        /// can have an array of multiple keywords.</param>
        public void FullTextTags( bool first )
        {
            // TODO: What do we do here? Maybe something with LIKE?
            throw new NotImplementedException();
        }
    }
}