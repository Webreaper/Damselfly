using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class AddMLObjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tags");

            migrationBuilder.AddColumn<int>(
                name: "TagType",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClassificationId",
                table: "Images",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "ClassificationScore",
                table: "Images",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateTable(
                name: "ImageClassifications",
                columns: table => new
                {
                    ClassificationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true)
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
                    ImageObjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<float>(type: "REAL", nullable: false),
                    RectX = table.Column<int>(type: "INTEGER", nullable: false),
                    RectY = table.Column<int>(type: "INTEGER", nullable: false),
                    RectWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    RectHeight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageObjects", x => x.ImageObjectId);
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageClassifications_Label",
                table: "ImageClassifications",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjects_ImageId",
                table: "ImageObjects",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageObjects_TagId",
                table: "ImageObjects",
                column: "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageClassifications");

            migrationBuilder.DropTable(
                name: "ImageObjects");

            migrationBuilder.DropColumn(
                name: "TagType",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ClassificationScore",
                table: "Images");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Tags",
                type: "TEXT",
                nullable: true);
        }
    }
}
