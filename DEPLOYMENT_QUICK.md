# 🚀 Resumen Rápido: Cambios Necesarios para CI/CD

## 📚 Guías Disponibles

1. **`EXPLICACION_CICD.md`** - ¿Qué hace cada GitHub Action? (LEER PRIMERO)
2. **`DEPLOYMENT_AZURE_PORTAL.md`** - Configurar todo desde Azure Portal Web (RECOMENDADO)
3. **`DEPLOYMENT.md`** - Configurar con Azure CLI (para avanzados)
4. **Este archivo** - Resumen rápido

## ✅ Archivos Creados

1. **`.github/workflows/ci.yml`** - Pipeline de CI (build y tests)
2. **`.github/workflows/cd.yml`** - Pipeline de CD (deploy a Azure)
3. **`FluentisCore/appsettings.Production.json`** - Configuración de producción

## 🔧 Cambios Requeridos en el Código

### 1. CORS en `Program.cs` (Líneas 15-30)

**❌ ANTES:**
```csharp
else
{
    policy.WithOrigins("http://localhost:5173") // Hardcoded!
          .AllowAnyHeader()
          .AllowAnyMethod();
}
```

**✅ DESPUÉS:**
```csharp
else
{
    var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
        ?.Split(',', StringSplitOptions.RemoveEmptyEntries) 
        ?? Array.Empty<string>();
    
    policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
}
```

---

## 🔐 GitHub Secrets a Configurar

Ve a: **Settings → Secrets and variables → Actions**

### Para Pruebas:
```
AZURE_WEBAPP_NAME_PRUEBAS = "fluentis-pruebas"
AZURE_PUBLISH_PROFILE_PRUEBAS = <XML del publish profile>
AZURE_SQL_CONNECTION_STRING_PRUEBAS = "Server=tcp:fluentis-sqlserver.database.windows.net,1433;..."
```

### Para Producción:
```
AZURE_WEBAPP_NAME_PROD = "fluentis-prod"
AZURE_PUBLISH_PROFILE_PROD = <XML del publish profile>
AZURE_SQL_CONNECTION_STRING_PROD = "Server=tcp:fluentis-sqlserver.database.windows.net,1433;..."
```

---

## 🎯 Variables de Entorno en Azure

Después de crear tu Web App, configura:

```bash
az webapp config appsettings set \
  --resource-group rg-fluentis \
  --name fluentis-pruebas \
  --settings \
    ConnectionStrings__DefaultConnection="Server=tcp:..." \
    AzureAd__ClientSecret="HmV8Q~..." \
    Cors__AllowedOrigins="https://tu-frontend.vercel.app" \
    ASPNETCORE_ENVIRONMENT="Production"
```

**⚠️ IMPORTANTE:** Reemplaza `https://tu-frontend.vercel.app` con la URL real de tu frontend.

---

## 📋 Pasos Rápidos de Deployment

1. **Crear recursos en Azure** (ver `DEPLOYMENT.md` completo)
2. **Configurar secrets en GitHub**
3. **Hacer el cambio de CORS en `Program.cs`**
4. **Push a `main`:**
   ```bash
   git add .
   git commit -m "feat: setup CI/CD"
   git push origin main
   ```
5. **Ver el deploy en GitHub Actions**

---

## 🐛 Troubleshooting Rápido

### Error de conexión a DB:
```bash
# Agregar regla de firewall para Azure
az sql server firewall-rule create \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Error de CORS:
Verifica que `Cors__AllowedOrigins` en Azure contenga la URL de tu frontend.

### Error de Migrations:
Revisa los logs en Azure Portal → App Service → Log stream

---

## 💡 URLs Útiles

- **Portal Azure:** https://portal.azure.com
- **GitHub Actions:** https://github.com/Fluentis-BPM/fluentis-backend/actions
- **API Pruebas:** https://fluentis-pruebas.azurewebsites.net
- **API Producción:** https://fluentis-prod.azurewebsites.net

---

Ver **`DEPLOYMENT.md`** para la guía completa detallada.
