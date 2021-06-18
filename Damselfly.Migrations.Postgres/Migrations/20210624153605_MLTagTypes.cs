using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class MLTagTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RectHeight",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "RectWidth",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "RectX",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "RectY",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Tags");

            migrationBuilder.AddColumn<int>(
                name: "ClassificationId",
                table: "Images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "ClassificationScore",
                table: "Images",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateTable(
                name: "ImageClassifications",
                columns: table => new
                {
                    ClassificationId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageClassifications", x => x.ClassificationId);
                    table.ForeignKey(
                        name: "FK_ImageClassifications_Images_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageObjects",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    RectX = table.Column<int>(type: "integer", nullable: false),
                    RectY = table.Column<int>(type: "integer", nullable: false),
                    RectWidth = table.Column<int>(type: "integer", nullable: false),
                    RectHeight = table.Column<int>(type: "integer", nullable: false),
                    TagId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageObjects", x => new { x.ImageId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ImageObjects_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageObjects_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageObjects_Tags_TagId1",
                        column: x => x.TagId1,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageClassifications_Label",
                table: "ImageClassifications",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjects_TagId",
                table: "ImageObjects",
                column: "TagId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjects_TagId1",
                table: "ImageObjects",
                column: "TagId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageClassifications");

            migrationBuilder.DropTable(
                name: "ImageObjects");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ClassificationScore",
                table: "Images");

            migrationBuilder.AddColumn<int>(
                name: "RectHeight",
                table: "Tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RectWidth",
                table: "Tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RectX",
                table: "Tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RectY",
                table: "Tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Score",
                table: "Tags",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
