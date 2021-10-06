using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class RemoveHashColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Logging.Log("Migrating hashes to new table...");
            // Copy data
            const string sql = @"INSERT INTO Hashes (ImageId, MD5ImageHash)
                                SELECT i.ImageId, i.Hash FROM ImageMetaData i
                                WHERE i.ImageID not in
                                    (SELECT imageid FROM hashes);";
            migrationBuilder.Sql(sql);

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "ImageMetaData");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Logging.Log("Calling migration Down()...");
            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "ImageMetaData",
                type: "TEXT",
                nullable: true);
        }
    }
}
