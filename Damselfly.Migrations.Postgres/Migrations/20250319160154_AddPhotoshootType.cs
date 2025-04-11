using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoshootType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhotoShootType",
                table: "PhotoShoots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoShootType",
                table: "PhotoShoots");
        }
    }
}
