# NexTalent — Fase 0, Semana 1: entorno, repositorio e infraestructura

| | |
|---|---|
| **Rol** | Backend 1 (API Gateway, Identidad, Vacantes y Búsqueda) |
| **Equipo** | Gustambitos · UPIICSA – IPN |
| **Periodo** | 22 y 23 de noviembre de 2026 |
| **Fase** | 0 – Investigación (semanas 1–2) |
| **Tarea del documento (tabla 3.5)** | Elegir stack, repo y Docker |
| **Repositorio** | https://github.com/AngelMontws/NexTalent (privado) |
| **Tablero** | https://github.com/users/AngelMontws/projects/2 |

---

## 1. Resumen

La semana 1 quedó **completada**. Hay un repositorio con solución .NET fijada a la versión 10, reglas de trabajo aplicadas por GitHub, una infraestructura de Docker que levanta SQL Server 2025 con Full-Text Search y RabbitMQ, y el backlog de 32 historias cargado en un tablero.

| Objetivo de la fase 0 | Estado al cierre de la semana 1 | Evidencia |
|---|---|---|
| Confirmar el stack del documento | Hecho, con una decisión pendiente (MassTransit, ver sección 4) | Secciones 3.2 y 4 |
| Repo con `NexTalent.sln` y `NexTalent.Contracts` | Hecho | Sección 3.3 |
| Carpetas de Gateway y de los 4 servicios | Pendiente → semana 2 | Sección 10 |
| Plantilla de servicio (capas, `/health`, Swagger, logs con correlación) | Pendiente → semana 2 | Sección 10 |
| `docker-compose.yml` con SQL Server y RabbitMQ | Hecho | Sección 3.5 |
| Compose con los servicios y puertos 5000–5004 | Pendiente → semana 2 | Sección 10 |
| Acuerdos de trabajo (ramas, PR con revisión, `main` protegida) | Hecho | Sección 3.4 |
| Tablero con el backlog | Hecho (32 historias) | Sección 3.6 |

---

## 2. Entorno verificado

| Componente | Versión / estado |
|---|---|
| Sistema | Windows 11, VS Code |
| .NET SDK | 10.0.404 y 11.0.100 (instalados en paralelo) |
| Runtimes | 10.0.15 y 11.0.0 (más 6.0 y 8.0) |
| Git | 2.56.1 |
| Docker | 29.9.3 · Compose v5.7.0 |
| GitHub CLI | 2.105.0, sesión como `AngelMontws` con permisos `repo`, `project`, `read:org`, `gist`, `workflow` |
| SQL Server nativo | 2025 Developer (servicio `MSSQLSERVER` en ejecución, sin uso en el proyecto) + SSMS 22 |
| SQL Server en contenedor | 2025 CU11 (`17.0.5065.9`) sobre Ubuntu 24.04.5 |
| RabbitMQ en contenedor | Imagen `rabbitmq:4-management` |

Puertos revisados antes de empezar: 1433, 5000–5004, 5672 y 15672 estaban libres (`netstat` sin resultados).

---

## 3. Trabajo realizado

### 3.1 Diagnóstico del entorno

Se revisaron versiones de .NET, Git, Docker, los servicios de SQL Server de Windows y los puertos. Se descubrió que la imagen `mcr.microsoft.com/mssql/server:2025-latest` está basada en Ubuntu 24.04, dato necesario para instalar Full-Text Search (sección 3.5).

### 3.2 Versión de .NET fijada con `global.json`

El documento del proyecto fija .NET 10 LTS. El equipo local tenía además el SDK 11.0.100. Según la política de soporte de Microsoft, .NET 10 y .NET 11 terminan soporte el 14 de noviembre de 2028, así que ambos cubren el semestre. Se mantuvo **.NET 10**, como indica el documento.

`global.json` (raíz del repo):

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

Esto significa "cualquier SDK 10.0 igual o más nuevo que 10.0.100". Verificación: `dotnet --version` dentro del repo → `10.0.404`.

### 3.3 Repositorio y solución

Archivos creados en la raíz:

`.gitattributes`:

```
* text=auto
*.sh text eol=lf
Dockerfile text eol=lf
```

`.editorconfig`:

```
root = true

[*]
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{json,yml,yaml,csproj,props,targets,xml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.sh]
end_of_line = lf

[Dockerfile]
end_of_line = lf
```

`Directory.Build.props`:

```xml
<Project>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

`src/NexTalent.Contracts/NexTalent.Contracts.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>NexTalent.Contracts</RootNamespace>
  </PropertyGroup>

</Project>
```

Comandos ejecutados desde la raíz del repo:

```powershell
dotnet new gitignore
dotnet new sln --name NexTalent --format sln
dotnet sln NexTalent.sln add src/NexTalent.Contracts/NexTalent.Contracts.csproj

Add-Content -Path .gitignore -Value @(
  "",
  "# NexTalent",
  ".env",
  "!.env.example",
  "*.local",
  "docker-data/"
)
```

Nota: con el SDK 10, `dotnet new sln` genera por defecto el formato `.slnx`. Se usó `--format sln` porque el documento pide `NexTalent.sln`.

Verificación: `dotnet build` → `Build succeeded`, 0 advertencias y 0 errores; `git status` sin carpetas `bin/` ni `obj/`.

### 3.4 Acuerdos de trabajo

Se agregaron tres archivos mediante el PR #1: `README.md` (requisitos, cómo empezar, estructura, puertos), `CONTRIBUTING.md` (ramas, commits, PR, tablero) y `.github/pull_request_template.md` (lista de verificación que GitHub carga en cada PR).

Reglas principales de `CONTRIBUTING.md`:

1. Nadie hace commit ni push directo a `main`.
2. Todo cambio entra por PR y necesita la aprobación de otra persona.
3. Todo cambio de esquema de BD lo revisa también el encargado de BD.
4. Nunca se suben contraseñas ni archivos `.env`; solo `.env.example`.
5. Ramas con formato `<tipo>/<descripcion>` (`feature/`, `fix/`, `docs/`, `chore/`, `test/`) y commits con formato `tipo: descripción`.
6. Los PR se unen con **Squash and merge**.

Configuración aplicada en GitHub (Settings del repo):

| Opción | Valor |
|---|---|
| Plan de la cuenta | GitHub Pro (Student Developer Pack), necesario para proteger ramas en un repo privado de una cuenta personal |
| Allow merge commits / Allow rebase merging | Desmarcados |
| Allow squash merging | Marcado |
| Automatically delete head branches | Marcado |
| Regla de rama para `main` | PR obligatorio, 1 aprobación, descartar aprobaciones al subir commits nuevos, resolver conversaciones, sin excepciones ni para el dueño, sin force push, sin borrado |
| Status checks | No requeridos (aún no hay CI) |

Verificación de la protección: un commit vacío enviado directamente a `main` fue rechazado:

```
remote: error: GH006: Protected branch update failed for refs/heads/main.
remote: - Changes must be made through a pull request.
 ! [remote rejected] main -> main (protected branch hook declined)
```

### 3.5 Infraestructura Docker

#### Variables: `.env.example`

```
# Copia este archivo como .env y cambia los valores.
# NUNCA subas el archivo .env a Git.

# RabbitMQ
RABBITMQ_USER=nextalent
RABBITMQ_PASSWORD=cambia-esta-contrasena

# SQL Server (se usa a partir del paso 1.5b)
# Debe tener al menos 8 caracteres y combinar mayusculas, minusculas, numeros y simbolos.
# No uses los caracteres $ " ' \ ni espacios: rompen el healthcheck y la lectura del archivo.
MSSQL_SA_PASSWORD=Cambia-Esta-Contrasena-1
```

Cada integrante crea su propio `.env` (ignorado por Git) con sus contraseñas.

#### Full-Text Search en SQL Server para Linux

El documento advierte que Full-Text Search no viene en la imagen de SQL Server para Linux. Se comprobó lo siguiente:

- El repositorio de paquetes `https://packages.microsoft.com/ubuntu/24.04/mssql-server-2025` existe (respuesta HTTP 200).
- La imagen ya trae la llave `/usr/share/keyrings/microsoft-prod.gpg`, por lo que no hacen falta `curl` ni `gpg`.
- El motor no pertenece a ningún paquete de `dpkg`. Como `mssql-server-fts` depende de `mssql-server`, se verificó después de construir que la versión del motor no cambiara.

Opciones consideradas: usar el SQL Server nativo de Windows (rompe "mismo entorno para los 5"), una imagen de terceros (no oficial, de la versión 2022) o una imagen propia. Se eligió la **imagen propia**.

`docker/sqlserver/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/mssql/server:2025-latest

USER root

# La llave de Microsoft ya viene en la imagen; solo se registra el repositorio del motor 2025
# para poder instalar el paquete de Full-Text Search.
RUN echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/mssql-server-2025 noble main" > /etc/apt/sources.list.d/mssql-server-2025.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends mssql-server-fts \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

USER mssql
```

#### `docker-compose.yml`

```yaml
name: nextalent

services:
  rabbitmq:
    image: rabbitmq:4-management
    container_name: nextalent-rabbitmq
    restart: unless-stopped
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:?Falta RABBITMQ_USER en el archivo .env}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:?Falta RABBITMQ_PASSWORD en el archivo .env}
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 15s
      timeout: 10s
      retries: 5
      start_period: 30s

  sqlserver:
    build:
      context: ./docker/sqlserver
    image: nextalent/sqlserver-fts:2025
    container_name: nextalent-sqlserver
    restart: unless-stopped
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_PID: Developer
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD:?Falta MSSQL_SA_PASSWORD en el archivo .env}
    volumes:
      - sqlserver-data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $$MSSQL_SA_PASSWORD -C -Q 'SELECT 1' -b -o /dev/null"]
      interval: 15s
      timeout: 10s
      retries: 10
      start_period: 40s

volumes:
  rabbitmq-data:
  sqlserver-data:
```

#### Cómo levantarlo (para todo el equipo)

```powershell
git pull
Copy-Item .env.example .env      # luego editar .env con contraseñas propias
docker compose up -d --build     # la primera vez tarda ~3 minutos
docker compose ps
```

Conexión a SQL Server desde SSMS: servidor `127.0.0.1,1433`, autenticación SQL Server, usuario `sa`, contraseña del `.env`, con "Trust server certificate". **No usar `localhost` ni `(local)`**: si el equipo tiene SQL Server instalado en Windows, SSMS puede entrar por memoria compartida a la instancia nativa en vez de al contenedor. Para confirmar dónde se está, `SELECT @@VERSION` dice `on Linux` si es el contenedor. RabbitMQ: http://localhost:15672.

`docker compose down` conserva los datos de los volúmenes; `docker compose down -v` los borra.

#### Verificación de versión y Full-Text Search

```powershell
$pw = ((Get-Content .env | Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' }) -replace '^MSSQL_SA_PASSWORD=', '').Trim()
docker exec nextalent-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $pw -C -Q "SELECT @@VERSION; SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS FTS; SELECT lcid, name FROM sys.fulltext_languages WHERE lcid IN (0, 1033, 3082);"
```

Resultado real:

- `@@VERSION`: SQL Server 2025 (RTM-CU11) `17.0.5065.9`, Enterprise Developer Edition, Linux Ubuntu 24.04.5 (**igual que la imagen base**).
- `FTS` = `1`.
- Idiomas: `3082 Spanish`, `1033 English`, `0 Neutral` (el word breaker de español existe, requisito de HU-04.01).
- `docker compose ps`: los dos servicios `Up (healthy)`; SSMS conectó al contenedor.
- La construcción de la imagen tardó 174 segundos.

### 3.6 Backlog en GitHub

El backlog (`Backlog_NexTalent.xlsx`) tiene 32 historias de usuario en 8 épicas: 19 Must, 10 Should y 3 Could; por fase: 4 en la fase 2, 14 en la 3, 11 en la 4 y 3 en la 5. GitHub Projects no importa CSV ni Excel de forma nativa, así que se hizo con el GitHub CLI.

Archivos (PR #36):

- `tools/backlog/backlog.csv`: el backlog en UTF-8 con encabezados sin acentos (`epica_id`, `epica_nombre`, `epica_descripcion`, `cda`, `hu_id`, `hu_nombre`, `hu_descripcion`, `criterios`, `anexos`, `prioridad`, `fase`, `servicio`).
- `tools/backlog/Import-Backlog.ps1`: script de PowerShell.

Qué hace el script:

1. Por defecto es una prueba en seco (no crea nada); `-Execute` crea y `-Only HU-xx.xx` limita a una historia.
2. Crea etiquetas (`historia-de-usuario`, `EP-01`…`EP-08`, `prio: Must/Should/Could`).
3. Usa o crea el proyecto `NexTalent` con los campos de selección `Epica`, `Fase`, `Prioridad` y `Servicio`.
4. Crea una issue por historia con título `HU-xx.xx - Nombre`, la épica, el CDA, el servicio, la fase, la prioridad, la historia, los criterios como casillas `- [ ]` y los anexos.
5. Agrega cada issue al proyecto y llena sus 4 campos.
6. Se puede repetir sin duplicar: si una historia ya existe, solo completa lo que falte.
7. Revisa el cupo de la API antes de empezar.

La parte que gasta poco cupo usa IDs de GraphQL directamente (3 llamadas ligeras por historia):

```powershell
$mutAgregar = 'mutation($project:ID!,$content:ID!){addProjectV2ItemById(input:{projectId:$project,contentId:$content}){item{id}}}'
# Un solo mutation con 4 alias (a, b, c, d) actualiza Epica, Fase, Prioridad y Servicio:
# a:updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$f1,value:{singleSelectOptionId:$o1}}){projectV2Item{id}}
```

El archivo `.ps1` debe contener solo caracteres ASCII: Windows PowerShell 5.1 lee mal los archivos sin BOM con acentos. Los textos con acentos salen del CSV o se construyen con `[char]`.

Resultado: **32 issues (#4 a #35)** y **32 ítems en el proyecto**. Los agrupamientos por Fase (4/14/11/3), Prioridad (19/10/3), Épica (8 grupos de 4) y Servicio coincidieron con lo esperado, sin grupos "sin valor". Los números #1 a #3 y #36 son PR.

El estado del proyecto usa las columnas por defecto de GitHub (Todo, In Progress, Done), porque el CLI no puede editar las opciones de `Status`.

---

## 4. Decisiones y hallazgos

| Tema | Hallazgo | Decisión |
|---|---|---|
| .NET 10 frente al SDK 11 | Ambos tienen soporte hasta el 14/11/2028; el equipo tenía el SDK 11.0.100 | Mantener `net10.0` como indica el documento y fijar el SDK con `global.json`. Si aparece una barrera, se reestructura |
| MassTransit | La versión 9 es comercial; la 8 sigue siendo de código abierto (Apache 2.0) con parches hasta fines de 2026. Un `dotnet add package MassTransit` sin versión trae la 9.x | **Pendiente.** Recomendación: fijar 8.x. Alternativas: Wolverine/Rebus (cambia el documento) o licencia v9. Decidir con Backend 2 antes de la fase 2 (semana 6). `NexTalent.Contracts` no depende de MassTransit |
| Full-Text Search en Docker | `mssql-server-fts` es un paquete aparte | Imagen propia a partir de la oficial (sección 3.5); versión verificada sin cambios |
| Puerto 1433 y SSMS | El SQL Server nativo de Windows puede confundirse con el contenedor | Conectarse siempre con `127.0.0.1,1433`, nunca `localhost` |
| Merge de PR | El PR #1 se unió con merge commit antes de existir las reglas | Solo squash merge desde entonces (PR #2, #3 y #36) |
| `Directory.Packages.props` | Se había previsto para la semana 1 | Se mueve a la semana 2: se crea junto con los primeros paquetes, verificando versiones actuales |
| Gateway | El documento pide una plantilla común para los servicios | Propuesta: el Gateway es un solo proyecto sin capas (por confirmar) |
| Campos del tablero | Los nombres de campo deben ser ASCII para el script | `Epica` (sin acento) y valores de Fase tal cual el Excel |

---

## 5. Incidencias y lecciones

| Incidencia | Causa | Solución |
|---|---|---|
| `failed to connect to the docker API ... dockerDesktopLinuxEngine` | Docker Desktop estaba cerrado | Abrir Docker Desktop antes de usar `docker` |
| `docker compose build`: `path ... docker\sqlserver not found` | El `Dockerfile` quedó en `docker/` y no en `docker/sqlserver/` | `New-Item` de la carpeta y `Move-Item` del archivo |
| `container name "/tmp-sql" is already in use` | Quedó un contenedor de prueba de un intento anterior | `docker rm -f tmp-sql` |
| `gh` no reconocido en la terminal de VS Code | VS Code heredó el `PATH` anterior a la instalación | Reiniciar VS Code, o `$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [Environment]::GetEnvironmentVariable("Path","User")` |
| `gh issue list ... unknown argument "url"` | En PowerShell, `--json title,url` sin comillas se parte por la coma | Entrecomillar: `--json 'title,url'` |
| `'AngelMontws/NexTalent' has different owner from '@me'` | `gh project link` compara el propietario de forma literal | Usar el usuario real (`AngelMontws`) como `--owner` |
| `unknown owner type` y luego `API rate limit already exceeded` | `gh project item-edit` por nombre hace consultas pesadas y agotó los 5000 puntos/hora de GraphQL | Reescribir el script con IDs de GraphQL (sección 3.6) y revisar el cupo con `gh api rate_limit` antes de empezar |
| Aviso de `git branch -d` tras un squash | Git ve el commit squash como distinto al de la rama local | Es normal; el contenido ya está en `main` |
| Conteo esperado de issues | `gh issue list` no incluye los PR | Son 32 issues, no 35 |

Lecciones: verificar versiones de paquetes antes de recomendarlos (MassTransit); preferir IDs de GraphQL a operaciones por nombre cuando se procesan muchos elementos; y revisar la ruta exacta de los archivos antes de construir.

---

## 6. Verificaciones

| Verificación | Resultado |
|---|---|
| `dotnet --version` dentro del repo | `10.0.404` |
| `dotnet build` | 0 errores, 0 advertencias |
| `git status` tras el primer commit | Sin `bin/` ni `obj/`; `.env` ignorado (`git check-ignore -v .env`) |
| Push directo a `main` | Rechazado (GH006) |
| `docker compose ps` | RabbitMQ y SQL Server `Up (healthy)` |
| Panel de RabbitMQ en `localhost:15672` | Abre e inicia sesión |
| `@@VERSION` del contenedor | `17.0.5065.9` (CU11), Linux, sin cambios respecto a la imagen base |
| `IsFullTextInstalled` | `1` |
| Idioma español de Full-Text Search | `3082 Spanish` disponible |
| SSMS a `127.0.0.1,1433` | Conecta al contenedor (`on Linux`) |
| Issues del backlog | 32 |
| Ítems en el proyecto `NexTalent` | 32, con campos llenos |

---

## 7. Historial de Git

| Commit | PR | Descripción |
|---|---|---|
| `aa5fc78`, `efdc96d` | — | Historial heredado de una estructura anterior del proyecto (el repo se vació en lugar de recrearse) |
| `4f3226a` | — | Estructura inicial de la solución |
| `6f602bf` (`3a8de28`) | #1 | README, guía de contribución y plantilla de PR (merge commit) |
| `1f4df1a` | #2 | RabbitMQ en el compose |
| `0861884` | #3 | SQL Server con Full-Text Search en el compose |
| `7bc47ed` | #36 | Backlog en CSV y script de importación |

---

## 8. Estructura del repositorio al cierre

```
NexTalent/
├─ .github/
│  └─ pull_request_template.md
├─ docker/
│  └─ sqlserver/
│     └─ Dockerfile
├─ src/
│  └─ NexTalent.Contracts/
│     └─ NexTalent.Contracts.csproj
├─ tools/
│  └─ backlog/
│     ├─ Import-Backlog.ps1
│     └─ backlog.csv
├─ .editorconfig
├─ .env.example          (el .env real existe solo en local)
├─ .gitattributes
├─ .gitignore
├─ CONTRIBUTING.md
├─ Directory.Build.props
├─ docker-compose.yml
├─ global.json
├─ NexTalent.sln
└─ README.md
```

---

## 9. Pendientes y dependencias

| Pendiente | Responsable | Notas |
|---|---|---|
| Decidir MassTransit (8.x, alternativa o licencia v9) | Backend 1 + Backend 2 | Antes de la semana 6 |
| 2 invitaciones al repo sin aceptar | Compañeros | Las invitaciones expiran a los 7 días; reenviar si hace falta |
| Dar acceso al proyecto de GitHub | Backend 1 | Manage access, después de que acepten |
| Avisar a BD y a Backend 2 que el compose ya está disponible | Backend 1 | Instrucciones en la sección 3.5 |
| Requisitos de datos y borrador E-R | BD | Para definir las tablas de cada esquema |
| Aclarar quién construye el perfil del candidato (HU-02.x) | Equipo | El backlog lo asigna a Identidad, pero el documento no lo lista en ese servicio |
| Aclarar quién ejecuta la caducidad diaria de vacantes (HU-03.03) | Backend 1 + Backend 2 | Hangfire vive en Notificaciones; el backlog pone el trabajo en Vacantes |
| Opcional: borrar el proyecto 1 vacío de GitHub | Backend 1 | `@AngelMontws's untitled project` |
| Opcional: agregar la columna "In review" al campo Status | Backend 1 | Se hace desde la web |

Lo que Backend 2 y BD esperan de Backend 1: RabbitMQ y SQL Server corriendo en Docker (listo) y la plantilla de servicio (semana 2).

---

## 10. Plan de la semana 2

| Paso | Queda hecho | Verificación |
|---|---|---|
| 2.1 Plantilla Clean Architecture | Proyectos Api / Application / Domain / Infrastructure de Identidad con las referencias correctas; plantilla `WeatherForecast` y `Class1.cs` eliminados; `Directory.Packages.props` con versiones verificadas | `dotnet build` sin errores |
| 2.2 `/health`, Swagger y logs | Endpoint de salud, Swagger y logs con id de correlación | `curl` a `/health` responde 200; el log muestra el id |
| 2.3 Prueba xUnit de Domain | Una regla de dominio de ejemplo probada sin base de datos | `dotnet test` en verde |
| 2.4 Clonar la plantilla | Esqueletos de Vacantes, Postulaciones y Notificaciones; Gateway con YARP mínimo | Todo compila; el Gateway enruta hacia Identidad |
| 2.5 Compose completo | Dockerfiles de servicios, puertos 5000–5004 y 4 esquemas vacíos en SQL Server (`identidad`, `vacantes`, `postulaciones`, `notificaciones`) | `docker compose up --build` y `/health` a través del gateway |
| 2.6 Cierre | README para BD y Backend 2 con cadena de conexión | "Cierra la semana 2" |

Al terminar la semana 2 se cierra la fase 0 completa.
