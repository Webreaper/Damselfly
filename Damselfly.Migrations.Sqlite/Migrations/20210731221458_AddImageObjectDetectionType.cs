using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddImageObjectDetectionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecogntionSource",
                table: "ImageObjects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_AILastUpdated",
                table: "ImageMetaData",
                column: "AILastUpdated");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageMetaData_AILastUpdated",
                table: "ImageMetaData");

            migrationBuilder.DropColumn(
                name: "RecogntionSource",
                table: "ImageObjects");
        }
    }
}
