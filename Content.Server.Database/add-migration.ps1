#!/usr/bin/env pwsh

dotnet ef migrations add --context SqliteServerDbContext -o Migrations/Sqlite "SurvivorJobVariants"
dotnet ef migrations add --context PostgresServerDbContext -o Migrations/Postgres "SurvivorJobVariants"
