FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY ["FluentisCore.sln", "."]
COPY ["FluentisCore/FluentisCore.csproj", "FluentisCore/"]
ARG HUSKY=0
ENV HUSKY=${HUSKY}
RUN dotnet restore "FluentisCore/FluentisCore.csproj"

# Copiar el código y publicar
COPY "FluentisCore/." "FluentisCore/"
WORKDIR "/src/FluentisCore"
RUN dotnet publish -c Release -o /app/publish

# ===== ETAPA DE EJECUCIÓN =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Configuración para HTTP (equivalente al perfil "http" de launchSettings.json)
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80 

EXPOSE 80  
ENTRYPOINT ["dotnet", "FluentisCore.dll"]