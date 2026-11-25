# Necesidades de datos de Backend 2 (tarea 1.4)

| | |
|---|---|
| **Responsables** | Backend 2 (propuesta) y Base de Datos (aprobación) |
| **Esquemas** | `postulaciones` y `notificaciones` |
| **Fuentes** | Documento v5 (secciones 1.4 y 3.4), `maquina-estados.md`, `contratos-eventos.md` y `whatsapp-cloud-api.md` |
| **Estado** | Propuesta. No hay migraciones hasta que la apruebe Base de Datos |

**[F]** = columna o tabla del documento v5. **[P]** = propuesta de Backend 2. Los tipos son todos [P].

Sin llaves foráneas entre esquemas [F]. Dentro del mismo esquema, sí. Fechas en UTC.

## 1. Esquema postulaciones

### postulacion [F]

| Columna | Tipo | Nulo | Origen |
|---|---|---|---|
| `id` | uniqueidentifier, PK | No | [F] |
| `folio` | varchar(20), UNIQUE | No | [F]; formato por acordar |
| `candidato_id` | uniqueidentifier | No | [F] |
| `vacante_id` | uniqueidentifier | No | [F] |
| `estado_actual` | varchar(20), CHECK con los 6 estados | No | [F]; CHECK [P] |
| `coincidencia_pct` | decimal(5,2), CHECK 0 a 100 | No | [F]; CHECK [P] |
| `deseables_cumplidas` | int, CHECK >= 0 | No | [P]: conserva el desempate |
| `creada_en` | datetimeoffset | No | [F] |
| `version` | rowversion | No | [P]: concurrencia |

- UNIQUE (`candidato_id`, `vacante_id`) [F].
- Índice (`vacante_id`, `coincidencia_pct` desc, `deseables_cumplidas` desc) para HU-06.01 [P].
- El UNIQUE ya sirve para "Mis postulaciones", porque empieza por `candidato_id`.

### historial_estado [F]

| Columna | Tipo | Nulo | Origen |
|---|---|---|---|
| `id` | bigint identity, PK | No | [F] |
| `postulacion_id` | uniqueidentifier, FK | No | [F]; FK [P] |
| `secuencia` | int | No | [P]: orden |
| `estado_anterior` | varchar(20) | Sí, solo al crear | [F] |
| `estado_nuevo` | varchar(20) | No | [F] |
| `causa` | varchar(20) | No | [P] |
| `cambiado_por` | uniqueidentifier | Sí, si fue el sistema | [F]; nulo [P] |
| `motivo` | nvarchar(500) | Sí | [F] |
| `fecha` | datetimeoffset | No | [F] |

- UNIQUE (`postulacion_id`, `secuencia`) [P]: también impide que dos cambios simultáneos escriban el mismo paso.
- **Inmutable** [F]. Propuesta: trigger `INSTEAD OF UPDATE, DELETE` que lance un error y, cuando haya usuario por servicio, también `DENY UPDATE, DELETE`. Con `sa`, `DENY` no tiene efecto. La FK no debe borrar en cascada.
- Un trigger no detiene `TRUNCATE` ni a quien puede modificar el esquema; conviene declararlo como límite del prototipo.

### vacante_replica [P]

| Columna | Tipo | Nulo |
|---|---|---|
| `vacante_id` | uniqueidentifier, PK | No |
| `empresa_id` | uniqueidentifier | Sí |
| `titulo` | nvarchar(200) | Sí |
| `nombre_empresa` | nvarchar(200) | Sí |
| `salario_min`, `salario_max` | decimal(12,2) | Sí |
| `tipo_salario` | varchar(10) | Sí |
| `expira_en` | datetimeoffset | Sí |
| `abierta` | bit | No |
| `version_evento` | bigint | No |

Los datos pueden ser nulos porque `VacanteCerrada` puede llegar antes que `VacantePublicada`.

### vacante_habilidad_replica y candidato_habilidad_replica [P]

| Tabla | Columnas | Llave |
|---|---|---|
| `vacante_habilidad_replica` | `vacante_id`, `habilidad_id`, `nivel`, `requerida` | (`vacante_id`, `habilidad_id`) |
| `candidato_habilidad_replica` | `candidato_id`, `habilidad_id`, `nivel` | (`candidato_id`, `habilidad_id`) |

`nivel`: varchar(12) con `Basico`, `Intermedio` o `Avanzado`. Cada evento reemplaza todas las filas de esa vacante o candidato.

### candidato_replica [P]

| Columna | Tipo | Nulo |
|---|---|---|
| `candidato_id` | uniqueidentifier, PK | No |
| `tiene_nombre`, `tiene_contacto` | bit | No |
| `version_evento` | bigint | No |

Nombre, contacto y salario esperado no se copian. Si los mockups del reclutador los necesitan, se revisa.

### Folio [P]

Secuencia `postulaciones.folio_seq`. El formato `NT-00001` imita un ejemplo ficticio del manual de identidad; no está acordado. Las secuencias pueden dejar huecos y son predecibles: verificar un folio en la web no debe mostrar datos a otra persona.

## 2. Esquema notificaciones

### notificacion [F]

| Columna | Tipo | Nulo | Origen |
|---|---|---|---|
| `id` | uniqueidentifier, PK | No | [F] |
| `usuario_id` | uniqueidentifier | No | [F] |
| `canal` | varchar(10), CHECK web, whatsapp, email | No | [F] |
| `plantilla` | varchar(50) | No | [F] |
| `estado_envio` | varchar(15), CHECK Pendiente, Enviada, Fallida | No | [F]; valores [P] |
| `intentos` | int | No | [F] |
| `creada_en` | datetimeoffset | No | [F] |
| `evento_id` | uniqueidentifier | Sí | [P]: evita repetir avisos |
| `postulacion_id` | uniqueidentifier | Sí | [P]: rastreo |
| `proximo_intento_en`, `enviada_en` | datetimeoffset | Sí | [P]: reintentos |
| `id_externo` | varchar(128) | Sí | [P]: id del mensaje de WhatsApp |
| `ultimo_error` | varchar(50) | Sí | [P]: solo el código |

- UNIQUE (`evento_id`, `usuario_id`, `canal`) cuando `evento_id` no es nulo.
- Índice (`estado_envio`, `proximo_intento_en`) para los reintentos.
- El texto enviado no se guarda, para no duplicar datos personales.

### contacto_replica [P]

| Columna | Tipo | Nulo |
|---|---|---|
| `usuario_id` | uniqueidentifier, PK | No |
| `rol` | varchar(15) | No |
| `empresa_id` | uniqueidentifier | Sí |
| `email` | nvarchar(256) | No |
| `telefono_whatsapp` | varchar(20) | Sí |
| `whatsapp_user_id` (BSUID) | varchar(128) | Sí |
| `whatsapp_opt_in` | bit | No |
| `whatsapp_opt_in_fecha`, `whatsapp_baja_fecha` | datetimeoffset | Sí |
| `version_evento` | bigint | No |

Índice único en teléfono y en BSUID cuando no son nulos. Un evento atrasado no puede reactivar una baja.

### codigo_whatsapp [P]

| Columna | Tipo | Nulo |
|---|---|---|
| `id` | uniqueidentifier, PK | No |
| `usuario_id` | uniqueidentifier | No |
| `telefono` | varchar(20) | No |
| `codigo_hash` | varbinary(32) | No |
| `expira_en` | datetimeoffset (10 min [F]) | No |
| `intentos` | int | No |
| `usado_en` | datetimeoffset | Sí |

El código se guarda con HMAC y una clave en `.env`: son solo 6 dígitos y un hash simple se adivina. Se limitan los intentos; el máximo está por acordar.

### mensaje_whatsapp_procesado [P]

`mensaje_id` varchar(128), PK; `recibido_en`; `resultado` varchar(30). Evita responder dos veces al mismo webhook.

### postulacion_seguimiento [P]

| Columna | Tipo | Nulo |
|---|---|---|
| `postulacion_id` | uniqueidentifier, PK | No |
| `vacante_id`, `empresa_id` | uniqueidentifier | No |
| `pendiente` | bit | No |
| `ultimo_cambio_en` | datetimeoffset | No |
| `secuencia` | int | No |
| `recordatorio_enviado_en` | datetimeoffset | Sí |

Sirve al recordatorio de 10 días hábiles. Falta definir el calendario de festivos y quién lo recibe.

## 3. Tablas de bibliotecas

- **MassTransit**: tablas de Inbox y Outbox en `postulaciones` y `notificaciones`, al fijar la versión 8.x.
- **Hangfire**: el v5 las ubica en `notificaciones`. Hangfire crea las suyas y conviene un esquema separado para no mezclarlas; lo decide Base de Datos.

## 4. Fuera de esta entrega

`alerta_enviada` y la relación botón-vacante de P-01 (HU-07.02). Métricas de HU-06.04.

## 5. Datos de prueba

Solo en Development y con datos ficticios: 2 empresas, 2 reclutadores, 4 candidatos (uno sin perfil mínimo) y 3 vacantes (una cerrada). Los ids deben ser los mismos que use Identidad y Vacantes.

## 6. Aprobación de Base de Datos

| # | Pregunta | Propuesta |
|---|---|---|
| 1 | ¿Tipo de id? | `uniqueidentifier`; `bigint identity` solo en el historial |
| 2 | ¿Nombres en `snake_case`? | Sí, como el v5 |
| 3 | ¿Columnas nuevas en `postulacion` e `historial_estado`? | `deseables_cumplidas`, `version`, `secuencia`, `causa` |
| 4 | ¿`cambiado_por` nulo para el sistema? | Sí |
| 5 | ¿Trigger para el historial? | Sí, y `DENY` cuando haya usuario por servicio |
| 6 | ¿Réplicas y tablas nuevas? | Las de las secciones 1 y 2 |
| 7 | ¿Formato del folio? | Por acordar; `NT-00001` provisional |
| 8 | ¿Índice de respuesta? | Sin propuesta: falta definir "respuesta". La vista quedaría en Postulaciones |
| 9 | ¿Esquema de Hangfire? | Uno separado |
| 10 | ¿Cada servicio guarda su historial de migraciones en su esquema? | Sí, para que no choquen |
