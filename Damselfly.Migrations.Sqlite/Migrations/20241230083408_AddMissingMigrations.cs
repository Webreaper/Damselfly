using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "6ec227c9-7b11-4339-95f2-a991d402064c");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "24202ce6-6127-4455-9940-c4887b9d160f");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "b0cdcef5-5239-46ab-a264-45a48e7d87b3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "86fbaf98-ef71-40fd-a386-a382266193af");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "b1ada73b-051d-4cf7-9cf7-612368004319");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "95a267fc-0628-4cd9-a9b4-9e9d631284b1");
        }
    }
}
