using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddCameraMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Serial",
                table: "Lenses",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FlashFired",
                table: "ImageMetaData",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Serial",
                table: "Cameras",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Serial",
                table: "Lenses");

            migrationBuilder.DropColumn(
                name: "FlashFired",
                table: "ImageMetaData");

            migrationBuilder.DropColumn(
                name: "Serial",
                table: "Cameras");
        }
    }
}
