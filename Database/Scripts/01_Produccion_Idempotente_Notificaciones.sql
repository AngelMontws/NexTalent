IF OBJECT_ID(N'[notificaciones].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'notificaciones') IS NULL EXEC(N'CREATE SCHEMA [notificaciones];');
    CREATE TABLE [notificaciones].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    IF SCHEMA_ID(N'notificaciones') IS NULL EXEC(N'CREATE SCHEMA [notificaciones];');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[alerta_vacante_enviada] (
        [usuario_id] uniqueidentifier NOT NULL,
        [vacante_id] uniqueidentifier NOT NULL,
        [id_alerta] varchar(128) NULL,
        [boton] varchar(30) NULL,
        [enviada_en] datetime2(0) NOT NULL,
        CONSTRAINT [pk_alerta_vacante_enviada] PRIMARY KEY ([usuario_id], [vacante_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[codigo_whatsapp] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [usuario_id] uniqueidentifier NOT NULL,
        [telefono] varchar(20) NULL,
        [codigo_hash] varbinary(32) NULL,
        [enviado_en] datetime2(0) NULL,
        [expira_en] datetime2(0) NULL,
        [intentos] int NOT NULL DEFAULT 0,
        [bloqueado_hasta] datetime2(0) NULL,
        [usado_en] datetime2(0) NULL,
        CONSTRAINT [pk_codigo_whatsapp] PRIMARY KEY ([id]),
        CONSTRAINT [ck_codigo_whatsapp_intentos] CHECK (intentos >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[contacto_replica] (
        [usuario_id] uniqueidentifier NOT NULL,
        [rol] varchar(20) NOT NULL,
        [empresa_id] uniqueidentifier NULL,
        [email] nvarchar(256) NOT NULL,
        [telefono_whatsapp] varchar(20) NULL,
        [whatsapp_user_id] varchar(128) NULL,
        [whatsapp_opt_in] bit NOT NULL DEFAULT CAST(0 AS bit),
        [whatsapp_opt_in_fecha] datetime2(0) NULL,
        [whatsapp_baja_fecha] datetime2(0) NULL,
        [frecuencia_alertas] varchar(15) NOT NULL DEFAULT 'Ninguna',
        [version_evento] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        CONSTRAINT [pk_contacto_replica] PRIMARY KEY ([usuario_id]),
        CONSTRAINT [ck_contacto_replica_frecuencia] CHECK (frecuencia_alertas IN ('Diaria', 'Semanal', 'Ninguna')),
        CONSTRAINT [ck_contacto_replica_opt_in_fecha] CHECK (whatsapp_opt_in = 0 OR whatsapp_opt_in_fecha IS NOT NULL),
        CONSTRAINT [ck_contacto_replica_rol] CHECK (rol IN ('Candidato', 'Reclutador', 'Administrador'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[InboxState] (
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
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[mensaje_whatsapp_procesado] (
        [mensaje_id] varchar(128) NOT NULL,
        [recibido_en] datetime2(0) NOT NULL,
        [resultado] varchar(30) NOT NULL,
        CONSTRAINT [pk_mensaje_whatsapp_procesado] PRIMARY KEY ([mensaje_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[notificacion] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [usuario_id] uniqueidentifier NOT NULL,
        [canal] varchar(15) NOT NULL,
        [plantilla] varchar(50) NOT NULL,
        [estado_envio] varchar(20) NOT NULL,
        [intentos] int NOT NULL,
        [creada_en] datetime2(0) NOT NULL,
        [evento_id] uniqueidentifier NULL,
        [postulacion_id] uniqueidentifier NULL,
        [vacante_id] uniqueidentifier NULL,
        [empresa_id] uniqueidentifier NULL,
        [referencia_folio] varchar(20) NULL,
        [titulo_vacante] nvarchar(200) NULL,
        [nombre_empresa] nvarchar(200) NULL,
        [estado_anterior] varchar(25) NULL,
        [estado_nuevo] varchar(25) NULL,
        [causa_cambio] varchar(25) NULL,
        [cambiado_en] datetime2(0) NULL,
        [secuencia] int NULL,
        [proximo_intento_en] datetime2(0) NULL,
        [enviada_en] datetime2(0) NULL,
        [id_externo] varchar(128) NULL,
        [ultimo_error] nvarchar(500) NULL,
        CONSTRAINT [pk_notificacion] PRIMARY KEY ([id]),
        CONSTRAINT [ck_notificacion_canal] CHECK (canal IN ('Web', 'Whatsapp', 'Email')),
        CONSTRAINT [ck_notificacion_estado_envio] CHECK (estado_envio IN ('Pendiente', 'Enviada', 'Fallida')),
        CONSTRAINT [ck_notificacion_intentos] CHECK (intentos >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[OutboxState] (
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
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[postulacion_seguimiento] (
        [postulacion_id] uniqueidentifier NOT NULL,
        [candidato_id] uniqueidentifier NOT NULL,
        [vacante_id] uniqueidentifier NOT NULL,
        [empresa_id] uniqueidentifier NOT NULL,
        [folio] varchar(20) NOT NULL,
        [titulo_vacante] nvarchar(200) NOT NULL,
        [estado_actual] varchar(25) NOT NULL,
        [pendiente] bit NOT NULL,
        [ultimo_cambio_en] datetime2(0) NOT NULL,
        [secuencia] int NOT NULL,
        [recordatorio_10d_enviado] bit NOT NULL DEFAULT CAST(0 AS bit),
        [recordatorio_enviado_en] datetime2(0) NULL,
        CONSTRAINT [pk_postulacion_seguimiento] PRIMARY KEY ([postulacion_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE TABLE [notificaciones].[OutboxMessage] (
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
        CONSTRAINT [FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId] FOREIGN KEY ([InboxMessageId], [InboxConsumerId]) REFERENCES [notificaciones].[InboxState] ([MessageId], [ConsumerId]),
        CONSTRAINT [FK_OutboxMessage_OutboxState_OutboxId] FOREIGN KEY ([OutboxId]) REFERENCES [notificaciones].[OutboxState] ([OutboxId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE INDEX [ix_alerta_vacante_enviada_alerta_boton] ON [notificaciones].[alerta_vacante_enviada] ([usuario_id], [id_alerta], [boton]) WHERE [id_alerta] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE UNIQUE INDEX [uq_codigo_whatsapp_usuario] ON [notificaciones].[codigo_whatsapp] ([usuario_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [ix_contacto_replica_empresa] ON [notificaciones].[contacto_replica] ([empresa_id], [rol]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [uq_contacto_replica_bsuid] ON [notificaciones].[contacto_replica] ([whatsapp_user_id]) WHERE [whatsapp_user_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [uq_contacto_replica_telefono] ON [notificaciones].[contacto_replica] ([telefono_whatsapp]) WHERE [telefono_whatsapp] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [IX_InboxState_Delivered] ON [notificaciones].[InboxState] ([Delivered]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE INDEX [ix_notificacion_postulacion_secuencia] ON [notificaciones].[notificacion] ([postulacion_id], [secuencia]) WHERE [postulacion_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [ix_notificacion_reintentos] ON [notificaciones].[notificacion] ([estado_envio], [proximo_intento_en]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [uq_notificacion_evento_usuario_canal] ON [notificaciones].[notificacion] ([evento_id], [usuario_id], [canal]) WHERE [evento_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [IX_OutboxMessage_EnqueueTime] ON [notificaciones].[OutboxMessage] ([EnqueueTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [IX_OutboxMessage_ExpirationTime] ON [notificaciones].[OutboxMessage] ([ExpirationTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber] ON [notificaciones].[OutboxMessage] ([InboxMessageId], [InboxConsumerId], [SequenceNumber]) WHERE [InboxMessageId] IS NOT NULL AND [InboxConsumerId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OutboxMessage_OutboxId_SequenceNumber] ON [notificaciones].[OutboxMessage] ([OutboxId], [SequenceNumber]) WHERE [OutboxId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [IX_OutboxState_Created] ON [notificaciones].[OutboxState] ([Created]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [ix_postulacion_seguimiento_pendientes] ON [notificaciones].[postulacion_seguimiento] ([pendiente], [recordatorio_10d_enviado], [ultimo_cambio_en]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    CREATE INDEX [ix_postulacion_seguimiento_vacante] ON [notificaciones].[postulacion_seguimiento] ([vacante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220249_InitialCreate_Notificaciones'
)
BEGIN
    INSERT INTO [notificaciones].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126220249_InitialCreate_Notificaciones', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127052505_AgregaLeidaEn_Notificaciones'
)
BEGIN
    ALTER TABLE [notificaciones].[notificacion] ADD [leida_en] datetime2(0) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [notificaciones].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127052505_AgregaLeidaEn_Notificaciones'
)
BEGIN
    INSERT INTO [notificaciones].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127052505_AgregaLeidaEn_Notificaciones', N'10.0.15');
END;

COMMIT;
GO

