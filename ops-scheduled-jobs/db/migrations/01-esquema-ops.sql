-- 01 - Esquema del modulo de automatizacion.
--
-- Sin EF Core no hay migraciones generadas: el esquema son estos scripts, numerados y
-- aplicados en orden por SqlScriptMigrator (ver skill data-migrations, variante SQL/Dapper).
-- Cada script es IDEMPOTENTE: volverlo a correr no falla ni duplica. Eso es lo que permite
-- que el arranque de la aplicacion los ejecute siempre sin preguntarse cuales ya estaban.

IF SCHEMA_ID(N'ops') IS NULL
    EXEC(N'CREATE SCHEMA [ops] AUTHORIZATION [dbo];');
GO
