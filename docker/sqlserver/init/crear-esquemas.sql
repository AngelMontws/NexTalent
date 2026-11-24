-- NexTalent: base de datos y esquemas vacios (uno por servicio).
-- Es idempotente: se puede ejecutar varias veces sin error.

IF DB_ID(N'NexTalent') IS NULL
BEGIN
    CREATE DATABASE [NexTalent];
END
GO

USE [NexTalent];
GO

IF SCHEMA_ID(N'identidad') IS NULL EXEC(N'CREATE SCHEMA [identidad]');
IF SCHEMA_ID(N'vacantes') IS NULL EXEC(N'CREATE SCHEMA [vacantes]');
IF SCHEMA_ID(N'postulaciones') IS NULL EXEC(N'CREATE SCHEMA [postulaciones]');
IF SCHEMA_ID(N'notificaciones') IS NULL EXEC(N'CREATE SCHEMA [notificaciones]');
GO