using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ExplicitMappingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumImage_Albums_AlbumsAlbumId",
                table: "AlbumImage");

            migrationBuilder.DropForeignKey(
                name: "FK_AlbumImage_Images_ImagesImageId",
                table: "AlbumImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AlbumImage",
                table: "AlbumImage");

            migrationBuilder.RenameColumn(
                name: "ImagesImageId",
                table: "AlbumImage",
                newName: "ImageId");

            migrationBuilder.RenameColumn(
                name: "AlbumsAlbumId",
                table: "AlbumImage",
                newName: "AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_AlbumImage_ImagesImageId",
                table: "AlbumImage",
                newName: "IX_AlbumImage_ImageId");

            migrationBuilder.AddColumn<Guid>(
                name: "AlbumImageId",
                table: "AlbumImage",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AlbumImage",
                table: "AlbumImage",
                column: "AlbumImageId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumImage_AlbumId",
                table: "AlbumImage",
                column: "AlbumId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumImage_Albums_AlbumId",
                table: "AlbumImage",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "AlbumId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumImage_Images_ImageId",
                table: "AlbumImage",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "ImageId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumImage_Albums_AlbumId",
                table: "AlbumImage");

            migrationBuilder.DropForeignKey(
                name: "FK_AlbumImage_Images_ImageId",
                table: "AlbumImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AlbumImage",
                table: "AlbumImage");

            migrationBuilder.DropIndex(
                name: "IX_AlbumImage_AlbumId",
                table: "AlbumImage");

            migrationBuilder.DropColumn(
                name: "AlbumImageId",
                table: "AlbumImage");

            migrationBuilder.RenameColumn(
                name: "ImageId",
                table: "AlbumImage",
                newName: "ImagesImageId");

            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "AlbumImage",
                newName: "AlbumsAlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_AlbumImage_ImageId",
                table: "AlbumImage",
                newName: "IX_AlbumImage_ImagesImageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AlbumImage",
                table: "AlbumImage",
                columns: new[] { "AlbumsAlbumId", "ImagesImageId" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "8d4d0b46-4409-4785-af52-218f2bbc5cdf");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "818d1b3d-effc-49e0-8ccf-34444079c21e");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "b6a8ca44-f898-4504-b4bb-1703585dd324");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumImage_Albums_AlbumsAlbumId",
                table: "AlbumImage",
                column: "AlbumsAlbumId",
                principalTable: "Albums",
                principalColumn: "AlbumId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumImage_Images_ImagesImageId",
                table: "AlbumImage",
                column: "ImagesImageId",
                principalTable: "Images",
                principalColumn: "ImageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
