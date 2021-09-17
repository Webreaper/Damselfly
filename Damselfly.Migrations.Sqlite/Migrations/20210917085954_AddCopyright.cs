using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddCopyright : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Copyright",
                table: "ImageMetaData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Credit",
                table: "ImageMetaData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "51edafb5-82a2-4567-9bc5-cb9ff30c1935");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "c77e8b3e-d3c2-451c-865b-c5d00b38c729");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "218160f1-65f9-4ff1-ad2d-4c3e8b59c916");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Copyright",
                table: "ImageMetaData");

            migrationBuilder.DropColumn(
                name: "Credit",
                table: "ImageMetaData");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "078c84ac-de77-4203-b494-0dddb96b17ea");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "5964515f-a02a-43d2-a735-bccffce39695");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "aeaac73c-2169-4612-84e2-710d7e638dd9");
        }
    }
}
