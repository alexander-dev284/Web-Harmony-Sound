# Dockerfile de la API (contexto de build = raíz del repositorio)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore HarmonySound.API/HarmonySound.API.csproj
RUN dotnet publish HarmonySound.API/HarmonySound.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render inyecta el puerto en la variable PORT; si no existe usa 8080
ENTRYPOINT dotnet HarmonySound.API.dll --urls "http://0.0.0.0:${PORT:-8080}"
