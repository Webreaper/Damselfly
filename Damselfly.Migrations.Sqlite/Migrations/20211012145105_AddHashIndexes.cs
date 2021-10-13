using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class AddHashIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
