using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class CancellationFlagForPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "PaymentTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "PaymentTransactions");
        }
    }
}
