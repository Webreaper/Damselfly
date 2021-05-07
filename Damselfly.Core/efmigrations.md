
EF Migrations

1. Run this from the root Damselfly folder

    dotnet ef migrations add Initial --project Damselfly.Migrations.Postgres --startup-project Damselfly.Web

Note: Need to change the default initialiser for DatabaseSpecialisation in BaseModel.cs until this is parameterised. Once it's parameterised we can pass on the ef migrations commandline, and do something like this: 

    https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli#using-one-context-type