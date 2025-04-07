FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY ["Fluentis Core.sln", "."]
COPY ["Fluentis Core.csproj", "."]
RUN dotnet restore "Fluentis Core.csproj"

# Copiar el código y publicar
COPY "." "."
RUN dotnet publish -c Release -o /app/publish

# ===== ETAPA DE EJECUCIÓN =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Configuración para HTTP (equivalente al perfil "http" de launchSettings.json)
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80 

EXPOSE 80  
ENTRYPOINT ["dotnet", "Fluentis Core.dll"]