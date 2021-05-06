using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// SQLite database specialisation. Assumes a Database path is set
    /// at construction.
    /// </summary>
    public class PostgresModel : IDataBase
    {
        /// <summary>
        /// The SQLite-specific initialisation.
        /// </summary>
        /// <param name="options"></param>
        public void Configure(DbContextOptionsBuilder options)
        {
            string dataSource = @"Host=localhost;Database=entitycore;Username=postgres;Password=mypassword";
            options.UseNpgsql(dataSource);
        }

        /// <summary>
        /// SQLite pragma execution.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="pragmaCommand"></param>
        private void ExecutePragma(BaseDBModel db, string pragmaCommand)
        {
            try
            {
                var connection = db.Database.GetDbConnection();
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = pragmaCommand;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logging.LogWarning($"Unable to execute pragma command {pragmaCommand}: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable SQLite performance improvements
        /// </summary>
        /// <param name="db"></param>
        private void IncreasePerformance(BaseDBModel db)
        {
            // Enable journal mode - this will also improve
            // concurrent acces
            ExecutePragma(db, "PRAGMA journal_mode=WAL;");
            // Turn off Synchronous mode. This means that writes aren't
            // sync'd to disk every single time. 
            ExecutePragma(db, "PRAGMA synchronous=OFF;");
            // Increate the cache page size TODO: check this value
            ExecutePragma(db, "PRAGMA cache_size=10000;");
            // Use a shared cache - good for multi-threaded access
            ExecutePragma(db, "PRAGMA cache=shared;");
            // Store temporary tables in memory
            ExecutePragma(db, "PRAGMA temp_store=MEMORY;");
        }

        public void FlushDBWriteCache(BaseDBModel db)
        {
            ExecutePragma(db, "PRAGMA schema.wal_checkpoint;");
        }

        /// <summary>
        /// SQLite specific initialisation. Run the migrations, and 
        /// always run a VACUUM to optimise the DB at startup.
        /// </summary>
        /// <param name="db"></param>
        public void Init( BaseDBModel db )
        {
            try
            {
                Logging.Log("Running Sqlite DB migrations...");
                db.Database.Migrate();
            }
            catch( Exception ex )
            {
                Logging.LogWarning("Migrations failed - creating DB. Exception: {0}", ex.Message);
                db.Database.EnsureCreated();
            }

            // Always rebuild the FTS table at startup
            FullTextTags(true);

            Logging.Log("Running Sqlite DB optimisation...");
            db.Database.ExecuteSqlRaw("VACUUM;");
            Logging.Log("DB optimisation complete.");

            IncreasePerformance(db);
        }

        /// <summary>
        /// SQLite bulk insert uses EF Extensions BulkIndex.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public bool BulkInsert<T>(BaseDBModel db, DbSet <T> collection, List<T> itemsToSave ) where T : class
        {
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be inserted.");
                return true;
            }

            bool success = false;
            try
            {
                db.BulkInsert(itemsToSave);
                success = true;
            }
            catch (Exception ex)
            {
                Logging.LogError("Exception during bulk insert: {0}", ex.Message);
            }

            return success;
        }

        /// <summary>
        /// SQLite bulk update uses EF Extensions BulkUpdate.
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

            bool success = false;
            try
            {
                db.BulkUpdate(itemsToSave);
                success = true;
            }
            catch (Exception ex)
            {
                Logging.LogError("Exception during bulk update: {0}", ex.Message);
            }

            return success;
        }

        /// <summary>
        /// SQLite bulk delete uses EF Extensions BulkDelete.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToDelete"></param>
        /// <returns></returns>
        public bool BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be deleted.");
                return true;
            }

            bool success = false;
            try
            {
                db.BulkDelete(itemsToDelete);
                success = true;
            }
            catch (Exception ex)
            {
                Logging.LogError("Exception during bulk delete: {0}", ex.Message);
            }

            return success;
        }

        // TODO - this is Sqlite specific and should move down into the MySqlite provider.
        public IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query) where T : class
        {
            // Convert the string from a set of terms to quote and add * so they're all exact partial matches
            // TODO: How do we handle suffix matches - i.e., contains. SQLite FTS doesn't support that. :(
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => $"\"{x}\"*");

            var subQueries = new List<String>();

            var sql = "SELECT i.* from Images i";
            int i = 1;
            foreach( var term in terms )
            {
                // Subquery to produce the distinct set of images that match the term
                var subQuery = $" join (select distinct it.ImageId from FTSKeywords fts join ImageTags it on it.tagId = fts.TagId where fts.Keyword MATCH ('{term}')) term{i} on term{i}.ImageID = i.ImageId";
                sql += subQuery;
                i++;
            }

            return resultSet.FromSqlRaw(sql);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageKeywords">A dictionary of images to keywords. Each image
        /// can have an array of multiple keywords.</param>
        public void FullTextTags( bool first )
        {
            int retry = 0;
            while (retry++ < 5)
            {
                using (var db = new ImageContext())
                {
                    try
                    {
                        // TODO - get the tablenames from the type
                        string createSql = "CREATE VIRTUAL TABLE IF NOT EXISTS \"FTSKeywords\" USING fts5( \"TagId\", \"Keyword\")";
                        string deleteSql = "DELETE FROM FTSKeywords";
                        string insertSql = "INSERT INTO FTSKeywords SELECT TagId, Keyword FROM Tags";

                        if (!first)
                            createSql = string.Empty;

                        string theSql = $"BEGIN TRANSACTION; {createSql}; {deleteSql}; {insertSql}; COMMIT;";

                        Logging.LogVerbose($"FreeText SQL: {theSql}");

                        // TODO: Can we do this less often?
                        db.Database.ExecuteSqlRaw(theSql);

                        Logging.LogVerbose($"Free text table rebuilt.");

                        // Skip retries and return
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError("'Exception adding freetext for images: {0} (retry {1})", ex.Message, retry);
                    }
                }

                Logging.LogWarning("Error writing freetext. Retrying...");
            }
        }
    }
}