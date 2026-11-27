# 04 — Manual de Despliegue Code-First (`NexTalentDb`)

Procedimiento reproducible para inicializar, migrar, optimizar, poblar y respaldar la base de datos de NexTalent bajo **Entity Framework Core Code-First** en SQL Server sobre Docker.

---

## 4.1 Prerrequisitos y Configuración Segura

* **Herramientas:** .NET 10 SDK (`global.json`), Docker Desktop con Docker Compose v2, `dotnet-ef` (`dotnet tool restore`) y `sqlcmd`.
* **Sin credenciales en el repositorio:** Ningún `appsettings.json` ni fábrica de diseño almacena contraseñas reales. Copiar `.env.example` a `.env` y definir contraseñas locales.
* **Bandera `-I` obligatoria en `sqlcmd`:** Los índices filtrados de MassTransit e `Identidad` / `Notificaciones` requieren `SET QUOTED_IDENTIFIER ON`. Siempre incluir `-I` al invocar `sqlcmd`.

---

## 4.2 Aislamiento de Historial de Migraciones

Cada microservicio registra su `DbContext` aislando su tabla `__EFMigrationsHistory` en su propio esquema:

* `IdentidadDbContext` → `identidad.__EFMigrationsHistory`
* `VacantesDbContext` → `vacantes.__EFMigrationsHistory`
* `PostulacionesDbContext` → `postulaciones.__EFMigrationsHistory`
* `NotificacionesDbContext` → `notificaciones.__EFMigrationsHistory`

---

## 4.3 Procedimiento Secuencial de Despliegue desde Cero

### Paso 1 — Levantar infraestructura base

```powershell
Copy-Item .env.example .env   # editar contraseñas antes de levantar
docker compose up -d sqlserver sqlserver-init rabbitmq
docker compose ps -a
```

### Paso 2 — Inicializar base de datos y esquemas (manual si no se usó `sqlserver-init`)

```bash
docker exec -i nextalent-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -I \
  -i /init/crear-esquemas.sql
```

### Paso 3 — Aplicar migraciones Code-First (o scripts idempotentes del Paso 4)

Con `ConnectionStrings__DefaultConnection` apuntando a `127.0.0.1,1433`:

```powershell
dotnet tool restore

dotnet ef database update --project src/Services/Identidad/NexTalent.Identidad.Infrastructure --startup-project src/Services/Identidad/NexTalent.Identidad.Api --context IdentidadDbContext
dotnet ef database update --project src/Services/Vacantes/NexTalent.Vacantes.Infrastructure --startup-project src/Services/Vacantes/NexTalent.Vacantes.Api --context VacantesDbContext
dotnet ef database update --project src/Services/Postulaciones/NexTalent.Postulaciones.Infrastructure --startup-project src/Services/Postulaciones/NexTalent.Postulaciones.Api --context PostulacionesDbContext
dotnet ef database update --project src/Services/Notificaciones/NexTalent.Notificaciones.Infrastructure --startup-project src/Services/Notificaciones/NexTalent.Notificaciones.Api --context NotificacionesDbContext
```

### Paso 4 — Exportar y/o ejecutar scripts SQL idempotentes (`01_Produccion_Idempotente_<Servicio>.sql`)

Para regenerar los cuatro scripts idempotentes versionados en `/Database/Scripts`:

```powershell
dotnet ef migrations script --idempotent --project src/Services/Identidad/NexTalent.Identidad.Infrastructure --startup-project src/Services/Identidad/NexTalent.Identidad.Api --context IdentidadDbContext --output Database/Scripts/01_Produccion_Idempotente_Identidad.sql
dotnet ef migrations script --idempotent --project src/Services/Vacantes/NexTalent.Vacantes.Infrastructure --startup-project src/Services/Vacantes/NexTalent.Vacantes.Api --context VacantesDbContext --output Database/Scripts/01_Produccion_Idempotente_Vacantes.sql
dotnet ef migrations script --idempotent --project src/Services/Postulaciones/NexTalent.Postulaciones.Infrastructure --startup-project src/Services/Postulaciones/NexTalent.Postulaciones.Api --context PostulacionesDbContext --output Database/Scripts/01_Produccion_Idempotente_Postulaciones.sql
dotnet ef migrations script --idempotent --project src/Services/Notificaciones/NexTalent.Notificaciones.Infrastructure --startup-project src/Services/Notificaciones/NexTalent.Notificaciones.Api --context NotificacionesDbContext --output Database/Scripts/01_Produccion_Idempotente_Notificaciones.sql
```

Para ejecutarlos directamente con `sqlcmd` (incluyendo siempre `-I`):

```bash
sqlcmd -S 127.0.0.1,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -I -d NexTalentDb -i Database/Scripts/01_Produccion_Idempotente_Identidad.sql
sqlcmd -S 127.0.0.1,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -I -d NexTalentDb -i Database/Scripts/01_Produccion_Idempotente_Vacantes.sql
sqlcmd -S 127.0.0.1,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -I -d NexTalentDb -i Database/Scripts/01_Produccion_Idempotente_Postulaciones.sql
sqlcmd -S 127.0.0.1,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -I -d NexTalentDb -i Database/Scripts/01_Produccion_Idempotente_Notificaciones.sql
```

### Paso 5 — Aplicar objetos extra (`02_Objetos_Extra.sql`)

Crea índices RNF-01, catálogo Full-Text `ftc_NexTalent`, procedimiento `vacantes.sp_BuscarVacantes`, vistas `vw_ConteoPostulacionesEmpresa` y `vw_IndiceRespuestaEmpresa`, triggers `INSTEAD OF UPDATE, DELETE` (`THROW 51000`) y permisos `GRANT`/`DENY` por servicio:

```bash
docker cp Database/Scripts/02_Objetos_Extra.sql nextalent-sqlserver:/tmp/02_Objetos_Extra.sql
docker exec -i nextalent-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -I -d NexTalentDb \
  -i /tmp/02_Objetos_Extra.sql
```

### Paso 6 — Ejecutar Seeders Determinísticos y levantar servicios

Con `DatabaseSeed__RunInitializers=true` en entorno `Development`, cada `*DbInitializer` carga de manera idempotente las 2 empresas verificadas, 2 reclutadores, 4 candidatos, catálogo de habilidades y 3 vacantes usando los mismos GUIDs determinísticos compartidos:

```powershell
docker compose up -d --build
```

Para poblar 50,000 vacantes de prueba (validación RNF-01 `< 200 ms`), activar temporalmente `DatabaseSeed__SeedBenchmark50k=true` en el contenedor `vacantes`.

### Paso 7 — Respaldo limpio (`DB_NexTalent_Seeded_v1.0.bak`)

```bash
docker exec -i nextalent-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -I \
  -Q "BACKUP DATABASE [NexTalentDb] TO DISK = N'/var/opt/mssql/backup/DB_NexTalent_Seeded_v1.0.bak' WITH FORMAT, INIT, COMPRESSION, STATS = 25;"
docker cp nextalent-sqlserver:/var/opt/mssql/backup/DB_NexTalent_Seeded_v1.0.bak ./Database/Backups/DB_NexTalent_Seeded_v1.0.bak
```

---

## 4.4 Verificación Post-Despliegue

1. **RNF-10 (`/health`):** `GET http://localhost:5000/health` y `/health/<servicio>` responden `200 OK` (`Healthy`).
2. **RN-02 (Salario obligatorio en motor):** `INSERT` en `vacantes.vacante` con `salario_min = 0` o `salario_max < salario_min` falla con error SQL `547` (`CK_Vacante_SalarioMin_Positivo` / `CK_Vacante_SalarioMax_Valido`).
3. **HU-05.04 / HU-08.03 (Inmutabilidad):** `UPDATE` o `DELETE` sobre `postulaciones.historial_estado`, `vacantes.bitacora_moderacion` o `identidad.bitacora_moderacion_empresa` falla con error `51000`.
4. **RNF-01 (Rendimiento):** `EXEC vacantes.sp_BuscarVacantes` con 3 filtros responde en `< 200 ms` sobre 50,000 vacantes usando `IX_Vacante_Busqueda`.

---

## 4.5 Gobierno de Cambios de Esquema y Estrategia de Rollback

* **Control de versiones de esquema:** Ninguna modificación a entidades, configuraciones Fluent API o scripts SQL se integra a `main` sin Pull Request revisado y aprobado por Base de Datos. Toda nueva migración debe implementar su método `Down()` completo y regenerar su archivo `01_Produccion_Idempotente_<Servicio>.sql`.
* **Rollback:** Revertir el esquema afectado a la migración estable previa mediante `dotnet ef database update <NombreMigracionAnterior>` indicando proyecto y contexto, o restaurar la base completa desde `DB_NexTalent_Seeded_v1.0.bak` con `WITH REPLACE` tras detener temporalmente los contenedores de aplicación.
