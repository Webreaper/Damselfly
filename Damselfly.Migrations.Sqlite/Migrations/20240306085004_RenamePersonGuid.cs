using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenamePersonGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudTransactions");

            migrationBuilder.RenameColumn(
                name: "AzurePersonId",
                table: "People",
                newName: "PersonGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PersonGuid",
                table: "People",
                newName: "AzurePersonId");

            migrationBuilder.CreateTable(
                name: "CloudTransactions",
                columns: table => new
                {
                    CloudTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TransType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudTransactions", x => x.CloudTransactionId);
                });
        }
    }
}
