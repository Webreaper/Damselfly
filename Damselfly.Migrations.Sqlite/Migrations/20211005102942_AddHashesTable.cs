using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class AddHashesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageMetaData_Hash",
                table: "ImageMetaData");

            migrationBuilder.CreateTable(
                name: "Hashes",
                columns: table => new
                {
                    HashId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: false),
                    MD5ImageHash = table.Column<string>(type: "TEXT", nullable: true),
                    PerceptualHex1 = table.Column<string>(type: "TEXT", nullable: true),
                    PerceptualHex2 = table.Column<string>(type: "TEXT", nullable: true),
                    PerceptualHex3 = table.Column<string>(type: "TEXT", nullable: true),
                    PerceptualHex4 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hashes", x => x.HashId);
                    table.ForeignKey(
                        name: "FK_Hashes_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "fa3a79e1-2a02-4fde-b26a-1498bd911931");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "f9bb3be3-1fb4-4a1a-bbcb-1d71ca3e198c");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "e2f1656c-d1eb-4db3-b3a0-efffab6ff171");

            migrationBuilder.CreateIndex(
                name: "IX_Hashes_ImageId",
                table: "Hashes",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hashes_MD5ImageHash",
                table: "Hashes",
                column: "MD5ImageHash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hashes");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "ddf4d227-28f9-43af-a72d-6f7778ea87a7");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "33b555f9-1e38-4250-bdac-1d56fce31a78");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "a0007d57-b34b-46c3-90f5-d4db384b2259");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_Hash",
                table: "ImageMetaData",
                column: "Hash");
        }
    }
}
