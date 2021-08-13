
EF Migrations

1. Uncomment the PostGres line in BaseDBModel.OnConfiguring.

2. Run this from the root Damselfly folder

    dotnet ef migrations add <migrationName> --project Damselfly.Migrations.Sqlite --startup-project Damselfly.Web

3. Comment the Postgres line and uncomment the Sqlite line in OnConfiguring.

4. Run the same migrations command, but for PostGres: 

    dotnet ef migrations add <migrationName> --project Damselfly.Migrations.Postgres --startup-project Damselfly.Web

Note: Need to change the default initialiser for DatabaseSpecialisation in BaseModel.cs until this
is parameterised. Once it's parameterised we can pass on the ef migrations commandline, and do something like this: 

    https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli#using-one-context-type