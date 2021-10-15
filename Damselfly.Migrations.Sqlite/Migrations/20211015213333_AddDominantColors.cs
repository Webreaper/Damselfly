using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class AddDominantColors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AverageColor",
                table: "ImageMetaData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DominantColor",
                table: "ImageMetaData",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageColor",
                table: "ImageMetaData");

            migrationBuilder.DropColumn(
                name: "DominantColor",
                table: "ImageMetaData");
        }
    }
}
