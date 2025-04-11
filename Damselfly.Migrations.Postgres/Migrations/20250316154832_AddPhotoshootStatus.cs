using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoshootStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTimeUtc",
                table: "PhotoShoots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCalendarId",
                table: "PhotoShoots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "PhotoShoots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PhotoShoots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"PhotoShoots\" SET \"Status\" = 6 WHERE \"IsDeleted\" = true;");
            migrationBuilder.Sql("UPDATE \"PhotoShoots\" SET \"Status\" = 4 WHERE \"PicturesDelivered\" = true;");
            migrationBuilder.Sql("UPDATE \"PhotoShoots\" SET \"Status\" = 2 WHERE \"IsConfirmed\" = true AND \"Status\" < 4;");

            migrationBuilder.DropColumn(
               name: "IsConfirmed",
               table: "PhotoShoots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhotoShoots");

            migrationBuilder.DropColumn(
                name: "PicturesDelivered",
                table: "PhotoShoots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDateTimeUtc",
                table: "PhotoShoots");

            migrationBuilder.DropColumn(
                name: "ExternalCalendarId",
                table: "PhotoShoots");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "PhotoShoots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PhotoShoots");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "PhotoShoots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhotoShoots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PicturesDelivered",
                table: "PhotoShoots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
