using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class PhotoShootReservationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestExpirationDateTime",
                table: "PhotoShoots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationCode",
                table: "PhotoShoots",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestExpirationDateTime",
                table: "PhotoShoots");

            migrationBuilder.DropColumn(
                name: "ReservationCode",
                table: "PhotoShoots");
        }
    }
}
