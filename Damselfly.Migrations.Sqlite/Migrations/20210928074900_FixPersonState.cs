using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class FixPersonState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // State should indicate if somebody is identified. Fix historic data.
            migrationBuilder.Sql("UPDATE people SET state = 1 WHERE name <> 'Unknown';");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "57ba482a-1757-4907-824b-8a0e32b0c325");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "48184797-ab82-44e8-91ff-8cf6c4931126");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "82edb408-85aa-4d03-9d93-1a8e97fb2492");

            migrationBuilder.CreateIndex(
                name: "IX_People_State",
                table: "People",
                column: "State");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_State",
                table: "People");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "36e9bf82-6ad6-46a7-b7b2-4b96a8d2ead3");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "e83efc33-70e8-417f-a059-68c1849d5d00");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "37926f58-3d82-439d-b148-fefaa5fbed37");
        }
    }
}
