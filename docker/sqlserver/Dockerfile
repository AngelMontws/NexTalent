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