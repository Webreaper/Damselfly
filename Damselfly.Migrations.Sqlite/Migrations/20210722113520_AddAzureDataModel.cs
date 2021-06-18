using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddAzureDataModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonId",
                table: "ImageObjects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CloudTransactions",
                columns: table => new
                {
                    CloudTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransType = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudTransactions", x => x.CloudTransactionId);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    AzurePersonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.PersonId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjects_PersonId",
                table: "ImageObjects",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CloudTransactions_Date_TransType",
                table: "CloudTransactions",
                columns: new[] { "Date", "TransType" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImageObjects_People_PersonId",
                table: "ImageObjects",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageObjects_People_PersonId",
                table: "ImageObjects");

            migrationBuilder.DropTable(
                name: "CloudTransactions");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropIndex(
                name: "IX_ImageObjects_PersonId",
                table: "ImageObjects");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "ImageObjects");
        }
    }
}
