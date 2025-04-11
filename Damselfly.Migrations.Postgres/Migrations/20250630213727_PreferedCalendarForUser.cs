using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class PreferedCalendarForUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredCalendarId",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredCalendarId",
                table: "Users");
        }
    }
}
