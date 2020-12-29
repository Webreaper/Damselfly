using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddSortDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SortDate",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Images_SortDate",
                table: "Images",
                column: "SortDate");

            // Migrate the existing data across.
            migrationBuilder.Sql("UPDATE images SET sortdate = (SELECT coalesce( m.datetaken, images.FileLastModDate ) FROM ImageMetaData m WHERE m.imageid = images.imageid)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_SortDate",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "SortDate",
                table: "Images");
        }
    }
}
