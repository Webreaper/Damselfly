using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAspectRatio : Migration
    {
        const string s_DropTriggers = @"
                    DROP TRIGGER IF EXISTS ftskeywords_before_update;
                    DROP TRIGGER IF EXISTS ftskeywords_after_update;
                    DROP TRIGGER IF EXISTS ftsnames_before_update;
                    DROP TRIGGER IF EXISTS ftssnames_after_update;
                    DROP TRIGGER IF EXISTS ftsimagemetadata_before_update;
                    DROP TRIGGER IF EXISTS ftsimagemetadata_after_update;
                ";

        const string s_TriggerCreation = @"
                    CREATE TRIGGER ftskeywords_before_update BEFORE UPDATE OF keyword ON tags BEGIN
                        DELETE FROM FTSKeywords WHERE tagid = old.tagid;
                    END;

                    CREATE TRIGGER ftskeywords_after_update AFTER UPDATE OF keyword ON tags BEGIN
                        INSERT INTO FTSKeywords (TagId, Keyword) SELECT t.TagId, t.Keyword FROM Tags t where new.TagId = t.TagId;
                    END;

                    CREATE TRIGGER ftsnames_before_update BEFORE UPDATE OF name ON people BEGIN
                        DELETE FROM FTSNames WHERE personid = old.personid;
                    END;

                    CREATE TRIGGER ftssnames_after_update AFTER UPDATE OF name ON people BEGIN
                        INSERT INTO FTSNames (PersonID, Name) SELECT PersonId, Name FROM people p where new.PersonId = p.PersonId AND p.state = 1;
                    END;

                    CREATE TRIGGER ftsimagemetadata_before_update BEFORE UPDATE OF Caption, Description, Copyright, Credit ON ImageMetaData BEGIN
                        DELETE FROM FTSImages WHERE imageId = old.imageId;
                    END;

                    CREATE TRIGGER ftsimagemetadata_after_update AFTER UPDATE OF Caption, Description, Copyright, Credit ON ImageMetaData BEGIN
                        INSERT INTO FTSImages ( ImageId, Caption, Description, Copyright, Credit ) 
                                SELECT i.ImageId, i.Caption, i.Description, i.CopyRight, i.Credit 
                                FROM imagemetadata i 
                                WHERE new.ImageID = i.ImageId 
                                        AND (coalesce(i.Caption, '') <> '' 
                                             OR coalesce(i.Description, '') <> '' 
                                             OR coalesce(i.Copyright, '') <> '' 
                                             OR coalesce(i.Credit, '') <> '');
                    END;
                ";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(s_DropTriggers);
            migrationBuilder.Sql(s_TriggerCreation);

            migrationBuilder.AddColumn<double>(
                name: "AspectRatio",
                table: "ImageMetaData",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetaData_AspectRatio",
                table: "ImageMetaData",
                column: "AspectRatio");

            // Populate the Aspect Ratio column
            const string sql = @"update imagemetadata set aspectratio = COALESCE(cast([Width] as float) / NULLIF(cast([Height] as float),0), 1);";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageMetaData_AspectRatio",
                table: "ImageMetaData");

            migrationBuilder.DropColumn(
                name: "AspectRatio",
                table: "ImageMetaData");

            migrationBuilder.Sql(s_DropTriggers);
        }
    }
}
