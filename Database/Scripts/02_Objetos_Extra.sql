/*
 * NexTalent — Database/Scripts/02_Objetos_Extra.sql
 * Objetos avanzados de SQL Server (idempotente):
 *   1. Índices covering y de tablas puente para RNF-01 (< 200 ms sobre 50,000 vacantes).
 *   2. Catálogo e índice Full-Text Search (ftc_NexTalent, LANGUAGE 3082) sobre vacantes.vacante.
 *   3. Tipo de tabla (TVP) vacantes.FiltroHabilidadTipo y procedimiento vacantes.sp_BuscarVacantes.
 *   4. Vista materializada y vista postulaciones.vw_IndiceRespuestaEmpresa (excluye cierres automáticos).
 *   5. Triggers de inmutabilidad (THROW 51000) en historial_estado y bitácoras de moderación.
 *   6. Logins, usuarios por esquema y matriz GRANT / DENY de mínimo privilegio.
 *
 * Ejecución recomendada (siempre con bandera -I para QUOTED_IDENTIFIER ON):
 *   sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -I -d NexTalentDb -i Database/Scripts/02_Objetos_Extra.sql
 */

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF SCHEMA_ID(N'identidad') IS NULL
   OR SCHEMA_ID(N'vacantes') IS NULL
   OR SCHEMA_ID(N'postulaciones') IS NULL
   OR SCHEMA_ID(N'notificaciones') IS NULL
BEGIN
    THROW 50001, N'Ejecute primero 00_Crear_Esquemas.sql y los scripts 01_Produccion_Idempotente_*.sql sobre la base destino.', 1;
END;
GO

IF SCHEMA_ID(N'HangFire') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [HangFire]');
END;
GO

/* ==========================================================================================
 * 1. ÍNDICES DE RENDIMIENTO RNF-01 (IDEMPOTENTES)
 * ========================================================================================== */
IF OBJECT_ID(N'[vacantes].[vacante]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'[vacantes].[vacante]')
         AND name = N'IX_Vacante_Busqueda'
   )
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Vacante_Busqueda]
        ON [vacantes].[vacante] ([estado], [salario_min], [salario_max])
        INCLUDE ([titulo], [empresa_id], [modalidad], [jornada], [ubicacion], [tipo_salario], [publicada_en], [expira_en]);
END;
GO

IF OBJECT_ID(N'[vacantes].[vacante_habilidad]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'[vacantes].[vacante_habilidad]')
         AND name = N'IX_VacanteHabilidad_Habilidad'
   )
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VacanteHabilidad_Habilidad]
        ON [vacantes].[vacante_habilidad] ([habilidad_id], [nivel])
        INCLUDE ([vacante_id]);
END;
GO

IF OBJECT_ID(N'[identidad].[candidato_habilidad]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'[identidad].[candidato_habilidad]')
         AND name = N'IX_CandidatoHabilidad_Habilidad'
   )
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CandidatoHabilidad_Habilidad]
        ON [identidad].[candidato_habilidad] ([habilidad_id], [nivel])
        INCLUDE ([candidato_id]);
END;
GO

/* ==========================================================================================
 * 2. FULL-TEXT SEARCH (HU-04.01, INSENSIBLE A ACENTOS, LANGUAGE 3082 ESPAÑOL)
 * ========================================================================================== */
IF SERVERPROPERTY('IsFullTextInstalled') = 1
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftc_NexTalent')
    BEGIN
        CREATE FULLTEXT CATALOG [ftc_NexTalent] WITH ACCENT_SENSITIVITY = OFF;
    END;

    IF OBJECT_ID(N'[vacantes].[vacante]', N'U') IS NOT NULL
       AND OBJECTPROPERTY(OBJECT_ID(N'[vacantes].[vacante]'), 'TableHasFulltextIndex') = 0
    BEGIN
        DECLARE @pk_vacante sysname;
        SELECT @pk_vacante = i.name
        FROM sys.indexes AS i
        WHERE i.object_id = OBJECT_ID(N'[vacantes].[vacante]')
          AND i.is_primary_key = 1;

        IF @pk_vacante IS NOT NULL
        BEGIN
            DECLARE @sql_fts nvarchar(max) =
                N'CREATE FULLTEXT INDEX ON [vacantes].[vacante] ([titulo] LANGUAGE 3082, [descripcion] LANGUAGE 3082) '
                + N'KEY INDEX ' + QUOTENAME(@pk_vacante)
                + N' ON [ftc_NexTalent] WITH CHANGE_TRACKING AUTO;';
            EXEC(@sql_fts);
        END;
    END;
END
ELSE
BEGIN
    PRINT N'Aviso: Full-Text Search no está instalado en esta instancia SQL Server; sp_BuscarVacantes usará respaldo CI_AI.';
END;
GO

/* ==========================================================================================
 * 3. TVP Y PROCEDIMIENTO ALMACENADO vacantes.sp_BuscarVacantes (RNF-01 + DAPPER)
 * ========================================================================================== */
IF TYPE_ID(N'[vacantes].[FiltroHabilidadTipo]') IS NULL
BEGIN
    CREATE TYPE [vacantes].[FiltroHabilidadTipo] AS TABLE
    (
        [habilidad_id] uniqueidentifier NOT NULL PRIMARY KEY,
        [nivel_minimo] tinyint NULL
    );
END;
GO

IF OBJECT_ID(N'[vacantes].[vacante]', N'U') IS NOT NULL
   AND OBJECTPROPERTY(OBJECT_ID(N'[vacantes].[vacante]'), 'TableHasFulltextIndex') = 1
BEGIN
    EXEC(N'
CREATE OR ALTER PROCEDURE [vacantes].[sp_BuscarVacantes]
    @texto nvarchar(200) = NULL,
    @salario_min decimal(12,2) = NULL,
    @modalidad varchar(20) = NULL,
    @ubicacion nvarchar(120) = NULL,
    @jornada varchar(30) = NULL,
    @habilidades [vacantes].[FiltroHabilidadTipo] READONLY,
    @pagina int = 1,
    @tamano_pagina int = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @pagina IS NULL OR @pagina < 1 SET @pagina = 1;
    IF @tamano_pagina IS NULL OR @tamano_pagina < 1 SET @tamano_pagina = 20;
    IF @tamano_pagina > 50 SET @tamano_pagina = 50;

    DECLARE @offset int = (@pagina - 1) * @tamano_pagina;
    DECLARE @total_habilidades int = (SELECT COUNT(*) FROM @habilidades);
    DECLARE @texto_limpio nvarchar(200) = NULLIF(LTRIM(RTRIM(REPLACE(@texto, N''"'', N''''))), N'''');
    DECLARE @fts_query nvarchar(250) = CASE
        WHEN @texto_limpio IS NULL THEN NULL
        ELSE N''"'' + @texto_limpio + N''*"''
    END;

    SELECT
        v.[id] AS [Id],
        v.[titulo] AS [Titulo],
        v.[empresa_id] AS [EmpresaId],
        er.[nombre] AS [NombreEmpresa],
        er.[verificada] AS [EmpresaVerificada],
        er.[indice_respuesta_pct] AS [IndiceRespuestaPct],
        v.[salario_min] AS [SalarioMin],
        v.[salario_max] AS [SalarioMax],
        v.[tipo_salario] AS [TipoSalario],
        v.[modalidad] AS [Modalidad],
        v.[jornada] AS [Jornada],
        v.[ubicacion] AS [Ubicacion],
        v.[publicada_en] AS [PublicadaEn],
        v.[expira_en] AS [ExpiraEn],
        COUNT(*) OVER() AS [TotalResultados]
    FROM [vacantes].[vacante] AS v
    INNER JOIN [vacantes].[empresa_resumen] AS er
        ON er.[empresa_id] = v.[empresa_id]
    WHERE v.[estado] = ''Publicada''
      AND (v.[expira_en] IS NULL OR v.[expira_en] > SYSUTCDATETIME())
      AND (@salario_min IS NULL OR v.[salario_max] >= @salario_min)
      AND (@modalidad IS NULL OR v.[modalidad] = @modalidad)
      AND (@jornada IS NULL OR v.[jornada] = @jornada)
      AND (@ubicacion IS NULL OR v.[ubicacion] COLLATE Latin1_General_CI_AI LIKE N''%'' + @ubicacion + N''%'')
      AND (@fts_query IS NULL OR CONTAINS((v.[titulo], v.[descripcion]), @fts_query, LANGUAGE 3082))
      AND (
          @total_habilidades = 0
          OR (
              SELECT COUNT(*)
              FROM @habilidades AS fh
              INNER JOIN [vacantes].[vacante_habilidad] AS vh
                  ON vh.[vacante_id] = v.[id]
                 AND vh.[habilidad_id] = fh.[habilidad_id]
              WHERE fh.[nivel_minimo] IS NULL OR vh.[nivel] >= fh.[nivel_minimo]
          ) = @total_habilidades
      )
    ORDER BY v.[publicada_en] DESC, v.[id] DESC
    OFFSET @offset ROWS FETCH NEXT @tamano_pagina ROWS ONLY;
END;
');
END
ELSE IF OBJECT_ID(N'[vacantes].[vacante]', N'U') IS NOT NULL
BEGIN
    EXEC(N'
CREATE OR ALTER PROCEDURE [vacantes].[sp_BuscarVacantes]
    @texto nvarchar(200) = NULL,
    @salario_min decimal(12,2) = NULL,
    @modalidad varchar(20) = NULL,
    @ubicacion nvarchar(120) = NULL,
    @jornada varchar(30) = NULL,
    @habilidades [vacantes].[FiltroHabilidadTipo] READONLY,
    @pagina int = 1,
    @tamano_pagina int = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @pagina IS NULL OR @pagina < 1 SET @pagina = 1;
    IF @tamano_pagina IS NULL OR @tamano_pagina < 1 SET @tamano_pagina = 20;
    IF @tamano_pagina > 50 SET @tamano_pagina = 50;

    DECLARE @offset int = (@pagina - 1) * @tamano_pagina;
    DECLARE @total_habilidades int = (SELECT COUNT(*) FROM @habilidades);
    DECLARE @texto_limpio nvarchar(200) = NULLIF(LTRIM(RTRIM(@texto)), N'''');

    SELECT
        v.[id] AS [Id],
        v.[titulo] AS [Titulo],
        v.[empresa_id] AS [EmpresaId],
        er.[nombre] AS [NombreEmpresa],
        er.[verificada] AS [EmpresaVerificada],
        er.[indice_respuesta_pct] AS [IndiceRespuestaPct],
        v.[salario_min] AS [SalarioMin],
        v.[salario_max] AS [SalarioMax],
        v.[tipo_salario] AS [TipoSalario],
        v.[modalidad] AS [Modalidad],
        v.[jornada] AS [Jornada],
        v.[ubicacion] AS [Ubicacion],
        v.[publicada_en] AS [PublicadaEn],
        v.[expira_en] AS [ExpiraEn],
        COUNT(*) OVER() AS [TotalResultados]
    FROM [vacantes].[vacante] AS v
    INNER JOIN [vacantes].[empresa_resumen] AS er
        ON er.[empresa_id] = v.[empresa_id]
    WHERE v.[estado] = ''Publicada''
      AND (v.[expira_en] IS NULL OR v.[expira_en] > SYSUTCDATETIME())
      AND (@salario_min IS NULL OR v.[salario_max] >= @salario_min)
      AND (@modalidad IS NULL OR v.[modalidad] = @modalidad)
      AND (@jornada IS NULL OR v.[jornada] = @jornada)
      AND (@ubicacion IS NULL OR v.[ubicacion] COLLATE Latin1_General_CI_AI LIKE N''%'' + @ubicacion + N''%'')
      AND (
          @texto_limpio IS NULL
          OR v.[titulo] COLLATE Latin1_General_CI_AI LIKE N''%'' + @texto_limpio + N''%''
          OR v.[descripcion] COLLATE Latin1_General_CI_AI LIKE N''%'' + @texto_limpio + N''%''
      )
      AND (
          @total_habilidades = 0
          OR (
              SELECT COUNT(*)
              FROM @habilidades AS fh
              INNER JOIN [vacantes].[vacante_habilidad] AS vh
                  ON vh.[vacante_id] = v.[id]
                 AND vh.[habilidad_id] = fh.[habilidad_id]
              WHERE fh.[nivel_minimo] IS NULL OR vh.[nivel] >= fh.[nivel_minimo]
          ) = @total_habilidades
      )
    ORDER BY v.[publicada_en] DESC, v.[id] DESC
    OFFSET @offset ROWS FETCH NEXT @tamano_pagina ROWS ONLY;
END;
');
END;
GO

/* ==========================================================================================
 * 4. VISTA INDEXADA Y VISTA DE ÍNDICE DE RESPUESTA (Acuerdo Punto 14 / Sección 4.C)
 *    Excluye cierres automáticos del sistema (causa = 'VacanteCerrada' o cambiado_por IS NULL).
 * ========================================================================================== */
IF OBJECT_ID(N'[postulaciones].[vw_ConteoPostulacionesEmpresa]', N'V') IS NULL
BEGIN
    EXEC(N'
CREATE VIEW [postulaciones].[vw_ConteoPostulacionesEmpresa]
WITH SCHEMABINDING
AS
SELECT
    vr.[empresa_id],
    COUNT_BIG(*) AS [total_postulaciones],
    SUM(CASE WHEN p.[estado_actual] <> ''Enviada'' THEN CAST(1 AS bigint) ELSE CAST(0 AS bigint) END) AS [salieron_de_enviada]
FROM [postulaciones].[vacante_replica] AS vr
INNER JOIN [postulaciones].[postulacion] AS p
    ON p.[vacante_id] = vr.[vacante_id]
GROUP BY vr.[empresa_id];
');
END;
GO

IF OBJECT_ID(N'[postulaciones].[vw_ConteoPostulacionesEmpresa]', N'V') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'[postulaciones].[vw_ConteoPostulacionesEmpresa]')
         AND name = N'IX_vw_ConteoPostulacionesEmpresa'
   )
BEGIN
    CREATE UNIQUE CLUSTERED INDEX [IX_vw_ConteoPostulacionesEmpresa]
        ON [postulaciones].[vw_ConteoPostulacionesEmpresa] ([empresa_id]);
END;
GO

CREATE OR ALTER VIEW [postulaciones].[vw_IndiceRespuestaEmpresa]
WITH SCHEMABINDING
AS
SELECT
    vr.[empresa_id],
    COUNT_BIG(p.[id]) AS [total_postulaciones],
    SUM(ISNULL(calc.[es_respondida], CAST(0 AS bigint))) AS [postulaciones_respondidas],
    CAST(
        CASE
            WHEN COUNT_BIG(p.[id]) = 0 THEN 0.00
            ELSE ROUND(
                (SUM(CAST(ISNULL(calc.[es_respondida], 0) AS decimal(12,4))) * 100.0) / COUNT_BIG(p.[id]),
                2
            )
        END
        AS decimal(5,2)
    ) AS [indice_respuesta_pct]
FROM [postulaciones].[vacante_replica] AS vr
LEFT JOIN [postulaciones].[postulacion] AS p
    ON p.[vacante_id] = vr.[vacante_id]
OUTER APPLY (
    SELECT
        CASE
            WHEN p.[id] IS NULL OR p.[estado_actual] = 'Enviada' THEN CAST(0 AS bigint)
            WHEN p.[estado_actual] = 'NoSeleccionado'
                 AND EXISTS (
                     SELECT 1
                     FROM [postulaciones].[historial_estado] AS h
                     WHERE h.[postulacion_id] = p.[id]
                       AND h.[estado_nuevo] = 'NoSeleccionado'
                       AND (h.[causa] = 'VacanteCerrada' OR h.[cambiado_por] IS NULL)
                       AND NOT EXISTS (
                           SELECT 1
                           FROM [postulaciones].[historial_estado] AS h2
                           WHERE h2.[postulacion_id] = p.[id]
                             AND h2.[secuencia] > h.[secuencia]
                       )
                 )
            THEN CAST(0 AS bigint)
            ELSE CAST(1 AS bigint)
        END AS [es_respondida]
) AS calc
GROUP BY vr.[empresa_id];
GO

/* ==========================================================================================
 * 5. TRIGGERS DE INMUTABILIDAD (AUDITORÍA HU-05.04 Y HU-08.03 — THROW 51000)
 * ========================================================================================== */
IF OBJECT_ID(N'[postulaciones].[historial_estado]', N'U') IS NOT NULL
BEGIN
    EXEC(N'
CREATE OR ALTER TRIGGER [postulaciones].[TR_HistorialEstado_Inmutable]
ON [postulaciones].[historial_estado]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, N''El historial de estados de postulaciones es estrictamente inmutable: no se permite UPDATE ni DELETE.'', 1;
END;
');
END;
GO

IF OBJECT_ID(N'[vacantes].[bitacora_moderacion]', N'U') IS NOT NULL
BEGIN
    EXEC(N'
CREATE OR ALTER TRIGGER [vacantes].[TR_BitacoraModeracion_Inmutable]
ON [vacantes].[bitacora_moderacion]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, N''La bitácora de moderación de vacantes y habilidades es inmutable: no se permite UPDATE ni DELETE.'', 1;
END;
');
END;
GO

IF OBJECT_ID(N'[identidad].[bitacora_moderacion_empresa]', N'U') IS NOT NULL
BEGIN
    EXEC(N'
CREATE OR ALTER TRIGGER [identidad].[TR_BitacoraEmpresa_Inmutable]
ON [identidad].[bitacora_moderacion_empresa]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, N''La bitácora de moderación de empresas es inmutable: no se permite UPDATE ni DELETE.'', 1;
END;
');
END;
GO

/* ==========================================================================================
 * 6. SEGURIDAD DE MÍNIMO PRIVILEGIO POR MICROSERVICIO (LOGINS, USUARIOS, GRANT Y DENY)
 *    Nota: en despliegue productivo, rotar las contraseñas de los logins mediante ALTER LOGIN.
 * ========================================================================================== */
IF IS_SRVROLEMEMBER('securityadmin') = 1 OR IS_SRVROLEMEMBER('sysadmin') = 1
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'svc_identidad')
        CREATE LOGIN [svc_identidad] WITH PASSWORD = N'NxT!Svc_Identidad#2026_ChangeMe', CHECK_POLICY = OFF;

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'svc_vacantes')
        CREATE LOGIN [svc_vacantes] WITH PASSWORD = N'NxT!Svc_Vacantes#2026_ChangeMe', CHECK_POLICY = OFF;

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'svc_postulaciones')
        CREATE LOGIN [svc_postulaciones] WITH PASSWORD = N'NxT!Svc_Postulaciones#2026_ChangeMe', CHECK_POLICY = OFF;

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'svc_notificaciones')
        CREATE LOGIN [svc_notificaciones] WITH PASSWORD = N'NxT!Svc_Notificaciones#2026_ChangeMe', CHECK_POLICY = OFF;

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'dba_migrator')
        CREATE LOGIN [dba_migrator] WITH PASSWORD = N'NxT!Dba_Migrator#2026_ChangeMe', CHECK_POLICY = OFF;
END;
GO

IF DATABASE_PRINCIPAL_ID(N'svc_identidad') IS NULL
    CREATE USER [svc_identidad] FOR LOGIN [svc_identidad] WITH DEFAULT_SCHEMA = [identidad];

IF DATABASE_PRINCIPAL_ID(N'svc_vacantes') IS NULL
    CREATE USER [svc_vacantes] FOR LOGIN [svc_vacantes] WITH DEFAULT_SCHEMA = [vacantes];

IF DATABASE_PRINCIPAL_ID(N'svc_postulaciones') IS NULL
    CREATE USER [svc_postulaciones] FOR LOGIN [svc_postulaciones] WITH DEFAULT_SCHEMA = [postulaciones];

IF DATABASE_PRINCIPAL_ID(N'svc_notificaciones') IS NULL
    CREATE USER [svc_notificaciones] FOR LOGIN [svc_notificaciones] WITH DEFAULT_SCHEMA = [notificaciones];

IF DATABASE_PRINCIPAL_ID(N'dba_migrator') IS NULL
    CREATE USER [dba_migrator] FOR LOGIN [dba_migrator];
GO

-- Permisos de svc_identidad: solo [identidad], sin UPDATE/DELETE en bitacora_moderacion_empresa.
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[identidad] TO [svc_identidad];
IF OBJECT_ID(N'[identidad].[bitacora_moderacion_empresa]', N'U') IS NOT NULL
    DENY UPDATE, DELETE ON OBJECT::[identidad].[bitacora_moderacion_empresa] TO [svc_identidad];

-- Permisos de svc_vacantes: solo [vacantes] + ejecucion de sp_BuscarVacantes y TVP, sin UPDATE/DELETE en bitacora_moderacion.
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[vacantes] TO [svc_vacantes];
IF OBJECT_ID(N'[vacantes].[sp_BuscarVacantes]', N'P') IS NOT NULL
    GRANT EXECUTE ON OBJECT::[vacantes].[sp_BuscarVacantes] TO [svc_vacantes];
IF TYPE_ID(N'[vacantes].[FiltroHabilidadTipo]') IS NOT NULL
    GRANT EXECUTE ON TYPE::[vacantes].[FiltroHabilidadTipo] TO [svc_vacantes];
IF OBJECT_ID(N'[vacantes].[bitacora_moderacion]', N'U') IS NOT NULL
    DENY UPDATE, DELETE ON OBJECT::[vacantes].[bitacora_moderacion] TO [svc_vacantes];

-- Permisos de svc_postulaciones: solo [postulaciones] + NEXT VALUE FOR folio_seq, sin UPDATE/DELETE en historial_estado.
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[postulaciones] TO [svc_postulaciones];
IF OBJECT_ID(N'[postulaciones].[folio_seq]', N'SO') IS NOT NULL
    GRANT UPDATE ON OBJECT::[postulaciones].[folio_seq] TO [svc_postulaciones];
IF OBJECT_ID(N'[postulaciones].[historial_estado]', N'U') IS NOT NULL
    DENY UPDATE, DELETE ON OBJECT::[postulaciones].[historial_estado] TO [svc_postulaciones];

-- Permisos de svc_notificaciones: [notificaciones] y [HangFire] (incluye DDL sobre HangFire para PrepareSchemaIfNecessary).
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[notificaciones] TO [svc_notificaciones];
GRANT SELECT, INSERT, UPDATE, DELETE, ALTER, CONTROL ON SCHEMA::[HangFire] TO [svc_notificaciones];
GRANT CREATE TABLE TO [svc_notificaciones];

-- Permisos de dba_migrator: uso exclusivo en CI/CD y mantenimiento.
ALTER ROLE [db_ddladmin] ADD MEMBER [dba_migrator];
ALTER ROLE [db_datareader] ADD MEMBER [dba_migrator];
ALTER ROLE [db_datawriter] ADD MEMBER [dba_migrator];
GO
