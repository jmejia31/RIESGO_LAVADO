# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY backend/RL.API/RL.API.csproj backend/RL.API/
RUN dotnet restore backend/RL.API/RL.API.csproj

FROM restore AS publish
COPY backend/RL.API/ backend/RL.API/
RUN dotnet publish backend/RL.API/RL.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs /app/App_Data/Evidencias \
    && chown -R app:app /app
COPY --from=publish --chown=app:app /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail --silent --show-error http://127.0.0.1:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "RL.API.dll"]
