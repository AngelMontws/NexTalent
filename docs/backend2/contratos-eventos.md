# Contratos de eventos (tarea 1.2)

| | |
|---|---|
| **Responsables** | Backend 2 (borrador) y Backend 1 (acuerdo) |
| **Fase** | 1 - Diseño |
| **Fuentes** | Documento v5 (secciones 1.3.4, 1.4.3, 1.4.4, 3.2 y 3.4), backlog y `maquina-estados.md` |
| **Estado** | Borrador. No hay código en `NexTalent.Contracts` hasta acordarlo |

**[F]** = definido en las fuentes. **[P]** = propuesta de Backend 2.

Las fuentes nombran cinco eventos, pero no sus campos. Salvo indicación, **todos los campos son [P]**.

## 1. Resumen

| Evento | Publica | Consume | Origen |
|---|---|---|---|
| `EmpresaVerificada` | Identidad | Vacantes (y Notificaciones, decisión 9) | [F] |
| `PerfilHabilidadesActualizado` | Identidad | Vacantes [F], Postulaciones [P] | [F] |
| `VacanteCerrada` | Vacantes | Postulaciones, Notificaciones | [F] |
| `PostulacionCreada` | Postulaciones | Notificaciones | [F] |
| `EstatusCambiado` | Postulaciones | Notificaciones | [F] |
| `VacantePublicada` | Vacantes | Postulaciones, Notificaciones | **Nuevo [P]** |
| `ContactoActualizado` | Identidad | Notificaciones | **Nuevo [P]** |

El v5 dice que Notificaciones "consume todos los eventos". La tabla solo lista los que necesita.

## 2. Huecos que obligan a decidir

1. **Postulaciones no conoce las vacantes.** Debe validar que estén abiertas y calcular RN-03 con sus habilidades, pero no puede leer el esquema `vacantes` y ningún evento se las envía. Propuesta: `VacantePublicada`.
2. **Postulaciones no conoce el perfil mínimo.** HU-02.04 exige nombre, medio de contacto y al menos 3 habilidades. El evento de perfil solo habla de habilidades. Propuesta: agregar `TieneNombre` y `TieneContacto`.
3. **Notificaciones no conoce correo, teléfono ni opt-in.** Viven en `identidad.usuario`. Propuesta: `ContactoActualizado`.
4. **No hay reclutador responsable de la vacante.** `vacante` solo tiene `empresa_id`, pero el recordatorio de 10 días es para "el reclutador". Ver decisión 4.

La alternativa a los eventos nuevos es una llamada REST. Se descarta como propuesta porque una caída de Identidad o Vacantes impediría postularse o avisar.

## 3. Reglas comunes [P]

- Contratos como `record` de C# en `NexTalent.Contracts`, sin depender de MassTransit.
- Fechas en UTC (`DateTimeOffset`).
- Cada evento lleva `EventoId` (para descartar repetidos) y `OcurridoEn`.
- Identificadores: se escriben `Guid`, **pero el tipo lo define Base de Datos** (decisión 1).
- `CandidatoId` es el `usuario_id`, porque `candidato` es 1:1 con `usuario`.
- Estados y causas viajan como texto estable, no como número, para que reordenar un enum no cambie su significado.
- Solo se agregan campos opcionales. Quitar o renombrar exige acuerdo.
- Los eventos de réplica llevan el contenido **completo** y una `Version` creciente; se descarta una versión anterior a la guardada.
- Nunca viajan CURP, CV, dirección, datos bancarios ni contraseñas.

## 4. Campos

Todos incluyen `EventoId` y `OcurridoEn`.

### 4.1 EmpresaVerificada

| Campo | Tipo | Uso |
|---|---|---|
| `EmpresaId` | Guid | Identifica la empresa |
| `Nombre` | string | Dato de despliegue |
| `Verificada` | bool | El v5 publica el evento al aprobar **o rechazar** |
| `Version` | long | Orden |

El motivo no viaja: solo hace falta para el correo (decisión 9).

### 4.2 PerfilHabilidadesActualizado

| Campo | Tipo | Uso |
|---|---|---|
| `CandidatoId` | Guid | Identifica a la persona |
| `Habilidades` | lista de `HabilidadId`, `Nivel` | RN-03; máximo 20 |
| `TieneNombre`, `TieneContacto` | bool | Perfil mínimo sin copiar los datos |
| `Version` | long | Orden |

`Nivel`: `Basico`, `Intermedio` o `Avanzado`. El panel del reclutador quizá necesite nombre y salario esperado (HU-06.01): depende de los mockups (decisión 6).

### 4.3 VacantePublicada (nuevo)

Se publica al publicar, editar, renovar o reabrir.

| Campo | Tipo | Uso |
|---|---|---|
| `VacanteId`, `EmpresaId` | Guid | Identificación y permiso por empresa |
| `Titulo`, `NombreEmpresa` | string | Avisos y "Mis postulaciones" |
| `SalarioMin`, `SalarioMax`, `TipoSalario` | decimal, decimal, string | Dato de despliegue |
| `Habilidades` | lista de `HabilidadId`, `Nivel`, `Requerida` | RN-03 y desempate; máximo 15 |
| `ExpiraEn` | DateTimeOffset | Dato de despliegue |
| `Version` | long | Orden, compartido con `VacanteCerrada` |

Editar la vacante no recalcula `coincidencia_pct`: es una instantánea [F].

### 4.4 VacanteCerrada

| Campo | Tipo | Uso |
|---|---|---|
| `VacanteId` | Guid | Cierra las postulaciones pendientes |
| `EmpresaId` | Guid | Índice de respuesta |
| `Titulo` | string | Aviso |
| `Causa` | string | `Manual`, `Caducidad` o `Moderacion` |
| `Version` | long | Orden, compartido con `VacantePublicada` |

Las tres causas son [F]. Ocultar por reportes no publica este evento [F: D-02].

### 4.5 PostulacionCreada

| Campo | Tipo | Uso |
|---|---|---|
| `PostulacionId`, `CandidatoId`, `VacanteId`, `EmpresaId` | Guid | Identificación |
| `Folio` | string | Aviso; formato por definir |
| `TituloVacante`, `NombreEmpresa` | string | Aviso |
| `CreadaEn` | DateTimeOffset | Inicio del plazo de 10 días hábiles |

`coincidencia_pct` no viaja: ningún consumidor lo necesita.

### 4.6 EstatusCambiado

| Campo | Tipo | Uso |
|---|---|---|
| `PostulacionId`, `CandidatoId`, `VacanteId`, `EmpresaId` | Guid | Identificación |
| `Folio` | string | Aviso |
| `TituloVacante`, `NombreEmpresa` | string | Aviso |
| `EstadoAnterior`, `EstadoNuevo` | string | Aviso y reinicio del plazo |
| `Causa` | string | `PerfilAbierto`, `Avance`, `Descarte`, `VacanteCerrada` o `Reactivacion` (T02 a T06) |
| `CambiadoEn` | DateTimeOffset | Fecha de la etapa |
| `Secuencia` | int | Orden dentro de la postulación |

- Valores de estado: `Enviada`, `Vista`, `EnRevision`, `Entrevista`, `OfertaContratado`, `NoSeleccionado`.
- El `motivo` **no viaja**: puede contener texto personal y terminar en un mensaje.
- HU-06.03 permite editar la plantilla del descarte. Cómo viaja ese texto es la decisión 5.

### 4.7 ContactoActualizado (nuevo)

| Campo | Tipo | Uso |
|---|---|---|
| `UsuarioId` | Guid | Identifica a la persona |
| `Rol` | string | Candidato o reclutador |
| `EmpresaId` | Guid opcional | Reclutadores de una empresa |
| `Email` | string | Respaldo por correo |
| `TelefonoWhatsapp` | string opcional | Envío |
| `WhatsappOptIn`, `WhatsappOptInFecha` | bool, fecha opcional | Sin opt-in no se envía |
| `Version` | long | Orden |

Contiene datos personales: solo lo consume Notificaciones y no se registra en logs.

El opt-in se confirma y se revoca en Notificaciones, pero se guarda en `identidad.usuario`. Falta definir quién es la fuente de verdad (decisión 3).

## 5. Réplicas locales

| Servicio | Réplica | Se alimenta de |
|---|---|---|
| Postulaciones | Vacante: datos, habilidades y si está abierta | `VacantePublicada`, `VacanteCerrada` |
| Postulaciones | Candidato: habilidades y perfil mínimo | `PerfilHabilidadesActualizado` |
| Notificaciones | Contacto y opt-in | `ContactoActualizado` |
| Notificaciones | Último cambio de cada postulación pendiente | `PostulacionCreada`, `EstatusCambiado` |

El detalle de tablas es la tarea 1.4 y lo aprueba Base de Datos. Las réplicas necesitan carga inicial si Identidad o Vacantes ya tienen datos (decisión 7).

## 6. Casos límite [P]

| Caso | Comportamiento |
|---|---|
| Evento repetido | Se ignora por `EventoId` |
| `VacantePublicada` antigua después de `VacanteCerrada` | Se ignora por `Version` |
| `VacanteCerrada` de una vacante desconocida | Se guarda como cerrada, para rechazar postulaciones |
| Postulación a una vacante que aún no llegó a la réplica | Error temporal que permite reintentar; no "vacante cerrada" |
| `EstatusCambiado` fuera de orden | Se avisa solo si `Secuencia` es mayor que la última procesada |
| Perfil aún no replicado | Se trata como perfil incompleto |

## 7. SignalR

El contexto de Backend 2 dice que Notificaciones avisa por SignalR; el v5 ubica el hub en **Postulaciones** y el gateway ya enruta `/hubs/estatus` ahí. Se sigue el v5: Postulaciones avisa por SignalR después de confirmar la transacción, y Notificaciones solo WhatsApp y correo.

## 8. Decisiones por acordar

| # | Pregunta | Propuesta | Con quién |
|---|---|---|---|
| 1 | ¿Tipo de los identificadores? | `Guid` | Base de Datos, Backend 1 |
| 2 | ¿Se agrega `VacantePublicada`? | Sí; **bloquea HU-05.01** | Backend 1 |
| 3 | ¿`ContactoActualizado` y quién es dueño del opt-in? | Identidad guarda el contacto; Notificaciones aplica la baja de inmediato y avisa a Identidad | Backend 1, Base de Datos |
| 4 | ¿A qué reclutador se recuerda? | Agregar un responsable a la vacante; si no, a todos los de la empresa | Backend 1, Base de Datos |
| 5 | ¿Cómo viaja el mensaje editado del descarte? | Solo por correo o web; en WhatsApp, la plantilla fija | Frontend, equipo |
| 6 | ¿Qué datos del candidato ve el reclutador? | Sin propuesta: depende de mockups | Frontend |
| 7 | ¿Cómo se cargan las réplicas la primera vez? | Republicar el estado actual | Backend 1 |
| 8 | ¿El JWT del reclutador lleva `empresa_id`? | Sí; si no, se usa la réplica | Backend 1 |
| 9 | ¿Quién envía el correo de verificación (HU-08.01)? | Notificaciones; entonces `EmpresaVerificada` necesita el motivo | Backend 1 |
| 10 | ¿Puede reabrirse una vacante cerrada? | Sin propuesta: es de Vacantes | Backend 1 |

## 9. Después del acuerdo

1. Crear los `record` en `NexTalent.Contracts`, en otra rama.
2. Probar que se serializan y deserializan sin perder datos.
3. Agregar los dos eventos nuevos al documento v5.
4. La conexión con MassTransit 8.x es la tarea 2.3.
