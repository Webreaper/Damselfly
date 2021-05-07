using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Damselfly.Core.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Baskets",
                columns: table => new
                {
                    BasketId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baskets", x => x.BasketId);
                });

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    CameraId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Model = table.Column<string>(nullable: true),
                    Make = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.CameraId);
                });

            migrationBuilder.CreateTable(
                name: "ConfigSettings",
                columns: table => new
                {
                    ConfigSettingId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigSettings", x => x.ConfigSettingId);
                });

            migrationBuilder.CreateTable(
                name: "DownloadConfigs",
                columns: table => new
                {
                    ExportConfigId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    Size = table.Column<int>(nullable: false),
                    WatermarkText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadConfigs", x => x.ExportConfigId);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    FolderId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(nullable: true),
                    ParentFolderId = table.Column<int>(nullable: false),
                    FolderScanDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.FolderId);
                });

            migrationBuilder.CreateTable(
                name: "FTSTags",
                columns: table => new
                {
                    FTSTagId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Keyword = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTSTags", x => x.FTSTagId);
                });

            migrationBuilder.CreateTable(
                name: "Lenses",
                columns: table => new
                {
                    LensId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Model = table.Column<string>(nullable: true),
                    Make = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lenses", x => x.LensId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Keyword = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    TimeStamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FolderId = table.Column<int>(nullable: false),
                    FileName = table.Column<string>(nullable: true),
                    FileSizeBytes = table.Column<ulong>(nullable: false),
                    FileCreationDate = table.Column<DateTime>(nullable: false),
                    FileLastModDate = table.Column<DateTime>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
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
                    BasketEntryId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    ImageId = table.Column<int>(nullable: false),
                    BasketId = table.Column<int>(nullable: false)
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
                    MetaDataId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageId = table.Column<int>(nullable: false),
                    Width = table.Column<int>(nullable: false),
                    Height = table.Column<int>(nullable: false),
                    Rating = table.Column<int>(nullable: false),
                    Caption = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    ISO = table.Column<string>(nullable: true),
                    FNum = table.Column<string>(nullable: true),
                    Exposure = table.Column<string>(nullable: true),
                    DateTaken = table.Column<DateTime>(nullable: false),
                    CameraId = table.Column<int>(nullable: true),
                    LensId = table.Column<int>(nullable: true),
                    LastUpdated = table.Column<DateTime>(nullable: false),
                    ThumbLastUpdated = table.Column<DateTime>(nullable: true)
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
                    ImageId = table.Column<int>(nullable: false),
                    TagId = table.Column<int>(nullable: false)
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
                    KeywordOperationId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageId = table.Column<int>(nullable: false),
                    Keyword = table.Column<string>(nullable: false),
                    Operation = table.Column<int>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordOperations", x => x.KeywordOperationId);
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
                name: "IX_Images_FolderId",
                table: "Images",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_LastUpdated",
                table: "Images",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_TagId",
                table: "ImageTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_ImageId_TagId",
                table: "ImageTags",
                columns: new[] { "ImageId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeywordOperations_TimeStamp",
                table: "KeywordOperations",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordOperations_ImageId_Keyword",
                table: "KeywordOperations",
                columns: new[] { "ImageId", "Keyword" });

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
