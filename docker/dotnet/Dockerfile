# Dockerfile unico para el Gateway y los 4 servicios.
# Se construye desde la raiz del repo; los argumentos los pasa docker-compose.yml:
#   PROYECTO: ruta del proyecto a publicar (carpeta del .csproj)
#   ASSEMBLY: nombre del ensamblado (sin .dll)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROYECTO
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet publish "${PROYECTO}" -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG ASSEMBLY
ENV ASSEMBLY=${ASSEMBLY}
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet ${ASSEMBLY}.dll"]