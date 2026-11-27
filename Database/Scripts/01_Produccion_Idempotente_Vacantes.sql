IF OBJECT_ID(N'[vacantes].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'vacantes') IS NULL EXEC(N'CREATE SCHEMA [vacantes];');
    CREATE TABLE [vacantes].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    IF SCHEMA_ID(N'vacantes') IS NULL EXEC(N'CREATE SCHEMA [vacantes];');
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[bitacora_moderacion] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [entidad_tipo] varchar(20) NOT NULL,
        [entidad_id] uniqueidentifier NOT NULL,
        [administrador_id] uniqueidentifier NOT NULL,
        [accion] varchar(30) NOT NULL,
        [motivo] nvarchar(500) NOT NULL,
        [fecha] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_bitacora_moderacion] PRIMARY KEY ([id]),
        CONSTRAINT [CK_BitacoraModeracion_Entidad] CHECK ([entidad_tipo] IN ('Vacante','Habilidad')),
        CONSTRAINT [CK_BitacoraModeracion_Motivo] CHECK (LEN(LTRIM(RTRIM([motivo]))) >= 5)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[empresa_resumen] (
        [empresa_id] uniqueidentifier NOT NULL,
        [nombre] nvarchar(160) NOT NULL,
        [verificada] bit NOT NULL DEFAULT CAST(0 AS bit),
        [indice_respuesta_pct] decimal(5,2) NOT NULL DEFAULT 0.0,
        [actualizado_en] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_empresa_resumen] PRIMARY KEY ([empresa_id]),
        CONSTRAINT [CK_EmpresaResumen_IndiceRespuesta] CHECK ([indice_respuesta_pct] BETWEEN 0 AND 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[habilidad] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [nombre] nvarchar(80) NOT NULL,
        [categoria] nvarchar(60) NOT NULL,
        [estado_aprobacion] varchar(20) NOT NULL DEFAULT 'Pendiente',
        [propuesta_por] uniqueidentifier NULL,
        CONSTRAINT [PK_habilidad] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Habilidad_EstadoAprobacion] CHECK ([estado_aprobacion] IN ('Aprobada','Pendiente','Rechazada'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[InboxState] (
        [Id] bigint NOT NULL IDENTITY,
        [MessageId] uniqueidentifier NOT NULL,
        [ConsumerId] uniqueidentifier NOT NULL,
        [LockId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NULL,
        [Received] datetime2 NOT NULL,
        [ReceiveCount] int NOT NULL,
        [ExpirationTime] datetime2 NULL,
        [Consumed] datetime2 NULL,
        [Delivered] datetime2 NULL,
        [LastSequenceNumber] bigint NULL,
        CONSTRAINT [PK_InboxState] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_InboxState_MessageId_ConsumerId] UNIQUE ([MessageId], [ConsumerId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[OutboxState] (
        [OutboxId] uniqueidentifier NOT NULL,
        [LockId] uniqueidentifier NOT NULL,
        [RowVersion] rowversion NULL,
        [Created] datetime2 NOT NULL,
        [Delivered] datetime2 NULL,
        [LastSequenceNumber] bigint NULL,
        CONSTRAINT [PK_OutboxState] PRIMARY KEY ([OutboxId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[vacante] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [empresa_id] uniqueidentifier NOT NULL,
        [reclutador_id] uniqueidentifier NOT NULL,
        [titulo] nvarchar(150) NOT NULL,
        [descripcion] nvarchar(max) NOT NULL,
        [actividades] nvarchar(2000) NULL,
        [beneficios] nvarchar(2000) NULL,
        [salario_min] decimal(12,2) NOT NULL,
        [salario_max] decimal(12,2) NOT NULL,
        [tipo_salario] varchar(10) NOT NULL,
        [modalidad] varchar(20) NOT NULL,
        [jornada] varchar(30) NOT NULL,
        [ubicacion] nvarchar(120) NOT NULL,
        [estado] varchar(25) NOT NULL DEFAULT 'Borrador',
        [publicada_en] datetime2(0) NULL,
        [expira_en] datetime2(0) NULL,
        [recordatorio_27d_enviado] bit NOT NULL DEFAULT CAST(0 AS bit),
        [version_evento] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        [creada_en] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_vacante] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Vacante_Estado] CHECK ([estado] IN ('Borrador','Publicada','OcultaPorReportes','Cerrada','Caducada')),
        CONSTRAINT [CK_Vacante_Modalidad] CHECK ([modalidad] IN ('Presencial','Hibrido','Remoto')),
        CONSTRAINT [CK_Vacante_SalarioMax_Valido] CHECK ([salario_max] >= [salario_min]),
        CONSTRAINT [CK_Vacante_SalarioMin_Positivo] CHECK ([salario_min] > 0),
        CONSTRAINT [CK_Vacante_TipoSalario] CHECK ([tipo_salario] IN ('Bruto','Neto')),
        CONSTRAINT [FK_vacante_empresa_resumen_empresa_id] FOREIGN KEY ([empresa_id]) REFERENCES [vacantes].[empresa_resumen] ([empresa_id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[OutboxMessage] (
        [SequenceNumber] bigint NOT NULL IDENTITY,
        [EnqueueTime] datetime2 NULL,
        [SentTime] datetime2 NOT NULL,
        [Headers] nvarchar(max) NULL,
        [Properties] nvarchar(max) NULL,
        [InboxMessageId] uniqueidentifier NULL,
        [InboxConsumerId] uniqueidentifier NULL,
        [OutboxId] uniqueidentifier NULL,
        [MessageId] uniqueidentifier NOT NULL,
        [ContentType] nvarchar(256) NOT NULL,
        [MessageType] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [ConversationId] uniqueidentifier NULL,
        [CorrelationId] uniqueidentifier NULL,
        [InitiatorId] uniqueidentifier NULL,
        [RequestId] uniqueidentifier NULL,
        [SourceAddress] nvarchar(256) NULL,
        [DestinationAddress] nvarchar(256) NULL,
        [ResponseAddress] nvarchar(256) NULL,
        [FaultAddress] nvarchar(256) NULL,
        [ExpirationTime] datetime2 NULL,
        CONSTRAINT [PK_OutboxMessage] PRIMARY KEY ([SequenceNumber]),
        CONSTRAINT [FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId] FOREIGN KEY ([InboxMessageId], [InboxConsumerId]) REFERENCES [vacantes].[InboxState] ([MessageId], [ConsumerId]),
        CONSTRAINT [FK_OutboxMessage_OutboxState_OutboxId] FOREIGN KEY ([OutboxId]) REFERENCES [vacantes].[OutboxState] ([OutboxId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[reporte_vacante] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [vacante_id] uniqueidentifier NOT NULL,
        [usuario_id] uniqueidentifier NOT NULL,
        [motivo_tipo] varchar(30) NOT NULL,
        [detalle] nvarchar(500) NULL,
        [fecha] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_reporte_vacante] PRIMARY KEY ([id]),
        CONSTRAINT [CK_ReporteVacante_Motivo] CHECK ([motivo_tipo] IN ('PideDinero','VacanteFalsa','Discriminatoria','Otro')),
        CONSTRAINT [FK_reporte_vacante_vacante_vacante_id] FOREIGN KEY ([vacante_id]) REFERENCES [vacantes].[vacante] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE TABLE [vacantes].[vacante_habilidad] (
        [vacante_id] uniqueidentifier NOT NULL,
        [habilidad_id] uniqueidentifier NOT NULL,
        [nivel] tinyint NOT NULL,
        [es_requerida] bit NOT NULL,
        CONSTRAINT [PK_vacante_habilidad] PRIMARY KEY ([vacante_id], [habilidad_id]),
        CONSTRAINT [CK_VacanteHabilidad_Nivel] CHECK ([nivel] IN (1,2,3)),
        CONSTRAINT [FK_vacante_habilidad_habilidad_habilidad_id] FOREIGN KEY ([habilidad_id]) REFERENCES [vacantes].[habilidad] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_vacante_habilidad_vacante_vacante_id] FOREIGN KEY ([vacante_id]) REFERENCES [vacantes].[vacante] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_BitacoraModeracion_Entidad] ON [vacantes].[bitacora_moderacion] ([entidad_tipo], [entidad_id], [fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Habilidad_Nombre] ON [vacantes].[habilidad] ([nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_InboxState_Delivered] ON [vacantes].[InboxState] ([Delivered]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_OutboxMessage_EnqueueTime] ON [vacantes].[OutboxMessage] ([EnqueueTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_OutboxMessage_ExpirationTime] ON [vacantes].[OutboxMessage] ([ExpirationTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber] ON [vacantes].[OutboxMessage] ([InboxMessageId], [InboxConsumerId], [SequenceNumber]) WHERE [InboxMessageId] IS NOT NULL AND [InboxConsumerId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OutboxMessage_OutboxId_SequenceNumber] ON [vacantes].[OutboxMessage] ([OutboxId], [SequenceNumber]) WHERE [OutboxId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_OutboxState_Created] ON [vacantes].[OutboxState] ([Created]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_ReporteVacante_Vacante_Usuario] ON [vacantes].[reporte_vacante] ([vacante_id], [usuario_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_Vacante_Busqueda] ON [vacantes].[vacante] ([estado], [salario_min], [salario_max]) INCLUDE ([titulo], [empresa_id], [modalidad], [jornada], [ubicacion], [tipo_salario], [publicada_en], [expira_en]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_Vacante_Empresa_Estado] ON [vacantes].[vacante] ([empresa_id], [estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_Vacante_Estado_Expira] ON [vacantes].[vacante] ([estado], [expira_en]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    CREATE INDEX [IX_VacanteHabilidad_Habilidad] ON [vacantes].[vacante_habilidad] ([habilidad_id], [nivel]) INCLUDE ([vacante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [vacantes].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220244_InitialCreate_Vacantes'
)
BEGIN
    INSERT INTO [vacantes].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126220244_InitialCreate_Vacantes', N'10.0.15');
END;

COMMIT;
GO

