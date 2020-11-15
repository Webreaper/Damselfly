using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class ExifOperation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_KeywordOperations",
                table: "KeywordOperations");

            migrationBuilder.RenameColumn(
                name: "Keyword",
                table: "KeywordOperations",
                newName: "Text");

            migrationBuilder.RenameColumn(
                name: "KeywordOperationId",
                table: "KeywordOperations",
                newName: "Type");

            migrationBuilder.RenameIndex(
                name: "IX_KeywordOperations_ImageId_Keyword",
                table: "KeywordOperations",
                newName: "IX_KeywordOperations_ImageId_Text");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "KeywordOperations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ExifOperationId",
                table: "KeywordOperations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_KeywordOperations",
                table: "KeywordOperations",
                column: "ExifOperationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_KeywordOperations",
                table: "KeywordOperations");

            migrationBuilder.DropColumn(
                name: "ExifOperationId",
                table: "KeywordOperations");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "KeywordOperations",
                newName: "KeywordOperationId");

            migrationBuilder.RenameColumn(
                name: "Text",
                table: "KeywordOperations",
                newName: "Keyword");

            migrationBuilder.RenameIndex(
                name: "IX_KeywordOperations_ImageId_Text",
                table: "KeywordOperations",
                newName: "IX_KeywordOperations_ImageId_Keyword");

            migrationBuilder.AlterColumn<int>(
                name: "KeywordOperationId",
                table: "KeywordOperations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_KeywordOperations",
                table: "KeywordOperations",
                column: "KeywordOperationId");
        }
    }
}
