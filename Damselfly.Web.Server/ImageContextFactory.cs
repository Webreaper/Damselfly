using Damselfly.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Damselfly.Web.Server;

public class ImageContextFactory : IDesignTimeDbContextFactory<ImageContext>
{
    public ImageContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ImageContext>();

        optionsBuilder.UseSqlite(
            b =>
            {
                b.MigrationsAssembly( "Damselfly.Migrations.Sqlite" );
                b.UseQuerySplittingBehavior( QuerySplittingBehavior.SingleQuery );
            } );
        return new ImageContext( optionsBuilder.Options );
    }
}