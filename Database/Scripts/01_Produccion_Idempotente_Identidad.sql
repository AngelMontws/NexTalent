IF OBJECT_ID(N'[identidad].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'identidad') IS NULL EXEC(N'CREATE SCHEMA [identidad];');
    CREATE TABLE [identidad].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    IF SCHEMA_ID(N'identidad') IS NULL EXEC(N'CREATE SCHEMA [identidad];');
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[empresa] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [nombre] nvarchar(160) NOT NULL,
        [rfc] varchar(13) NOT NULL,
        [verificada] bit NOT NULL DEFAULT CAST(0 AS bit),
        [estado_verificacion] varchar(20) NOT NULL DEFAULT 'Pendiente',
        [creada_en] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_empresa] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Empresa_EstadoVerificacion] CHECK ([estado_verificacion] IN ('Pendiente','Aprobada','Rechazada')),
        CONSTRAINT [CK_Empresa_Rfc_Longitud] CHECK (LEN([rfc]) IN (12,13))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[usuario] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [email] nvarchar(256) NOT NULL,
        [password_hash] nvarchar(512) NOT NULL,
        [telefono_whatsapp] varchar(16) NULL,
        [rol] varchar(20) NOT NULL,
        [whatsapp_opt_in] bit NOT NULL DEFAULT CAST(0 AS bit),
        [whatsapp_opt_in_fecha] datetime2(0) NULL,
        [aviso_privacidad_version] varchar(20) NOT NULL,
        [aviso_privacidad_aceptado_en] datetime2(0) NOT NULL,
        [email_verificado] bit NOT NULL DEFAULT CAST(0 AS bit),
        [intentos_fallidos] int NOT NULL DEFAULT 0,
        [bloqueado_hasta] datetime2(0) NULL,
        [creado_en] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_usuario] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Usuario_OptIn_Fecha] CHECK ([whatsapp_opt_in] = 0 OR [whatsapp_opt_in_fecha] IS NOT NULL),
        CONSTRAINT [CK_Usuario_Rol] CHECK ([rol] IN ('Candidato','Reclutador','Administrador'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[candidato] (
        [usuario_id] uniqueidentifier NOT NULL,
        [nombres] nvarchar(100) NOT NULL,
        [apellido_paterno] nvarchar(80) NOT NULL,
        [apellido_materno] nvarchar(80) NULL,
        [edad] tinyint NULL,
        [ocupacion] nvarchar(120) NULL,
        [curp] char(18) NULL,
        [calle] nvarchar(120) NULL,
        [numero_exterior] nvarchar(20) NULL,
        [colonia] nvarchar(100) NULL,
        [municipio_alcaldia] nvarchar(100) NULL,
        [estado_mx] nvarchar(60) NULL,
        [codigo_postal] char(5) NULL,
        [ubicacion] nvarchar(120) NULL,
        [salario_esperado] decimal(12,2) NULL,
        [modalidad_preferida] varchar(20) NULL,
        [foto_url] nvarchar(500) NULL,
        [cv_perfil_url] nvarchar(500) NULL,
        [completitud_pct] tinyint NOT NULL DEFAULT CAST(0 AS tinyint),
        CONSTRAINT [PK_candidato] PRIMARY KEY ([usuario_id]),
        CONSTRAINT [CK_Candidato_CodigoPostal] CHECK ([codigo_postal] IS NULL OR [codigo_postal] LIKE '[0-9][0-9][0-9][0-9][0-9]'),
        CONSTRAINT [CK_Candidato_Completitud] CHECK ([completitud_pct] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Candidato_Edad] CHECK ([edad] IS NULL OR [edad] BETWEEN 16 AND 99),
        CONSTRAINT [CK_Candidato_Modalidad] CHECK ([modalidad_preferida] IS NULL OR [modalidad_preferida] IN ('Presencial','Hibrido','Remoto')),
        CONSTRAINT [CK_Candidato_SalarioEsperado] CHECK ([salario_esperado] IS NULL OR [salario_esperado] > 0),
        CONSTRAINT [FK_candidato_usuario_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [identidad].[usuario] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[candidato_habilidad] (
        [candidato_id] uniqueidentifier NOT NULL,
        [habilidad_id] uniqueidentifier NOT NULL,
        [nivel] tinyint NOT NULL,
        CONSTRAINT [PK_candidato_habilidad] PRIMARY KEY ([candidato_id], [habilidad_id]),
        CONSTRAINT [CK_CandidatoHabilidad_Nivel] CHECK ([nivel] IN (1,2,3)),
        CONSTRAINT [FK_candidato_habilidad_candidato_candidato_id] FOREIGN KEY ([candidato_id]) REFERENCES [identidad].[candidato] ([usuario_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Candidato_Curp] ON [identidad].[candidato] ([curp]) WHERE [curp] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE INDEX [IX_CandidatoHabilidad_Habilidad] ON [identidad].[candidato_habilidad] ([habilidad_id], [nivel]) INCLUDE ([candidato_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE INDEX [IX_Empresa_ColaFifo] ON [identidad].[empresa] ([estado_verificacion], [creada_en]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Empresa_Rfc] ON [identidad].[empresa] ([rfc]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Usuario_Email] ON [identidad].[usuario] ([email]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Usuario_Telefono] ON [identidad].[usuario] ([telefono_whatsapp]) WHERE [telefono_whatsapp] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126060819_InitialCreate_Identidad'
)
BEGIN
    INSERT INTO [identidad].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126060819_InitialCreate_Identidad', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[bitacora_moderacion_empresa] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [empresa_id] uniqueidentifier NOT NULL,
        [administrador_id] uniqueidentifier NOT NULL,
        [decision] varchar(20) NOT NULL,
        [motivo] nvarchar(500) NOT NULL,
        [fecha] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_bitacora_moderacion_empresa] PRIMARY KEY ([id]),
        CONSTRAINT [CK_BitacoraEmpresa_Decision] CHECK ([decision] IN ('Aprobada','Rechazada')),
        CONSTRAINT [CK_BitacoraEmpresa_Motivo] CHECK (LEN(LTRIM(RTRIM([motivo]))) >= 5),
        CONSTRAINT [FK_bitacora_moderacion_empresa_empresa_empresa_id] FOREIGN KEY ([empresa_id]) REFERENCES [identidad].[empresa] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_bitacora_moderacion_empresa_usuario_administrador_id] FOREIGN KEY ([administrador_id]) REFERENCES [identidad].[usuario] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[experiencia_laboral] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [candidato_id] uniqueidentifier NOT NULL,
        [puesto] nvarchar(120) NOT NULL,
        [empresa_nombre] nvarchar(160) NOT NULL,
        [descripcion] nvarchar(1000) NULL,
        [fecha_inicio] date NOT NULL,
        [fecha_fin] date NULL,
        [es_actual] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_experiencia_laboral] PRIMARY KEY ([id]),
        CONSTRAINT [CK_ExperienciaLaboral_Fechas] CHECK ([es_actual] = 1 OR ([fecha_fin] IS NOT NULL AND [fecha_fin] >= [fecha_inicio])),
        CONSTRAINT [FK_experiencia_laboral_candidato_candidato_id] FOREIGN KEY ([candidato_id]) REFERENCES [identidad].[candidato] ([usuario_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[formacion_academica] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [candidato_id] uniqueidentifier NOT NULL,
        [institucion] nvarchar(160) NOT NULL,
        [titulo_grado] nvarchar(160) NOT NULL,
        [fecha_inicio] date NOT NULL,
        [fecha_fin] date NULL,
        [es_actual] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_formacion_academica] PRIMARY KEY ([id]),
        CONSTRAINT [CK_FormacionAcademica_Fechas] CHECK ([es_actual] = 1 OR ([fecha_fin] IS NOT NULL AND [fecha_fin] >= [fecha_inicio])),
        CONSTRAINT [FK_formacion_academica_candidato_candidato_id] FOREIGN KEY ([candidato_id]) REFERENCES [identidad].[candidato] ([usuario_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[reclutador] (
        [usuario_id] uniqueidentifier NOT NULL,
        [empresa_id] uniqueidentifier NOT NULL,
        [nombre_contacto] nvarchar(120) NOT NULL,
        [aprobado_empresa] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_reclutador] PRIMARY KEY ([usuario_id]),
        CONSTRAINT [FK_reclutador_empresa_empresa_id] FOREIGN KEY ([empresa_id]) REFERENCES [identidad].[empresa] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_reclutador_usuario_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [identidad].[usuario] ([id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE TABLE [identidad].[refresh_token] (
        [id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [usuario_id] uniqueidentifier NOT NULL,
        [token_hash] varchar(128) NOT NULL,
        [expira_en] datetime2(0) NOT NULL,
        [revocado_en] datetime2(0) NULL,
        CONSTRAINT [PK_refresh_token] PRIMARY KEY ([id]),
        CONSTRAINT [FK_refresh_token_usuario_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [identidad].[usuario] ([id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE INDEX [IX_bitacora_moderacion_empresa_administrador_id] ON [identidad].[bitacora_moderacion_empresa] ([administrador_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE INDEX [IX_BitacoraEmpresa_Empresa_Fecha] ON [identidad].[bitacora_moderacion_empresa] ([empresa_id], [fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE INDEX [IX_ExperienciaLaboral_Candidato] ON [identidad].[experiencia_laboral] ([candidato_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE INDEX [IX_FormacionAcademica_Candidato] ON [identidad].[formacion_academica] ([candidato_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE INDEX [IX_Reclutador_Empresa] ON [identidad].[reclutador] ([empresa_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE INDEX [IX_RefreshToken_Usuario_Expira] ON [identidad].[refresh_token] ([usuario_id], [expira_en]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_RefreshToken_Hash] ON [identidad].[refresh_token] ([token_hash]);
END;

IF NOT EXISTS (
    SELECT * FROM [identidad].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126220239_AgregaTablasComplementarias_Identidad'
)
BEGIN
    INSERT INTO [identidad].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126220239_AgregaTablasComplementarias_Identidad', N'10.0.15');
END;

COMMIT;
GO

