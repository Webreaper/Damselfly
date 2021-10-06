using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class RemoveHashColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy data
            const string migrateSql = @"INSERT INTO Hashes (ImageId, MD5ImageHash)
                                SELECT i.ImageId, i.Hash FROM ImageMetaData i
                                WHERE i.ImageID not in
                                    (SELECT imageid FROM hashes);";
            migrationBuilder.Sql(migrateSql);

            const string clearSql = @"UPDATE ImageMetadata set Hash = null;";
            migrationBuilder.Sql(clearSql);

            /*
               For some reason, dropping this column causes a FOREIGN Key constraint, so leave it for now.

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "ImageMetaData");
            */
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
             
            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "ImageMetaData",
                type: "TEXT",
                nullable: true);
            */
        }
    }
}
