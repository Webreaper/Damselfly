using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintPhotoShootToAlbum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoShoots_AlbumId",
                table: "PhotoShoots");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "edc28fea-4625-46dc-b435-54be216ca490");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "05653c42-59ec-4af1-bc8e-9ee66bdf09cc");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "53a2c48c-85bd-4d6b-9193-59f150738da1");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoShoots_AlbumId",
                table: "PhotoShoots",
                column: "AlbumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoShoots_AlbumId",
                table: "PhotoShoots");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "7080c453-6249-4d28-af32-a3c136b37823");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "e7ff9b0d-5702-4ab4-aa1f-f8b48552e116");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "8d149178-11cb-4142-b32c-ff272efe8c31");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoShoots_AlbumId",
                table: "PhotoShoots",
                column: "AlbumId",
                unique: true);
        }
    }
}
