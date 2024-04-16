using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipBetweenAlbumAndFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Album_Images_CoverImageId",
                table: "Album");

            migrationBuilder.DropForeignKey(
                name: "FK_AlbumImage_Album_AlbumsAlbumId",
                table: "AlbumImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Album",
                table: "Album");

            migrationBuilder.RenameTable(
                name: "Album",
                newName: "Albums");

            migrationBuilder.RenameIndex(
                name: "IX_Album_CoverImageId",
                table: "Albums",
                newName: "IX_Albums_CoverImageId");

            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "Albums",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Albums",
                table: "Albums",
                column: "AlbumId");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "127a2f82-b5f3-4739-9659-17d5cf31aac8");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "a79a5b6a-1f9b-4206-84b5-a972365c72da");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "c3a5aae7-317c-4204-9aaf-270ff4200c79");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_FolderId",
                table: "Albums",
                column: "FolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumImage_Albums_AlbumsAlbumId",
                table: "AlbumImage",
                column: "AlbumsAlbumId",
                principalTable: "Albums",
                principalColumn: "AlbumId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_Folders_FolderId",
                table: "Albums",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "FolderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_Images_CoverImageId",
                table: "Albums",
                column: "CoverImageId",
                principalTable: "Images",
                principalColumn: "ImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumImage_Albums_AlbumsAlbumId",
                table: "AlbumImage");

            migrationBuilder.DropForeignKey(
                name: "FK_Albums_Folders_FolderId",
                table: "Albums");

            migrationBuilder.DropForeignKey(
                name: "FK_Albums_Images_CoverImageId",
                table: "Albums");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Albums",
                table: "Albums");

            migrationBuilder.DropIndex(
                name: "IX_Albums_FolderId",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Albums");

            migrationBuilder.RenameTable(
                name: "Albums",
                newName: "Album");

            migrationBuilder.RenameIndex(
                name: "IX_Albums_CoverImageId",
                table: "Album",
                newName: "IX_Album_CoverImageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Album",
                table: "Album",
                column: "AlbumId");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "7f8ec30e-c7ab-4d53-ab2a-a1a2a16c4896");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "82a093c0-20e3-4958-9140-635fdc5dc5f1");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "aa0989e0-1f13-4bf6-907b-b4810ab3ad74");

            migrationBuilder.AddForeignKey(
                name: "FK_Album_Images_CoverImageId",
                table: "Album",
                column: "CoverImageId",
                principalTable: "Images",
                principalColumn: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumImage_Album_AlbumsAlbumId",
                table: "AlbumImage",
                column: "AlbumsAlbumId",
                principalTable: "Album",
                principalColumn: "AlbumId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
