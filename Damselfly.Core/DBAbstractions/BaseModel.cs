using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Utils;
using Microsoft.Extensions.Logging;
using Damselfly.Core.Interfaces;
using EFCore.BulkExtensions;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// Base model for DB interactions. Will be passed a model class instance with an
    /// implmentation of a DB type - e.g., SqlIe or MySql, kindof like a visitor
    /// pattern.
    /// </summary>
    public abstract class BaseDBModel : DbContext
    {
        public static readonly ILoggerFactory SqlLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });

#if DEBUG
        static bool traceSQL = false;
#else
        static bool traceSQL = false;
#endif

        static bool lazyLoad = false;

        public static bool ReadOnly { get; private set; }

        // Instance of our DB type that implements the Database interface
        public static IDataBase DatabaseSpecialisation { get; private set; } = new SqlLiteModel("dummy");

        /// <summary>
        /// Bulk insert weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">Type of the object to insert</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're inserting the objects</param>
        /// <param name="itemsToSave">Objects to insert</param>
        /// <returns>True if the insert succeeded</returns>
        public  bool BulkInsert<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class
        { 
            try
            {
                db.BulkInsert(itemsToSave);
                return true;
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception bulk inserting {typeof(T)}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bulk update weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">Type of the object to update</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're updating the objects</param>
        /// <param name="itemsToSave">Objects to update</param>
        /// <returns>True if the update succeeded</returns>
        public bool BulkUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            return DatabaseSpecialisation.BulkUpdate(db, collection, itemsToSave);
        }

        /// <summary>
        /// Bulk insert weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">Type of the object to insert</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're inserting the objects</param>
        /// <param name="itemsToDelete">Objects to insert</param>
        /// <returns>True if the insert succeeded</returns>
        public bool BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            return DatabaseSpecialisation.BulkDelete(db, collection, itemsToDelete);
        }

        /// <summary>
        /// If the DB supports it, and write-caching is enabled, flush.
        /// </summary>
        public void FlushDBWriteCache()
        {
            DatabaseSpecialisation.FlushDBWriteCache( this );
        }

        /// <summary>
        /// Initialise the DB instance of the model type, using a DB specialisation. 
        /// </summary>
        /// <typeparam name="ModelType"></typeparam>
        /// <param name="dbSpecialisation">Instance of our DB specialisation</param>
        /// <param name="readOnly"></param>
        public static void InitDB<ModelType>( IDataBase dbSpecialisation, bool readOnly ) where ModelType : BaseDBModel
        {
            DatabaseSpecialisation = dbSpecialisation;
            ReadOnly = readOnly;

            using (var db = Activator.CreateInstance<ModelType>())
            {
                dbSpecialisation.Init( db );
            }
        }

        /// <summary>
        /// Basic initialisation for the DB that are generic to all DB types
        /// </summary>
        /// <param name="options"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (traceSQL)
                options.UseLoggerFactory(SqlLoggerFactory);

            if(lazyLoad )
               options.UseLazyLoadingProxies();

            DatabaseSpecialisation.Configure(options);
        }

        protected void ExecutePragma(string pragmaCommand)
        {
            try
            {
                var connection = Database.GetDbConnection();
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
        /// Called whenever we Save Changes, this is an EF helper that interrogates
        /// the change tracker and dumps a log of all of the changes in it. So we'll
        /// see something like:
        ///    Insert Image: 5
        ///    Update Lens: 3
        ///    Updae Folder: 6
        /// </summary>
        private void LogChangeSummary()
        {
            if (!Logging.Trace)
                return;

            var allChanges = ChangeTracker.Entries()
                                      .GroupBy(x => x.State.ToString() + " " + x.Entity.GetType())
                                      .Select(x => new { Change = x.Key, Number = x.Count() })
                                      .OrderBy(x => x.Change)
                                      .ToList();

            Logging.LogTrace("Changes Summary:");
            if (allChanges.Any())
            {
                foreach (var x in allChanges)
                {
                    Logging.LogTrace("  {0} {1}", x.Change, x.Number);
                }
            }
            else
                Logging.LogTrace("  No changes.");
        }

        /// <summary>
        /// Helper to clear any changes in the tracker (used if we're manually
        /// writing them, or want to reset state mid-scope, or we want to throw
        /// away changes unilaterally (e.g., if SaveChanges is called in ReadOnly
        /// mode.
        /// </summary>
        private void ClearChangeTracker()
        {
            ChangeTracker.Entries()
                .Where(e => e.Entity != null).ToList()
                .ForEach(e => e.State = EntityState.Detached);
        }

        public override int SaveChanges()
        {
            return SaveChanges("Unknown");
        }

        /// <summary>
        /// Log all writes to the DB - including the set of changes
        /// written and whether we succeeded.
        /// </summary>
        /// <param name="contextDesc"></param>
        /// <returns></returns>
        public int SaveChanges(string contextDesc)
        {
            if (ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                ClearChangeTracker();
                return 1;
            }

            try
            {
                // Write to the DB
                var watch = new Stopwatch("SaveChanges" + contextDesc);

                LogChangeSummary();

                int written = base.SaveChanges();

                Logging.LogTrace("{0} changes written to the DB", written);

                watch.Stop();

                return written;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    Logging.Log("Exception - DB WRITE FAILED. InnerException: {0}", ex.InnerException.Message);
                else
                    Logging.Log("Exception - DB WRITE FAILED: {0}", ex.Message);

                return 0;
            }
        }

        internal void FullTextTags( bool first )
        {
            DatabaseSpecialisation.FullTextTags(first);
        }

        // TODO - this is Sqlite specific and should move down into the MySqlite provider.
        public IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query) where T:class
        {
            return DatabaseSpecialisation.ImageSearch<T>(resultSet, query);
        }
    }
}