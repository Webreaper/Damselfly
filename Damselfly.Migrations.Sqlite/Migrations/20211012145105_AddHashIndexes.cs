using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class AddHashIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "edb1b335-1ffc-40ee-a3a5-5bd96a555044");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "b43d08d3-89cb-4162-b0c2-26eaf6b38c30");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "95858e91-470e-4154-bf23-43699746aa68");

            migrationBuilder.CreateIndex(
                name: "IX_Hashes_PerceptualHex1_PerceptualHex2_PerceptualHex3_PerceptualHex4",
                table: "Hashes",
                columns: new[] { "PerceptualHex1", "PerceptualHex2", "PerceptualHex3", "PerceptualHex4" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hashes_PerceptualHex1_PerceptualHex2_PerceptualHex3_PerceptualHex4",
                table: "Hashes");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "82b60d90-9f24-4a12-a246-bc6e1f529102");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "adc5f5b1-cc1c-4827-abe8-0c7c5fdbbc68");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "3d168dfe-134e-4802-a6c0-923f450b99c9");
        }
    }
}
