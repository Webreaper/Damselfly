using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Damselfly.Core.Migrations
{
    public partial class AddFTSTriggers : Migration
    {
        const string s_DropTriggers = @"
                    DROP TRIGGER IF EXISTS ftskeywords_before_update;
                    DROP TRIGGER IF EXISTS ftskeywords_before_delete;
                    DROP TRIGGER IF EXISTS ftskeywords_after_update;
                    DROP TRIGGER IF EXISTS ftskeywords_after_insert;
                    DROP TRIGGER IF EXISTS ftsnames_before_update;
                    DROP TRIGGER IF EXISTS ftsnames_before_delete;
                    DROP TRIGGER IF EXISTS ftssnames_after_update;
                    DROP TRIGGER IF EXISTS ftsnames_after_insert;
                    DROP TRIGGER IF EXISTS ftsimagemetadata_before_update;
                    DROP TRIGGER IF EXISTS ftsimagemetadata_before_delete;
                    DROP TRIGGER IF EXISTS ftsimagemetadata_after_update;
                    DROP TRIGGER IF EXISTS ftsimagemetadata_after_insert;
                ";
        const string s_TableCreation = @"
                    CREATE VIRTUAL TABLE IF NOT EXISTS FTSKeywords USING fts5( TagId, Keyword );
                    CREATE VIRTUAL TABLE IF NOT EXISTS FTSNames USING fts5( PersonID, Name );
                    CREATE VIRTUAL TABLE IF NOT EXISTS FTSImages USING fts5( ImageId, Caption, Description, Copyright, Credit );
                ";
        const string s_TriggerCreation = @"
                    CREATE TRIGGER ftskeywords_before_update BEFORE UPDATE ON tags BEGIN
                        DELETE FROM FTSKeywords WHERE tagid = old.tagid;
                    END;

                    CREATE TRIGGER ftskeywords_before_delete BEFORE DELETE ON tags BEGIN
                        DELETE FROM FTSKeywords WHERE tagid = old.tagid;
                    END;
                    CREATE TRIGGER ftskeywords_after_update AFTER UPDATE ON tags BEGIN
                        INSERT INTO FTSKeywords (TagId, Keyword) SELECT t.TagId, t.Keyword FROM Tags t where new.TagId = t.TagId;
                    END;

                    CREATE TRIGGER ftskeywords_after_insert AFTER INSERT ON tags BEGIN
                        INSERT INTO FTSKeywords (TagId, Keyword) SELECT t.TagId, t.Keyword FROM Tags t where new.TagId = t.TagId;
                    END;

                    CREATE TRIGGER ftsnames_before_update BEFORE UPDATE ON people BEGIN
                        DELETE FROM FTSNames WHERE personid = old.personid;
                    END;

                    CREATE TRIGGER ftsnames_before_delete BEFORE DELETE ON people BEGIN
                        DELETE FROM FTSNames WHERE personid = old.personid;
                    END;

                    CREATE TRIGGER ftssnames_after_update AFTER UPDATE ON people BEGIN
                        INSERT INTO FTSNames (PersonID, Name) SELECT PersonId, Name FROM people p where new.PersonId = p.PersonId AND p.state = 1;
                    END;

                    CREATE TRIGGER ftsnames_after_insert AFTER INSERT ON people BEGIN
                        INSERT INTO FTSNames (PersonID, Name) SELECT PersonId, Name FROM people p where new.PersonId = p.PersonId AND p.state = 1;
                    END;

                    CREATE TRIGGER ftsimagemetadata_before_update BEFORE UPDATE ON ImageMetaData BEGIN
                        DELETE FROM FTSImages WHERE imageId = old.imageId;
                    END;

                    CREATE TRIGGER ftsimagemetadata_before_delete BEFORE DELETE ON ImageMetaData BEGIN
                        DELETE FROM FTSImages WHERE imageId = old.imageId;
                    END;

                    CREATE TRIGGER ftsimagemetadata_after_update AFTER UPDATE ON ImageMetaData BEGIN
                        INSERT INTO FTSImages ( ImageId, Caption, Description, Copyright, Credit ) 
                                SELECT i.ImageId, i.Caption, i.Description, i.CopyRight, i.Credit 
                                FROM imagemetadata i 
                                WHERE new.ImageID = i.ImageId 
                                        AND (coalesce(i.Caption, '') <> '' 
                                             OR coalesce(i.Description, '') <> '' 
                                             OR coalesce(i.Copyright, '') <> '' 
                                             OR coalesce(i.Credit, '') <> '');
                    END;

                    CREATE TRIGGER ftsimagemetadata_after_insert AFTER INSERT ON images BEGIN
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

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(s_DropTriggers);
            migrationBuilder.Sql(s_TableCreation);
            migrationBuilder.Sql(s_TriggerCreation);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "ddf4d227-28f9-43af-a72d-6f7778ea87a7");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "33b555f9-1e38-4250-bdac-1d56fce31a78");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "a0007d57-b34b-46c3-90f5-d4db384b2259");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(s_DropTriggers);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConcurrencyStamp",
                value: "57ba482a-1757-4907-824b-8a0e32b0c325");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConcurrencyStamp",
                value: "48184797-ab82-44e8-91ff-8cf6c4931126");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "ConcurrencyStamp",
                value: "82edb408-85aa-4d03-9d93-1a8e97fb2492");
        }
    }
}
