using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddMLTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageObjects",
                table: "ImageObjects");

            migrationBuilder.AddColumn<int>(
                name: "ImageObjectId",
                table: "ImageObjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageObjects",
                table: "ImageObjects",
                column: "ImageObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjects_ImageId",
                table: "ImageObjects",
                column: "ImageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageObjects",
                table: "ImageObjects");

            migrationBuilder.DropIndex(
                name: "IX_ImageObjects_ImageId",
                table: "ImageObjects");

            migrationBuilder.DropColumn(
                name: "ImageObjectId",
                table: "ImageObjects");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageObjects",
                table: "ImageObjects",
                columns: new[] { "ImageId", "TagId" });
        }
    }
}
