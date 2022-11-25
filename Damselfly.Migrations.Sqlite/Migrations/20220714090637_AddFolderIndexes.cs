using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Folders",
                type: "INTEGER",
                nullable: true);

            // Copy the values across, except for the zero-value entry
            const string sql = @"UPDATE Folders SET ParentID = ParentFolderId where ParentFolderId <> 0;";
            migrationBuilder.Sql(sql);

            migrationBuilder.DropColumn(
                name: "ParentFolderId",
                table: "Folders");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_ParentId",
                table: "Folders",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Folders_ParentId",
                table: "Folders",
                column: "ParentId",
                principalTable: "Folders",
                principalColumn: "FolderId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Folders_ParentId",
                table: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Folders_ParentId",
                table: "Folders");

            migrationBuilder.AddColumn<int>(
                name: "ParentFolderId",
                table: "Folders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            const string sql = @"UPDATE Folders SET ParentFolderID = COALESCE( ParentId, 0 );";
            migrationBuilder.Sql(sql);

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Folders");
        }
    }
}
