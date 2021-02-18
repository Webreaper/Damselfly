using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class BasketEntryIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BasketEntries_ImageId_BasketId",
                table: "BasketEntries",
                columns: new[] { "ImageId", "BasketId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BasketEntries_ImageId_BasketId",
                table: "BasketEntries");
        }
    }
}
