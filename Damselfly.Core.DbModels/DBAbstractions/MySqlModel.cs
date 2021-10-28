using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.DbModels.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.DbModels.DBAbstractions;
using System.Threading.Tasks;

namespace Damselfly.Core.Models.DBAbstractions
{
    /// <summary>
    /// MySQL database specialisation. Pretty limited at present:
    /// - no support for migrations
    /// - no free-text searching/indexing.
    /// Work to do.
    /// </summary>
    public class MySqlModel : IDataBase
    {
        public void Configure(DbContextOptionsBuilder options)
        {
#if USE_MYSQL
            // To support MySQL, add a refernece to Pomelo.EntityFrameworkCore.MySql
            // and remove this #if section. But some of this isn't implemented yet.

            string connString = "Server=127.0.0.1;Database=damselfly;User=damsel;Password=distress123;";
            int batchSize = 20;

            options.UseMySql(connString, mysqlOptions => mysqlOptions.MaxBatchSize(batchSize));
#else
            throw new NotSupportedException("MySQL not currently supported");
#endif
        }

        public void Init(BaseDBModel db)
        {
            try
            {
                Logging.Log("Running MySql DB migrations...");

                // TODO MySQL doesn't support migrations?! - remove this big hammer
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
            catch( Exception ex )
            {
                Logging.LogWarning("Migrations failed - creating DB. Exception: {0}", ex.Message);
                db.Database.EnsureCreated();
            }
        }

        /// <summary>
        /// Polyfill to account for the fact that EF Extensions BulkIndex only
        /// works with SQLite and SQL Server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public async Task<bool> BulkInsert<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                return true;
            }

            collection.AddRange(itemsToSave);

            int ret = await db.SaveChangesAsync("BulkSave");

            return ret == itemsToSave.Count;
        }

        /// <summary>
        /// Polyfill to account for the fact that EF Extensions BulkIndex only
        /// works with SQLite and SQL Server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public async Task<bool> BulkUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                return true;
            }

            collection.UpdateRange(itemsToSave);

            int ret = await db.SaveChangesAsync("BulkSave");

            return ret == itemsToSave.Count;
        }

        /// <summary>
        /// Polyfill to account for the fact that EF Extensions BulkIndex only
        /// works with SQLite and SQL Server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToDelete"></param>
        /// <returns></returns>
        public async Task<bool> BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be deleted.");
                return true;
            }

            collection.RemoveRange(itemsToDelete);

            int ret = await db.SaveChangesAsync("BulkSave");

            return ret == itemsToDelete.Count;
        }

        /// <summary>
        /// Basic implementation that inserts or updates based on whether the key/ID is zero or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <param name="getKey"></param>
        /// <returns></returns>
        public async Task<bool> BulkInsertOrUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave, Func<T, bool> isNew) where T : class
        {
            var result = false;

            itemsToSave.ForEach(x => { if (isNew(x)) collection.Add(x); else collection.Update(x); });

            if (await db.SaveChangesAsync("BulkInsertOrUpdate") > 0)
                result = true;

            return result;
        }

        public Task<int> BatchDelete<T>(IQueryable<T> query) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<IQueryable<T>> Search<T>(string query, DbSet<T> collection) where T : class
        {
            // Full text search not supported in MySQL
            // TODO: Implement with a Like Query?
            throw new NotImplementedException();
        }


        public Task<int> BatchUpdate<T>(IQueryable<T> query, Expression<Func<T, T>> updateExpression) where T : class
        {
            throw new NotImplementedException();
        }

        public void FlushDBWriteCache(BaseDBModel db)
        {
            // No-op
        }

        public IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query, bool IncludeAITags) where T : class
        {
            // TODO: What do we do here? Maybe something with LIKE?
            throw new NotImplementedException();
        }

        public void CreateIndexes(ModelBuilder builder)
        {
        }
    }
}