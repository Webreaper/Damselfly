using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Damselfly.Core.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Baskets",
                columns: table => new
                {
                    BasketId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baskets", x => x.BasketId);
                });

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    CameraId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Serial = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.CameraId);
                });

            migrationBuilder.CreateTable(
                name: "ConfigSettings",
                columns: table => new
                {
                    ConfigSettingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigSettings", x => x.ConfigSettingId);
                });

            migrationBuilder.CreateTable(
                name: "DownloadConfigs",
                columns: table => new
                {
                    ExportConfigId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    WatermarkText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadConfigs", x => x.ExportConfigId);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    FolderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "text", nullable: true),
                    ParentFolderId = table.Column<int>(type: "integer", nullable: false),
                    FolderScanDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.FolderId);
                });

            migrationBuilder.CreateTable(
                name: "FTSTags",
                columns: table => new
                {
                    FTSTagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Keyword = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTSTags", x => x.FTSTagId);
                });

            migrationBuilder.CreateTable(
                name: "Lenses",
                columns: table => new
                {
                    LensId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Serial = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lenses", x => x.LensId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Keyword = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Favourite = table.Column<bool>(type: "boolean", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FolderId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileSizeBytes = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FileCreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FileLastModDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SortDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_Images_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "FolderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BasketEntries",
                columns: table => new
                {
                    BasketEntryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    BasketId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketEntries", x => x.BasketEntryId);
                    table.ForeignKey(
                        name: "FK_BasketEntries_Baskets_BasketId",
                        column: x => x.BasketId,
                        principalTable: "Baskets",
                        principalColumn: "BasketId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BasketEntries_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageMetaData",
                columns: table => new
                {
                    MetaDataId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ISO = table.Column<string>(type: "text", nullable: true),
                    FNum = table.Column<string>(type: "text", nullable: true),
                    Exposure = table.Column<string>(type: "text", nullable: true),
                    FlashFired = table.Column<bool>(type: "boolean", nullable: false),
                    DateTaken = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: true),
                    CameraId = table.Column<int>(type: "integer", nullable: true),
                    LensId = table.Column<int>(type: "integer", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ThumbLastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageMetaData", x => x.MetaDataId);
                    table.ForeignKey(
                        name: "FK_ImageMetaData_Cameras_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Cameras",
                        principalColumn: "CameraId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImageMetaData_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageMetaData_Lenses_LensId",
                        column: x => x.LensId,
                        principalTable: "Lenses",
                        principalColumn: "LensId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImageTags",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageTags", x => new { x.ImageId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ImageTags_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImageTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeywordOperations",
                columns: table => new
                {
                    ExifOperationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordOperations", x => x.ExifOperationId);
                    table.ForeignKey(
                        name: "FK_KeywordOperations_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BasketEntries_BasketId",
                table: "BasketEntries",
                column: "BasketId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketEntries_ImageId",
                table: "BasketEntries",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BasketEntries_ImageId_BasketId",
                table: "BasketEntries",
                columns: new[] { "ImageId", "BasketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Folders_FolderScanDate",
                table: "Folders",
                column: "FolderScanDate");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Path",
                table: "Folders",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_CameraId",
                table: "ImageMetaData",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_DateTaken",
                table: "ImageMetaData",
                column: "DateTaken");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_Hash",
                table: "ImageMetaData",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_ImageId",
                table: "ImageMetaData",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_LensId",
                table: "ImageMetaData",
                column: "LensId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_ThumbLastUpdated",
                table: "ImageMetaData",
                column: "ThumbLastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_Images_FileLastModDate",
                table: "Images",
                column: "FileLastModDate");

            migrationBuilder.CreateIndex(
                name: "IX_Images_FileName",
                table: "Images",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_Images_FolderId",
                table: "Images",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_LastUpdated",
                table: "Images",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_Images_SortDate",
                table: "Images",
                column: "SortDate");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_ImageId_TagId",
                table: "ImageTags",
                columns: new[] { "ImageId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_TagId",
                table: "ImageTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordOperations_ImageId_Text",
                table: "KeywordOperations",
                columns: new[] { "ImageId", "Text" });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordOperations_TimeStamp",
                table: "KeywordOperations",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Keyword",
                table: "Tags",
                column: "Keyword",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasketEntries");

            migrationBuilder.DropTable(
                name: "ConfigSettings");

            migrationBuilder.DropTable(
                name: "DownloadConfigs");

            migrationBuilder.DropTable(
                name: "FTSTags");

            migrationBuilder.DropTable(
                name: "ImageMetaData");

            migrationBuilder.DropTable(
                name: "ImageTags");

            migrationBuilder.DropTable(
                name: "KeywordOperations");

            migrationBuilder.DropTable(
                name: "Baskets");

            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "Lenses");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "Folders");
        }
    }
}
