using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTransformations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transformations",
                columns: table => new
                {
                    TransformationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransformsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transformations", x => x.TransformationId);
                    table.ForeignKey(
                        name: "FK_Transformations_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transformations_ImageId",
                table: "Transformations",
                column: "ImageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transformations");
        }
    }
}
