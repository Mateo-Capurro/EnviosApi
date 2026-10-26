# Etapa 1: compilar y publicar con el SDK completo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo el .csproj: la capa del restore se cachea mientras no cambien los paquetes
COPY EnviosApi.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2: imagen final solo con el runtime de ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuario no root que ya trae la imagen oficial
USER $APP_UID

ENTRYPOINT ["dotnet", "EnviosApi.dll"]
