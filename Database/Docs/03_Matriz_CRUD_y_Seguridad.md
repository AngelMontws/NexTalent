# 03 — Matriz CRUD y Seguridad (`NexTalentDb`)

El control de acceso en NexTalent implementa defensa en profundidad conforme a **RNF-04**, **RNF-05** y **RNF-06**: validación JWT y rol en API Gateway (YARP), verificación de propiedad de recurso (*resource ownership*, HTTP 403) en cada microservicio, y aislamiento físico de permisos por esquema en SQL Server (`Database/Scripts/02_Objetos_Extra.sql`).

---

## 1. Matriz de Privilegios en SQL Server (Mínimo Privilegio)

| Login / Usuario SQL | Esquemas Autorizados | Permisos Concedidos (`GRANT`) | Restricciones Explícitas (`DENY`) |
|---|---|---|---|
| `svc_identidad` | `[identidad]` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` en `[identidad]`. | `DENY UPDATE, DELETE` en `identidad.bitacora_moderacion_empresa`; sin acceso a otros esquemas. |
| `svc_vacantes` | `[vacantes]` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` en `[vacantes]`; `EXECUTE` en `vacantes.sp_BuscarVacantes` y `vacantes.FiltroHabilidadTipo`. | `DENY UPDATE, DELETE` en `vacantes.bitacora_moderacion`; sin acceso a otros esquemas. |
| `svc_postulaciones` | `[postulaciones]` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` en `[postulaciones]`; `UPDATE` en `postulaciones.folio_seq` (`NEXT VALUE FOR`). | `DENY UPDATE, DELETE` en `postulaciones.historial_estado` + trigger `TR_HistorialEstado_Inmutable`. |
| `svc_notificaciones` | `[notificaciones]`, `[HangFire]` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` en `[notificaciones]` y administración de colas en `[HangFire]`. | Sin acceso a `identidad.candidato` (CURP/domicilio) ni a otros esquemas de dominio. |
| `dba_migrator` | Todos (solo despliegue) | `db_ddladmin`, `db_datareader`, `db_datawriter` para migraciones y `02_Objetos_Extra.sql`. | Uso exclusivo en CI/CD o mantenimiento; nunca asignado a contenedores API en ejecución. |

---

## 2. Salvaguardas de Auditoría y Privacidad

1. **Inmutabilidad garantizada por motor (`THROW 51000`):** `postulaciones.historial_estado`, `vacantes.bitacora_moderacion` e `identidad.bitacora_moderacion_empresa` cuentan con triggers `INSTEAD OF UPDATE, DELETE` que bloquean modificaciones incluso bajo cuentas administrativas (`sa`).
2. **Minimización de datos personales en mensajería (RNF-06):** Ni los eventos de RabbitMQ ni `notificaciones.notificacion` ni las réplicas de `postulaciones` almacenan CURP, domicilio, CV ni contraseñas. Los códigos OTP de WhatsApp se almacenan únicamente como hash `HMAC-SHA256` (`varbinary(32)`).
3. **Índice de respuesta antifantasma (`postulaciones.vw_IndiceRespuestaEmpresa`):** Excluye las postulaciones cerradas automáticamente por el sistema (`causa = 'VacanteCerrada'` o `cambiado_por IS NULL`), impidiendo que empresas inactivas obtengan un falso 100% de respuesta.
