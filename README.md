# NexTalent

Bolsa de trabajo web responsiva para México. Cada vacante publica su rango salarial, cada postulación muestra su estatus en tiempo real y el candidato puede recibir alertas y postularse por WhatsApp.

Proyecto escolar del equipo **Gustambitos** · UPIICSA – IPN. El documento del proyecto es la fuente de verdad de las decisiones de producto, arquitectura y datos.

## Requisitos

- Git
- .NET SDK 10 (versión 10.0.100 o superior de la rama 10.0). El archivo `global.json` fija esta versión: si solo tienes el SDK 11, instala también el 10.
- Docker Desktop (se usa a partir del paso de infraestructura)
- VS Code o Visual Studio

## Cómo empezar

```powershell
git clone https://github.com/AngelMontws/NexTalent.git
cd NexTalent
dotnet --version
dotnet build
```

`dotnet --version` debe mostrar `10.0.x`, y `dotnet build` debe terminar sin errores.

## Estructura del repositorio

| Ruta | Qué contiene | Estado |
|---|---|---|
| `NexTalent.sln` | Solución única | Listo |
| `src/NexTalent.Contracts/` | Eventos compartidos entre servicios | Listo (vacío) |
| `src/Gateway/` | API Gateway con YARP | Semana 2 |
| `src/Identidad/` | Registro, login y JWT | Semana 2 |
| `src/Vacantes/` | Vacantes, habilidades y búsqueda | Semana 2 |
| `src/Postulaciones/` | Postulaciones, estados y SignalR | Semana 2 |
| `src/Notificaciones/` | WhatsApp, correo y recordatorios | Semana 2 |
| `docker-compose.yml` | SQL Server y RabbitMQ (y luego los servicios) | Pendiente |

## Puertos locales previstos

| Componente | Puerto |
|---|---|
| API Gateway | 5000 |
| Identidad | 5001 |
| Vacantes y Búsqueda | 5002 |
| Postulaciones | 5003 |
| Notificaciones | 5004 |
| RabbitMQ (AMQP / panel web) | 5672 / 15672 |
| SQL Server | 1433 |

## Cómo trabajamos

Ramas, commits y revisión de código están en [CONTRIBUTING.md](CONTRIBUTING.md).