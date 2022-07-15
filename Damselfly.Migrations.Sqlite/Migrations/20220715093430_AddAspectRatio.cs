using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAspectRatio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AspectRatio",
                table: "ImageMetaData",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_AspectRatio",
                table: "ImageMetaData",
                column: "AspectRatio");

            // Populate the Aspect Ratio column
            const string sql = @"update ImageMetaData set AspectRatio = 1 where width = 0 or height = 0;";
            migrationBuilder.Sql(sql);

            const string sql2 = @"update ImageMetaData set AspectRatio = cast(width as float) / cast(height as float) where width <> 0 and height <> 0;";
            migrationBuilder.Sql(sql2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageMetaData_AspectRatio",
                table: "ImageMetaData");

            migrationBuilder.DropColumn(
                name: "AspectRatio",
                table: "ImageMetaData");
        }
    }
}
