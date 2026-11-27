# 02 — Diccionario de Datos Normalizado (`NexTalentDb`)

Todas las tablas, columnas, índices y restricciones físicas utilizan `snake_case` (salvo las tablas internas de MassTransit y Hangfire). Todas las marcas temporales se persisten en UTC mediante `datetime2(0)`. Los identificadores primarios usan `uniqueidentifier` (`DEFAULT NEWSEQUENTIALID()`), excepto `postulaciones.historial_estado.id` (`bigint IDENTITY(1,1)`) para garantizar orden físico estricto de inserción.

---

## 1. Esquema `[identidad]`

| Tabla | Columnas Principales y Tipos | Restricciones e Índices Clave |
|---|---|---|
| `identidad.usuario` | `id` (`uniqueidentifier` PK), `email` (`nvarchar(256)`), `password_hash` (`nvarchar(512)`), `telefono_whatsapp` (`varchar(16)` NULL), `rol` (`varchar(20)`), `whatsapp_opt_in` (`bit`), `whatsapp_opt_in_fecha` (`datetime2(0)` NULL), `aviso_privacidad_version` (`varchar(20)`), `aviso_privacidad_aceptado_en` (`datetime2(0)`), `email_verificado` (`bit`), `intentos_fallidos` (`int`), `bloqueado_hasta` (`datetime2(0)` NULL), `creado_en` (`datetime2(0)`) | `UQ_Usuario_Email`, `UQ_Usuario_Telefono` (filtrado `IS NOT NULL`), `CK_Usuario_Rol`, `CK_Usuario_OptIn_Fecha` |
| `identidad.candidato` | `usuario_id` (`uniqueidentifier` PK/FK), `nombres` (`nvarchar(100)`), `apellido_paterno` (`nvarchar(80)`), `apellido_materno` (`nvarchar(80)` NULL), `edad` (`tinyint` NULL), `ocupacion` (`nvarchar(120)` NULL), `curp` (`char(18)` NULL), domicilio separado, `ubicacion` (`nvarchar(120)` NULL), `salario_esperado` (`decimal(12,2)` NULL), `modalidad_preferida` (`varchar(20)` NULL), `foto_url` / `cv_perfil_url` (`nvarchar(500)` NULL), `completitud_pct` (`tinyint`) | `UQ_Candidato_Curp` (filtrado), `CK_Candidato_Edad`, `CK_Candidato_CodigoPostal`, `CK_Candidato_SalarioEsperado`, `CK_Candidato_Modalidad`, `CK_Candidato_Completitud` |
| `identidad.candidato_habilidad` | `candidato_id` (`uniqueidentifier` FK), `habilidad_id` (`uniqueidentifier` ref. lógica), `nivel` (`tinyint` 1..3) | PK (`candidato_id`, `habilidad_id`), `CK_CandidatoHabilidad_Nivel`, `IX_CandidatoHabilidad_Habilidad` |
| `identidad.experiencia_laboral` | `id` (`uniqueidentifier` PK), `candidato_id` (`uniqueidentifier` FK), `puesto` (`nvarchar(120)`), `empresa_nombre` (`nvarchar(160)`), `descripcion` (`nvarchar(1000)` NULL), `fecha_inicio` (`date`), `fecha_fin` (`date` NULL), `es_actual` (`bit`) | `CK_ExperienciaLaboral_Fechas`, `IX_ExperienciaLaboral_Candidato` |
| `identidad.formacion_academica` | `id` (`uniqueidentifier` PK), `candidato_id` (`uniqueidentifier` FK), `institucion` (`nvarchar(160)`), `titulo_grado` (`nvarchar(160)`), `fecha_inicio` (`date`), `fecha_fin` (`date` NULL), `es_actual` (`bit`) | `CK_FormacionAcademica_Fechas`, `IX_FormacionAcademica_Candidato` |
| `identidad.empresa` | `id` (`uniqueidentifier` PK), `nombre` (`nvarchar(160)`), `rfc` (`varchar(13)`), `verificada` (`bit`), `estado_verificacion` (`varchar(20)`), `creada_en` (`datetime2(0)`) | `UQ_Empresa_Rfc`, `CK_Empresa_Rfc_Longitud`, `CK_Empresa_EstadoVerificacion`, `IX_Empresa_ColaFifo` |
| `identidad.reclutador` | `usuario_id` (`uniqueidentifier` PK/FK), `empresa_id` (`uniqueidentifier` FK), `nombre_contacto` (`nvarchar(120)`), `aprobado_empresa` (`bit`) | `IX_Reclutador_Empresa` |
| `identidad.refresh_token` | `id` (`uniqueidentifier` PK), `usuario_id` (`uniqueidentifier` FK), `token_hash` (`varchar(128)`), `expira_en` (`datetime2(0)`), `revocado_en` (`datetime2(0)` NULL) | `UQ_RefreshToken_Hash`, `IX_RefreshToken_Usuario_Expira` |
| `identidad.bitacora_moderacion_empresa` | `id` (`uniqueidentifier` PK), `empresa_id` (`uniqueidentifier` FK), `administrador_id` (`uniqueidentifier` FK), `decision` (`varchar(20)`), `motivo` (`nvarchar(500)`), `fecha` (`datetime2(0)`) | `CK_BitacoraEmpresa_Decision`, `CK_BitacoraEmpresa_Motivo`, trigger `TR_BitacoraEmpresa_Inmutable` |

---

## 2. Esquema `[vacantes]`

| Tabla | Columnas Principales y Tipos | Restricciones e Índices Clave |
|---|---|---|
| `vacantes.empresa_resumen` | `empresa_id` (`uniqueidentifier` PK), `nombre` (`nvarchar(160)`), `verificada` (`bit`), `indice_respuesta_pct` (`decimal(5,2)`), `actualizado_en` (`datetime2(0)`) | `CK_EmpresaResumen_IndiceRespuesta` (`0..100`) |
| `vacantes.vacante` | `id` (`uniqueidentifier` PK), `empresa_id` (`uniqueidentifier` FK), `reclutador_id` (`uniqueidentifier` ref. lógica), `titulo` (`nvarchar(150)`), `descripcion` (`nvarchar(max)`), `actividades` / `beneficios` (`nvarchar(2000)` NULL), `salario_min` / `salario_max` (`decimal(12,2)`), `tipo_salario` (`varchar(10)`), `modalidad` (`varchar(20)`), `jornada` (`varchar(30)`), `ubicacion` (`nvarchar(120)`), `estado` (`varchar(25)`), `publicada_en` / `expira_en` (`datetime2(0)` NULL), `recordatorio_27d_enviado` (`bit`), `version_evento` (`bigint`), `creada_en` (`datetime2(0)`) | RN-02: `CK_Vacante_SalarioMin_Positivo` (`> 0`), `CK_Vacante_SalarioMax_Valido` (`>= salario_min`), `CK_Vacante_TipoSalario`, `CK_Vacante_Modalidad`, `CK_Vacante_Estado`, `IX_Vacante_Busqueda`, Full-Text `ftc_NexTalent` |
| `vacantes.habilidad` | `id` (`uniqueidentifier` PK), `nombre` (`nvarchar(80)`), `categoria` (`nvarchar(60)`), `estado_aprobacion` (`varchar(20)`), `propuesta_por` (`uniqueidentifier` NULL) | `UQ_Habilidad_Nombre`, `CK_Habilidad_EstadoAprobacion` |
| `vacantes.vacante_habilidad` | `vacante_id` (`uniqueidentifier` FK), `habilidad_id` (`uniqueidentifier` FK), `nivel` (`tinyint` 1..3), `es_requerida` (`bit`) | PK (`vacante_id`, `habilidad_id`), `CK_VacanteHabilidad_Nivel`, `IX_VacanteHabilidad_Habilidad` |
| `vacantes.reporte_vacante` | `id` (`uniqueidentifier` PK), `vacante_id` (`uniqueidentifier` FK), `usuario_id` (`uniqueidentifier` ref. lógica), `motivo_tipo` (`varchar(30)`), `detalle` (`nvarchar(500)` NULL), `fecha` (`datetime2(0)`) | `UQ_ReporteVacante_Vacante_Usuario`, `CK_ReporteVacante_Motivo` |
| `vacantes.bitacora_moderacion` | `id` (`uniqueidentifier` PK), `entidad_tipo` (`varchar(20)`), `entidad_id` (`uniqueidentifier`), `administrador_id` (`uniqueidentifier`), `accion` (`varchar(30)`), `motivo` (`nvarchar(500)`), `fecha` (`datetime2(0)`) | `CK_BitacoraModeracion_Entidad`, `CK_BitacoraModeracion_Motivo`, trigger `TR_BitacoraModeracion_Inmutable` |

---

## 3. Esquema `[postulaciones]`

| Tabla | Columnas Principales y Tipos | Restricciones e Índices Clave |
|---|---|---|
| `postulaciones.postulacion` | `id` (`uniqueidentifier` PK), `folio` (`varchar(20)`), `candidato_id` (`uniqueidentifier`), `vacante_id` (`uniqueidentifier` FK a réplica), `coincidencia_pct` (`decimal(5,2)`), `deseables_cumplidas` (`int`), `creada_en` (`datetime2(0)`), `canal_origen` (`varchar(15)`), `estado_actual` (`varchar(25)`), `etapa_actualizada_en` (`datetime2(0)`), `recordatorio_10d_enviado` (`bit`), `version` (`rowversion`) | `uq_postulacion_folio`, `uq_postulacion_candidato_vacante`, `ix_postulacion_vacante_coincidencia`, `ck_postulacion_coincidencia_pct` (`0..100`), `ck_postulacion_deseables_cumplidas`, `ck_postulacion_estado_actual`, `ck_postulacion_canal_origen` |
| `postulaciones.historial_estado` | `id` (`bigint IDENTITY(1,1)` PK), `postulacion_id` (`uniqueidentifier` FK), `secuencia` (`int`), `estado_anterior` (`varchar(25)` NULL), `estado_nuevo` (`varchar(25)`), `causa` (`varchar(25)`), `cambiado_por` (`uniqueidentifier` NULL para sistema), `motivo` (`nvarchar(300)` NULL), `fecha` (`datetime2(0)`) | `uq_historial_estado_postulacion_secuencia`, `ck_historial_estado_motivo_reactivacion`, trigger `TR_HistorialEstado_Inmutable` |
| `postulaciones.vacante_replica` | `vacante_id` (`uniqueidentifier` PK), `empresa_id` (`uniqueidentifier`), `titulo` (`nvarchar(200)`), `nombre_empresa` (`nvarchar(200)` NULL), `salario_min` / `salario_max` (`decimal(12,2)` NULL), `tipo_salario` (`varchar(10)` NULL), `expira_en` (`datetime2(0)` NULL), `estado` (`varchar(25)`), `actualizada_en` (`datetime2(0)`), `version_evento` (`bigint`) | `ck_vacante_replica_estado` (`'Activa'`, `'Cerrada'`), `ix_vacante_replica_empresa` |
| `postulaciones.vacante_habilidad_replica` | `vacante_id` (`uniqueidentifier` FK), `habilidad_id` (`uniqueidentifier`), `nivel` (`varchar(12)`), `requerida` (`bit`) | PK (`vacante_id`, `habilidad_id`), `ck_vacante_habilidad_replica_nivel` |
| `postulaciones.candidato_replica` | `candidato_id` (`uniqueidentifier` PK), `tiene_nombre` (`bit`), `tiene_contacto` (`bit`), `version_evento` (`bigint`) | Sin datos personales sensibles |
| `postulaciones.candidato_habilidad_replica` | `candidato_id` (`uniqueidentifier` FK), `habilidad_id` (`uniqueidentifier`), `nivel` (`varchar(12)`) | PK (`candidato_id`, `habilidad_id`), `ck_candidato_habilidad_replica_nivel` |

---

## 4. Esquema `[notificaciones]`

| Tabla | Columnas Principales y Tipos | Restricciones e Índices Clave |
|---|---|---|
| `notificaciones.notificacion` | `id` (`uniqueidentifier` PK), `usuario_id` (`uniqueidentifier`), `canal` (`varchar(15)`), `plantilla` (`varchar(50)`), `estado_envio` (`varchar(20)`), `intentos` (`int`), `creada_en` (`datetime2(0)`), metadatos de evento sin texto personal (`evento_id`, `postulacion_id`, `vacante_id`, `empresa_id`, `referencia_folio`, `titulo_vacante`, `nombre_empresa`, `estado_anterior`, `estado_nuevo`, `causa_cambio`, `cambiado_en`, `secuencia`), `proximo_intento_en` / `enviada_en` (`datetime2(0)` NULL), `id_externo` (`varchar(128)` NULL), `ultimo_error` (`nvarchar(500)` NULL) | `uq_notificacion_evento_usuario_canal` (filtrado), `ix_notificacion_reintentos`, `ix_notificacion_postulacion_secuencia`, `ck_notificacion_canal`, `ck_notificacion_estado_envio`, `ck_notificacion_intentos` |
| `notificaciones.contacto_replica` | `usuario_id` (`uniqueidentifier` PK), `rol` (`varchar(20)`), `empresa_id` (`uniqueidentifier` NULL), `email` (`nvarchar(256)`), `telefono_whatsapp` (`varchar(20)` NULL), `whatsapp_user_id` (`varchar(128)` NULL), `whatsapp_opt_in` (`bit`), `whatsapp_opt_in_fecha` / `whatsapp_baja_fecha` (`datetime2(0)` NULL), `frecuencia_alertas` (`varchar(15)`), `version_evento` (`bigint`) | `uq_contacto_replica_telefono` (filtrado), `uq_contacto_replica_bsuid` (filtrado), `ix_contacto_replica_empresa`, `ck_contacto_replica_opt_in_fecha` |
| `notificaciones.codigo_whatsapp` | `id` (`uniqueidentifier` PK), `usuario_id` (`uniqueidentifier`), `telefono` (`varchar(20)` NULL), `codigo_hash` (`varbinary(32)` NULL), `enviado_en` / `expira_en` / `bloqueado_hasta` / `usado_en` (`datetime2(0)` NULL), `intentos` (`int`) | `uq_codigo_whatsapp_usuario`, `ck_codigo_whatsapp_intentos` |
| `notificaciones.mensaje_whatsapp_procesado` | `mensaje_id` (`varchar(128)` PK), `recibido_en` (`datetime2(0)`), `resultado` (`varchar(30)`) | Idempotencia de webhooks de Meta |
| `notificaciones.postulacion_seguimiento` | `postulacion_id` (`uniqueidentifier` PK), `candidato_id`, `vacante_id`, `empresa_id` (`uniqueidentifier`), `folio` (`varchar(20)`), `titulo_vacante` (`nvarchar(200)`), `estado_actual` (`varchar(25)`), `pendiente` (`bit`), `ultimo_cambio_en` (`datetime2(0)`), `secuencia` (`int`), `recordatorio_10d_enviado` (`bit`), `recordatorio_enviado_en` (`datetime2(0)` NULL) | `ix_postulacion_seguimiento_pendientes`, `ix_postulacion_seguimiento_vacante` |
| `notificaciones.alerta_vacante_enviada` | `usuario_id` (`uniqueidentifier`), `vacante_id` (`uniqueidentifier`), `id_alerta` (`varchar(128)` NULL), `boton` (`varchar(30)` NULL), `enviada_en` (`datetime2(0)`) | PK (`usuario_id`, `vacante_id`), `ix_alerta_vacante_enviada_alerta_boton` |
