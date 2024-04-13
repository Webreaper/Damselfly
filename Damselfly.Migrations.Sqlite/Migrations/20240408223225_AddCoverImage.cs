using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Album",
                type: "INTEGER",
                nullable: true);


            migrationBuilder.CreateIndex(
                name: "IX_Album_CoverImageId",
                table: "Album",
                column: "CoverImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Album_Images_CoverImageId",
                table: "Album",
                column: "CoverImageId",
                principalTable: "Images",
                principalColumn: "ImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Album_Images_CoverImageId",
                table: "Album");

            migrationBuilder.DropIndex(
                name: "IX_Album_CoverImageId",
                table: "Album");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Album");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "153b5e4f-7aaf-4cf4-9828-bb2d25866051");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "38d8f057-7fcb-4c67-a980-f0df8f09e5cb");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "9584db4b-125d-4c4f-bbc0-b8c45cdb8feb");
        }
    }
}
