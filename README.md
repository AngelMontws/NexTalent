# NexTalent

Bolsa de trabajo web responsiva para México. Cada vacante publica su rango salarial, cada postulación muestra su estatus en tiempo real y el candidato puede recibir alertas y postularse por WhatsApp.

Proyecto escolar del equipo **Gustambitos** · UPIICSA – IPN. El documento del proyecto es la fuente de verdad de las decisiones de producto, arquitectura y datos.

## Requisitos

- Git
- .NET SDK 10 (versión 10.0.100 o superior de la rama 10.0). El archivo `global.json` fija esta versión: si solo tienes el SDK 11, instala también el 10.
- Docker Desktop (debe estar abierto antes de usar `docker`)
- VS Code o Visual Studio
- Opcional: SSMS para consultar SQL Server

## Cómo empezar

```powershell
git clone https://github.com/AngelMontws/NexTalent.git
cd NexTalent
Copy-Item .env.example .env      # luego edita .env con tus propias contraseñas
docker compose up -d --build     # la primera vez tarda varios minutos
docker compose ps -a
```

Nunca subas el archivo `.env` a Git: contiene contraseñas y está ignorado. El `.env.example` explica qué debe tener cada valor (la contraseña de SQL Server necesita al menos 8 caracteres con mayúsculas, minúsculas, números y símbolos, sin `$ " ' \` ni espacios).

En `docker compose ps -a` debes ver `rabbitmq` y `sqlserver` en `healthy`, `sqlserver-init` en `Exited (0)` (es un contenedor de un solo uso) y `identidad`, `vacantes`, `postulaciones`, `notificaciones` y `gateway` en `Up`.

Comprobación rápida (en PowerShell usa `curl.exe`, porque `curl` a secas es otro comando):

```powershell
curl.exe -i http://localhost:5000/health
curl.exe -i http://localhost:5000/health/identidad
```

Ambos deben responder `200 OK` con `Healthy`.

`docker compose down` detiene todo y conserva los datos; `docker compose down -v` los borra.

## Gateway y rutas

Todo el tráfico externo entra por el API Gateway en `http://localhost:5000`. Los servicios no se publican al host.

| Ruta pública | Destino | Qué ve el servicio |
|---|---|---|
| `/health` | El propio Gateway | — |
| `/health/identidad`, `/health/vacantes`, `/health/postulaciones`, `/health/notificaciones` | El servicio correspondiente | `/health` |
| `/api/auth/**` | Identidad | `/auth/**` |
| `/api/vacantes/**` y `/api/habilidades/**` | Vacantes y Búsqueda | `/vacantes/**` y `/habilidades/**` |
| `/api/postulaciones/**` | Postulaciones | `/postulaciones/**` |
| `/hubs/estatus/**` | Postulaciones (SignalR) | Igual |
| `/whatsapp/**` | Notificaciones (webhook) | Igual |

La validación de JWT y el límite de peticiones por usuario se agregan en la Fase 2.

## Puertos

| Componente | Puerto | ¿Desde tu máquina? |
|---|---|---|
| API Gateway | 5000 | Sí: `http://localhost:5000` |
| Identidad | 5001 | No (solo red interna de Docker; con `dotnet run` sí) |
| Vacantes y Búsqueda | 5002 | No (ídem) |
| Postulaciones | 5003 | No (ídem) |
| Notificaciones | 5004 | No (ídem) |
| RabbitMQ (AMQP / panel web) | 5672 / 15672 | Sí |
| SQL Server | 1433 | Sí |
| Frontend (Vite, `npm run dev`) | 5173 | Sí: `http://localhost:5173` |

## Frontend

Necesita Node.js 20 o superior y el backend corriendo con `docker compose up -d`.

```powershell
cd frontend
npm ci
npm run dev
```

Abre `http://localhost:5173`. En desarrollo, Vite reenvía `/api` y `/hubs` al gateway (`localhost:5000`), así que no hace falta CORS. Por ahora las pantallas usan los datos de prueba de `src/datos.ts`. Más detalle en [`frontend/README.md`](frontend/README.md).

## Base de datos (SQL Server)

SQL Server 2025 Developer en un contenedor (imagen propia con Full-Text Search: `docker/sqlserver/Dockerfile`). Es una sola instancia con la base `NexTalent` y **un esquema por servicio**: `identidad`, `vacantes`, `postulaciones` y `notificaciones`. Los crea el contenedor `sqlserver-init` con `docker/sqlserver/init/crear-esquemas.sql`, que se puede repetir sin error. Por ahora están vacíos: las tablas llegan con las migraciones de EF Core en la Fase 2.

Cada servicio solo lee y escribe su propio esquema. Las referencias entre esquemas se guardan como id, sin llave foránea.

| Desde | Servidor | Cadena de conexión |
|---|---|---|
| Tu máquina (SSMS, `dotnet run`) | `127.0.0.1,1433` | `Server=127.0.0.1,1433;Database=NexTalent;User Id=sa;Password=<MSSQL_SA_PASSWORD de tu .env>;TrustServerCertificate=True` |
| Otro contenedor | `sqlserver,1433` | `Server=sqlserver,1433;Database=NexTalent;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True` |

En SSMS usa autenticación SQL Server y marca "Trust server certificate". **No uses `localhost` ni `(local)`**: si tienes SQL Server instalado en Windows, SSMS puede entrar a esa instancia en lugar de al contenedor. Para confirmar dónde estás, `SELECT @@VERSION` debe decir `on Linux`.

Provisional: por ahora se usa `sa` solo para desarrollo local. Está por decidir si cada servicio tendrá su propio usuario con permisos únicamente sobre su esquema. La contraseña nunca va en el repositorio: cuando los servicios la usen, se pasará por variables de entorno desde el compose.

## RabbitMQ

Panel web: `http://localhost:15672`, con el usuario y la contraseña de tu `.env`. Conexión AMQP: `localhost:5672` desde tu máquina y `rabbitmq:5672` desde otro contenedor. Todavía no hay ninguna biblioteca de mensajería instalada. Está decidido usar MassTransit 8.x (código abierto), que se fija en `Directory.Packages.props` antes de la semana 6: no la instales por tu cuenta.

## Correr un servicio sin su contenedor

```powershell
docker compose up -d rabbitmq sqlserver sqlserver-init
dotnet run --project src/Services/Vacantes/NexTalent.Vacantes.Api
```

El servicio usa el puerto de su `launchSettings.json` (5001 a 5004) y el entorno `Development`; Swagger queda en `/swagger` (por ejemplo, `http://localhost:5002/swagger`). Para pasar por un Gateway local: `docker compose stop gateway` y luego `dotnet run --project src/Gateway/NexTalent.Gateway`. Su `appsettings.json` apunta a `localhost:5001` a `5004`, así que en ese caso los 4 servicios deben correr en tu máquina.

## Pruebas

```powershell
dotnet test --solution NexTalent.sln
```

`global.json` activa Microsoft.Testing.Platform y los proyectos de prueba usan xunit.v3. Para crear uno nuevo, copia la plantilla de `tests/Services/Identidad/NexTalent.Identidad.Domain.Tests`.

## Estructura del repositorio

| Ruta | Qué contiene | Responsable | Estado |
|---|---|---|---|
| `NexTalent.sln` | Solución única | Todos | Listo |
| `global.json`, `Directory.Build.props`, `Directory.Packages.props` | SDK, propiedades comunes y versiones centrales de paquetes | Backend 1 | Listo |
| `src/NexTalent.Contracts/` | Eventos compartidos entre servicios | Backend 1 y 2 | Listo (vacío) |
| `src/Gateway/NexTalent.Gateway/` | API Gateway con YARP | Backend 1 | Rutas listas |
| `src/Services/Identidad/` | Registro, login y JWT | Backend 1 | Plantilla (sin lógica) |
| `src/Services/Vacantes/` | Vacantes, habilidades y búsqueda | Backend 1 | Plantilla (sin lógica) |
| `src/Services/Postulaciones/` | Postulaciones, estados y SignalR | Backend 2 | En curso |
| `src/Services/Notificaciones/` | WhatsApp, correo y recordatorios | Backend 2 | En curso |
| `frontend/` | React 18 + Vite + Tailwind | Frontend | Pantallas listas, con datos de prueba |
| `tests/` | Pruebas xUnit | Todos | 1 proyecto (Identidad.Domain) |
| `docker/` | Dockerfile único de .NET, imagen y script de SQL Server | Backend 1 | Listo |
| `docker-compose.yml` | Todo el entorno local | Backend 1 | Listo |
| `docs/semanas/` | Documento de cierre de cada semana (Backend 1) | Backend 1 | Al día |
| `docs/backend2/` | Notas de Backend 2 | Backend 2 | En curso |
| `tools/backlog/` | Importación del backlog a GitHub Projects | Backend 1 | Listo |

Cada servicio tiene 4 proyectos (`NexTalent.<Servicio>.Api`, `.Application`, `.Domain` e `.Infrastructure`), un endpoint `/health`, Swagger solo en `Development` y un middleware de id de correlación.

## Convenciones técnicas

- Las versiones de paquetes viven solo en `Directory.Packages.props`; los `.csproj` ponen el nombre sin versión. Después de un `dotnet add package`, revisa con `git diff Directory.Packages.props` que la versión sea la esperada.
- Clean Architecture por servicio: las reglas de negocio viven en `Domain` y se prueban sin base de datos.
- Cada petición lleva un id de correlación (`X-Correlation-ID`) que aparece en los logs. Nunca registres contraseñas ni datos sensibles.
- Swagger solo se activa en `Development` (el compose lo fija así para el entorno local).

## Decisiones abiertas

- Instalar MassTransit 8.x (ya decidido; Wolverine como plan B) y fijarlo en `Directory.Packages.props` antes de la semana 6.
- Usuario de SQL por servicio en lugar de `sa`, nombre y collation de la base.
- Quién construye el perfil del candidato.

## Cómo trabajamos

Ramas, commits y revisión de código están en [CONTRIBUTING.md](CONTRIBUTING.md). La documentación de cada semana está en `docs/semanas/`.
