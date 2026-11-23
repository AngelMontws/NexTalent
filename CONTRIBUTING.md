# Guía de contribución

## Reglas básicas

1. Nadie hace commit ni push directo a `main`.
2. Todo cambio entra por Pull Request (PR) y necesita la aprobación de **otra persona** del equipo.
3. Todo cambio al esquema de la base de datos lo revisa también el encargado de BD.
4. Nunca se suben contraseñas, tokens ni archivos `.env`. Solo se sube `.env.example` con valores de ejemplo.

## Ramas

Se crea una rama por función o corrección, a partir de `main` actualizada:

```powershell
git switch main
git pull
git switch -c feature/vacantes-crud
```

Formato: `<tipo>/<descripcion-corta-en-minusculas>`

| Tipo | Cuándo se usa | Ejemplo |
|---|---|---|
| `feature/` | Función nueva | `feature/identidad-login` |
| `fix/` | Corrección de un error | `fix/salario-validacion` |
| `docs/` | Solo documentación | `docs/readme-puertos` |
| `chore/` | Configuración, dependencias, Docker | `chore/compose-rabbitmq` |
| `test/` | Solo pruebas | `test/maquina-estados` |

## Commits

Formato: `tipo: descripción corta en español, en presente`.

Ejemplos:

- `feat: agrega validación de rango salarial`
- `fix: corrige caducidad de vacantes a 30 días`
- `docs: actualiza puertos en el README`
- `chore: agrega servicio de RabbitMQ al compose`

## Pull Requests

1. Sube tu rama con `git push -u origin <tu-rama>` y abre el PR hacia `main`.
2. Llena la plantilla del PR (se carga sola).
3. Pide revisión a otra persona. Quien revisa no puede ser quien escribió el código.
4. Antes de pedir revisión, `dotnet build` y `dotnet test` deben pasar sin errores.
5. Al aprobarse, el PR se une con **Squash and merge** y se borra la rama.
6. Mantén los PR pequeños: un cambio claro por PR es más rápido de revisar.

## Tablero

Las tareas viven en GitHub Projects. Cada PR debe mencionar la tarea que atiende (por ejemplo, `HU-03.01`).