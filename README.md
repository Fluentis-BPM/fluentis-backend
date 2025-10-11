# Fluentis Core

Backend para sistema de gestión de flujos de trabajo (BPM) desarrollado con ASP.NET Core 9.0.

## 🚀 CI/CD y Deployment

### 📚 Guías Disponibles

| Archivo | Descripción | Para quién |
|---------|-------------|-----------|
| **[EXPLICACION_CICD.md](EXPLICACION_CICD.md)** | Explicación detallada de qué hace cada GitHub Action | 👨‍🎓 Principiantes |
| **[DEPLOYMENT_AZURE_PORTAL.md](DEPLOYMENT_AZURE_PORTAL.md)** | Configurar todo desde el portal web de Azure | ✅ **RECOMENDADO** |
| **[DEPLOYMENT.md](DEPLOYMENT.md)** | Configurar con Azure CLI (línea de comandos) | 🔧 Avanzados |
| **[DEPLOYMENT_QUICK.md](DEPLOYMENT_QUICK.md)** | Resumen rápido de cambios necesarios | ⚡ Referencia rápida |

### 🔥 Inicio Rápido

1. **Lee primero:** [EXPLICACION_CICD.md](EXPLICACION_CICD.md) para entender cómo funciona
2. **Sigue:** [DEPLOYMENT_AZURE_PORTAL.md](DEPLOYMENT_AZURE_PORTAL.md) para configurar Azure
3. **Haz el cambio en `Program.cs`** (explicado en las guías)
4. **Push a `master`** y el deploy se ejecutará automáticamente

### ✅ Ambientes

- **Pruebas:** `https://fluentis-pruebas.azurewebsites.net`
- **Producción:** `https://fluentis-prod.azurewebsites.net`

---

## 🛠️ Tecnologías

- ASP.NET Core 9.0
- Entity Framework Core
- SQL Server
- Azure AD Authentication
- Azure SQL Database
- Azure App Service

## 📦 Estructura del Proyecto

```
FluentisCore/
├── Controllers/        # API Controllers
├── Models/            # Modelos de datos
├── DTO/               # Data Transfer Objects
├── Services/          # Lógica de negocio
├── Migrations/        # Migraciones de EF Core
└── appsettings.json   # Configuración
```

## 🔧 Desarrollo Local

### Requisitos

- .NET 9.0 SDK
- SQL Server (local o Docker)
- Visual Studio 2022 o VS Code

### Ejecutar localmente

```bash
# Restaurar dependencias
dotnet restore

# Ejecutar migraciones
dotnet ef database update --project FluentisCore

# Ejecutar aplicación
dotnet run --project FluentisCore
```

## 🧪 Testing

```bash
dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj
```

---

## 📝 Licencia

Este proyecto es parte de un proyecto académico de la Universidad del Valle de Guatemala.