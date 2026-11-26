IF OBJECT_ID(N'[postulaciones].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'postulaciones') IS NULL EXEC(N'CREATE SCHEMA [postulaciones];');
    CREATE TABLE [postulaciones].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    IF SCHEMA_ID(N'postulaciones') IS NULL EXEC(N'CREATE SCHEMA [postulaciones];');
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE SEQUENCE [postulaciones].[folio_seq] START WITH 1 INCREMENT BY 1 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE TABLE [postulaciones].[candidato_replica] (
        [candidato_id] uniqueidentifier NOT NULL,
        [tiene_nombre] bit NOT NULL,
        [tiene_contacto] bit NOT NULL,
        [version_evento] bigint NOT NULL,
        CONSTRAINT [pk_candidato_replica] PRIMARY KEY ([candidato_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE TABLE [postulaciones].[vacante_replica] (
        [vacante_id] uniqueidentifier NOT NULL,
        [empresa_id] uniqueidentifier NOT NULL,
        [titulo] nvarchar(200) NOT NULL,
        [nombre_empresa] nvarchar(200) NULL,
        [salario_min] decimal(12,2) NULL,
        [salario_max] decimal(12,2) NULL,
        [tipo_salario] varchar(10) NULL,
        [expira_en] datetime2(0) NULL,
        [estado] varchar(25) NOT NULL,
        [actualizada_en] datetime2(0) NOT NULL,
        [version_evento] bigint NOT NULL,
        CONSTRAINT [pk_vacante_replica] PRIMARY KEY ([vacante_id]),
        CONSTRAINT [ck_vacante_replica_estado] CHECK (estado IN ('Activa', 'Cerrada'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE TABLE [postulaciones].[candidato_habilidad_replica] (
        [candidato_id] uniqueidentifier NOT NULL,
        [habilidad_id] uniqueidentifier NOT NULL,
        [nivel] varchar(12) NOT NULL,
        CONSTRAINT [pk_candidato_habilidad_replica] PRIMARY KEY ([candidato_id], [habilidad_id]),
        CONSTRAINT [ck_candidato_habilidad_replica_nivel] CHECK (nivel IN ('Basico', 'Intermedio', 'Avanzado')),
        CONSTRAINT [fk_candidato_habilidad_replica_candidato] FOREIGN KEY ([candidato_id]) REFERENCES [postulaciones].[candidato_replica] ([candidato_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE TABLE [postulaciones].[postulacion] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [folio] varchar(20) NOT NULL,
        [candidato_id] uniqueidentifier NOT NULL,
        [vacante_id] uniqueidentifier NOT NULL,
        [coincidencia_pct] decimal(5,2) NOT NULL,
        [deseables_cumplidas] int NOT NULL,
        [creada_en] datetime2(0) NOT NULL,
        [canal_origen] varchar(15) NOT NULL,
        [estado_actual] varchar(25) NOT NULL,
        [etapa_actualizada_en] datetime2(0) NOT NULL,
        [recordatorio_10d_enviado] bit NOT NULL,
        [version] rowversion NULL,
        CONSTRAINT [pk_postulacion] PRIMARY KEY ([id]),
        CONSTRAINT [ck_postulacion_canal_origen] CHECK (canal_origen IN ('Web', 'WhatsApp')),
        CONSTRAINT [ck_postulacion_coincidencia_pct] CHECK (coincidencia_pct BETWEEN 0 AND 100),
        CONSTRAINT [ck_postulacion_deseables_cumplidas] CHECK (deseables_cumplidas >= 0),
        CONSTRAINT [ck_postulacion_estado_actual] CHECK (estado_actual IN ('Enviada', 'Vista', 'EnRevision', 'Entrevista', 'OfertaContratado', 'NoSeleccionado')),
        CONSTRAINT [fk_postulacion_vacante_replica] FOREIGN KEY ([vacante_id]) REFERENCES [postulaciones].[vacante_replica] ([vacante_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE TABLE [postulaciones].[vacante_habilidad_replica] (
        [vacante_id] uniqueidentifier NOT NULL,
        [habilidad_id] uniqueidentifier NOT NULL,
        [nivel] varchar(12) NOT NULL,
        [requerida] bit NOT NULL,
        CONSTRAINT [pk_vacante_habilidad_replica] PRIMARY KEY ([vacante_id], [habilidad_id]),
        CONSTRAINT [ck_vacante_habilidad_replica_nivel] CHECK (nivel IN ('Basico', 'Intermedio', 'Avanzado')),
        CONSTRAINT [fk_vacante_habilidad_replica_vacante] FOREIGN KEY ([vacante_id]) REFERENCES [postulaciones].[vacante_replica] ([vacante_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE TABLE [postulaciones].[historial_estado] (
        [id] bigint NOT NULL IDENTITY,
        [secuencia] int NOT NULL,
        [estado_anterior] varchar(25) NULL,
        [estado_nuevo] varchar(25) NOT NULL,
        [causa] varchar(25) NOT NULL,
        [cambiado_por] uniqueidentifier NULL,
        [motivo] nvarchar(300) NULL,
        [fecha] datetime2(0) NOT NULL,
        [postulacion_id] uniqueidentifier NOT NULL,
        CONSTRAINT [pk_historial_estado] PRIMARY KEY ([id]),
        CONSTRAINT [ck_historial_estado_causa] CHECK (causa IN ('Creacion', 'PerfilAbierto', 'Avance', 'Descarte', 'VacanteCerrada', 'Reactivacion')),
        CONSTRAINT [ck_historial_estado_motivo_reactivacion] CHECK (estado_anterior IS NULL OR estado_anterior <> 'NoSeleccionado' OR LEN(LTRIM(motivo)) > 0),
        CONSTRAINT [ck_historial_estado_nuevo] CHECK (estado_nuevo IN ('Enviada', 'Vista', 'EnRevision', 'Entrevista', 'OfertaContratado', 'NoSeleccionado')),
        CONSTRAINT [fk_historial_estado_postulacion] FOREIGN KEY ([postulacion_id]) REFERENCES [postulaciones].[postulacion] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE UNIQUE INDEX [uq_historial_estado_postulacion_secuencia] ON [postulaciones].[historial_estado] ([postulacion_id], [secuencia]);
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE INDEX [ix_postulacion_vacante_coincidencia] ON [postulaciones].[postulacion] ([vacante_id], [coincidencia_pct] DESC, [deseables_cumplidas] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE UNIQUE INDEX [uq_postulacion_candidato_vacante] ON [postulaciones].[postulacion] ([candidato_id], [vacante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE UNIQUE INDEX [uq_postulacion_folio] ON [postulaciones].[postulacion] ([folio]);
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    CREATE INDEX [ix_vacante_replica_empresa] ON [postulaciones].[vacante_replica] ([empresa_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [postulaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126044511_InitialCreate_Postulaciones'
)
BEGIN
    INSERT INTO [postulaciones].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126044511_InitialCreate_Postulaciones', N'10.0.15');
END;

COMMIT;
GO

