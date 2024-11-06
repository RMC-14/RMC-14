#!/usr/bin/env pwsh


dotnet ef migrations add --context SqliteServerDbContext -o Migrations/Sqlite ArmorPreferences
dotnet ef migrations add --context PostgresServerDbContext -o Migrations/Postgres ArmorPreferences
