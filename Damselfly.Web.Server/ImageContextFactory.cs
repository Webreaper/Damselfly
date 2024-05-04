using Damselfly.Core.Database;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Damselfly.Web.Server;

//public class ImageContextFactory : IDesignTimeDbContextFactory<ImageContext>
//{
//    //public ImageContext CreateDbContext(string[] args)
//    //{
//    //    var optionsBuilder = new DbContextOptionsBuilder<ImageContext>();

//    //    optionsBuilder.UseNpgsql(
//    //        b => {
//    //            b.MigrationsAssembly( "Damselfly.Migrations.Postgres" );
//    //            b.UseQuerySplittingBehavior( QuerySplittingBehavior.SingleQuery );
//    //        } );
//    //    return new ImageContext( optionsBuilder.Options );
//    //}
//}

