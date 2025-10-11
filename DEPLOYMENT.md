# 🚀 Guía de Configuración CI/CD para Fluentis Backend

## 📋 Tabla de Contenidos
1. [Requisitos Previos](#requisitos-previos)
2. [Configuración de Azure](#configuración-de-azure)
3. [Configuración de GitHub Secrets](#configuración-de-github-secrets)
4. [Variables que Cambiar en el Código](#variables-que-cambiar-en-el-código)
5. [Testing del Pipeline](#testing-del-pipeline)
6. [Troubleshooting](#troubleshooting)

---

## 📦 Requisitos Previos

- Cuenta de Azure for Students activa
- Azure CLI instalado: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
- Acceso admin al repositorio de GitHub
- Visual Studio Code o editor de texto

---

## ⚙️ Configuración de Azure

### 1. Login a Azure CLI

```bash
az login
```

### 2. Crear Grupo de Recursos

```bash
az group create \
  --name rg-fluentis \
  --location eastus
```

### 3. Crear SQL Server

```bash
az sql server create \
  --name fluentis-sqlserver \
  --resource-group rg-fluentis \
  --location eastus \
  --admin-user adminfluentis \
  --admin-password "TuPasswordSeguro123!"
```

⚠️ **IMPORTANTE**: Guarda este password en un lugar seguro.

### 4. Crear Bases de Datos (Pruebas y Producción)

```bash
# Base de datos de PRUEBAS
az sql db create \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver \
  --name fluentis-db-pruebas \
  --service-objective Basic \
  --backup-storage-redundancy Local

# Base de datos de PRODUCCIÓN
az sql db create \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver \
  --name fluentis-db-prod \
  --service-objective Basic \
  --backup-storage-redundancy Local
```

### 5. Configurar Firewall de SQL Server

```bash
# Permitir servicios de Azure
az sql server firewall-rule create \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Permitir tu IP local (para testing)
az sql server firewall-rule create \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver \
  --name AllowMyIP \
  --start-ip-address TU_IP_PUBLICA \
  --end-ip-address TU_IP_PUBLICA
```

Para obtener tu IP pública: https://www.whatismyip.com/

### 6. Obtener Connection Strings

```bash
# Connection String para PRUEBAS
az sql db show-connection-string \
  --client ado.net \
  --name fluentis-db-pruebas \
  --server fluentis-sqlserver

# Connection String para PRODUCCIÓN
az sql db show-connection-string \
  --client ado.net \
  --name fluentis-db-prod \
  --server fluentis-sqlserver
```

📝 **Guarda estos connection strings**, los necesitarás para los secrets de GitHub.

El formato será:
```
Server=tcp:fluentis-sqlserver.database.windows.net,1433;Initial Catalog=fluentis-db-pruebas;Persist Security Info=False;User ID=adminfluentis;Password=TuPasswordSeguro123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 7. Crear App Service Plans

```bash
# Plan para PRUEBAS (Free tier)
az appservice plan create \
  --name plan-fluentis-pruebas \
  --resource-group rg-fluentis \
  --sku F1 \
  --is-linux

# Plan para PRODUCCIÓN (Basic tier - más recursos)
az appservice plan create \
  --name plan-fluentis-prod \
  --resource-group rg-fluentis \
  --sku B1 \
  --is-linux
```

### 8. Crear Web Apps

```bash
# Web App de PRUEBAS
az webapp create \
  --resource-group rg-fluentis \
  --plan plan-fluentis-pruebas \
  --name fluentis-pruebas \
  --runtime "DOTNET:9.0"

# Web App de PRODUCCIÓN
az webapp create \
  --resource-group rg-fluentis \
  --plan plan-fluentis-prod \
  --name fluentis-prod \
  --runtime "DOTNET:9.0"
```

⚠️ **NOTA**: Los nombres `fluentis-pruebas` y `fluentis-prod` deben ser únicos globalmente en Azure. Si están tomados, usa: `fluentis-tuequipo-pruebas`

### 9. Configurar Variables de Entorno en Azure Web Apps

#### Para PRUEBAS:

```bash
az webapp config appsettings set \
  --resource-group rg-fluentis \
  --name fluentis-pruebas \
  --settings \
    ConnectionStrings__DefaultConnection="Server=tcp:fluentis-sqlserver.database.windows.net,1433;Initial Catalog=fluentis-db-pruebas;User ID=adminfluentis;Password=TuPasswordSeguro123!;Encrypt=True;TrustServerCertificate=False;" \
    AzureAd__ClientSecret="HmV8Q~FcOqPZuhLdbLun3lz3rdXUPL-0cnYODaEC" \
    Cors__AllowedOrigins="https://tu-frontend-pruebas.vercel.app,http://localhost:3000,http://localhost:5173" \
    ASPNETCORE_ENVIRONMENT="Production"
```

#### Para PRODUCCIÓN:

```bash
az webapp config appsettings set \
  --resource-group rg-fluentis \
  --name fluentis-prod \
  --settings \
    ConnectionStrings__DefaultConnection="Server=tcp:fluentis-sqlserver.database.windows.net,1433;Initial Catalog=fluentis-db-prod;User ID=adminfluentis;Password=TuPasswordSeguro123!;Encrypt=True;TrustServerCertificate=False;" \
    AzureAd__ClientSecret="HmV8Q~FcOqPZuhLdbLun3lz3rdXUPL-0cnYODaEC" \
    Cors__AllowedOrigins="https://tu-frontend-produccion.com" \
    ASPNETCORE_ENVIRONMENT="Production"
```

⚠️ **IMPORTANTE**: Reemplaza:
- `TuPasswordSeguro123!` con tu password real
- URLs de frontend con las URLs reales de tu aplicación

### 10. Obtener Publish Profiles

```bash
# Para PRUEBAS
az webapp deployment list-publishing-profiles \
  --name fluentis-pruebas \
  --resource-group rg-fluentis \
  --xml > publish-profile-pruebas.xml

# Para PRODUCCIÓN
az webapp deployment list-publishing-profiles \
  --name fluentis-prod \
  --resource-group rg-fluentis \
  --xml > publish-profile-prod.xml
```

📝 **Guarda el contenido de estos archivos XML**, los necesitarás para GitHub Secrets.

---

## 🔐 Configuración de GitHub Secrets

### 1. Ir a tu repositorio en GitHub

`https://github.com/Fluentis-BPM/fluentis-backend`

### 2. Navegar a Settings → Secrets and variables → Actions

### 3. Crear los siguientes Repository Secrets:

Click en **"New repository secret"** para cada uno:

#### Secrets para PRUEBAS:

| Nombre del Secret | Valor | Descripción |
|-------------------|-------|-------------|
| `AZURE_WEBAPP_NAME_PRUEBAS` | `fluentis-pruebas` | Nombre de tu Web App de pruebas |
| `AZURE_PUBLISH_PROFILE_PRUEBAS` | `<contenido del XML>` | Todo el contenido de `publish-profile-pruebas.xml` |
| `AZURE_SQL_CONNECTION_STRING_PRUEBAS` | `Server=tcp:fluentis-sqlserver.database.windows.net,1433;Initial Catalog=fluentis-db-pruebas;User ID=adminfluentis;Password=TuPasswordSeguro123!;Encrypt=True;` | Connection string de pruebas |

#### Secrets para PRODUCCIÓN:

| Nombre del Secret | Valor | Descripción |
|-------------------|-------|-------------|
| `AZURE_WEBAPP_NAME_PROD` | `fluentis-prod` | Nombre de tu Web App de producción |
| `AZURE_PUBLISH_PROFILE_PROD` | `<contenido del XML>` | Todo el contenido de `publish-profile-prod.xml` |
| `AZURE_SQL_CONNECTION_STRING_PROD` | `Server=tcp:fluentis-sqlserver.database.windows.net,1433;Initial Catalog=fluentis-db-prod;User ID=adminfluentis;Password=TuPasswordSeguro123!;Encrypt=True;` | Connection string de producción |

### 4. Crear Environments (Opcional pero Recomendado)

Ve a **Settings → Environments** y crea:

1. **pruebas**
   - No requiere aprobación manual
   
2. **produccion**
   - ✅ Requiere aprobación manual (Required reviewers: tu usuario)
   - Esto evitará deploys accidentales a producción

---

## 🔄 Variables que Cambiar en el Código

### 1. ✅ `appsettings.Production.json` (Ya creado)

Este archivo ya está configurado. Azure sobrescribirá los valores con las variables de entorno.

### 2. ⚠️ CORS en `Program.cs` (Líneas 15-30)

**Estado actual:**
```csharp
if (builder.Environment.IsDevelopment())
{
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}
else
{
    policy.WithOrigins("http://localhost:5173") // ❌ NO ES TU FRONTEND DE PRODUCCIÓN
          .AllowAnyHeader()
          .AllowAnyMethod();
}
```

**Cambio recomendado:**
```csharp
if (builder.Environment.IsDevelopment())
{
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}
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

Esto leerá los orígenes desde la variable de entorno configurada en Azure.

### 3. ⚠️ URL del Frontend

Cuando configures Azure (paso 9 arriba), cambia:
```
Cors__AllowedOrigins="https://tu-frontend-produccion.com"
```

Por la URL real de tu frontend desplegado (Vercel, Netlify, Azure Static Web Apps, etc.)

### 4. ✅ Connection Strings

Ya están configuradas para leerse desde variables de entorno. No necesitas cambiar nada en el código.

### 5. ✅ Azure AD Configuration

Ya está en `appsettings.json`. Azure sobrescribirá el `ClientSecret` desde variables de entorno.

---

## 🧪 Testing del Pipeline

### 1. Hacer Push a la Rama `develop` (para probar CI)

```bash
git add .
git commit -m "feat: configurar CI/CD con GitHub Actions"
git push origin develop
```

Ve a **GitHub → Actions** y verifica que el workflow `CI Backend` se ejecute correctamente.

### 2. Merge a `main` (para ejecutar CD)

```bash
git checkout main
git merge develop
git push origin main
```

El workflow `CD Backend` se ejecutará automáticamente:
1. ✅ Desplegará a PRUEBAS
2. ⏸️ Esperará aprobación manual (si configuraste el environment)
3. ✅ Desplegará a PRODUCCIÓN

### 3. Verificar Deployment

```bash
# Probar API de PRUEBAS
curl https://fluentis-pruebas.azurewebsites.net/api/solicitudes

# Probar API de PRODUCCIÓN
curl https://fluentis-prod.azurewebsites.net/api/solicitudes
```

---

## 🐛 Troubleshooting

### Error: "Database connection failed"

**Solución:**
```bash
# Verifica las reglas de firewall
az sql server firewall-rule list \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver

# Agrega la IP de Azure Web App al firewall
az sql server firewall-rule create \
  --resource-group rg-fluentis \
  --server fluentis-sqlserver \
  --name AllowWebApp \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Error: "CORS policy blocked"

**Solución:**
Verifica que la URL del frontend esté en la variable `Cors__AllowedOrigins` de Azure Web App:

```bash
az webapp config appsettings list \
  --resource-group rg-fluentis \
  --name fluentis-pruebas \
  --query "[?name=='Cors__AllowedOrigins']"
```

### Error: "Migrations failed to apply"

**Solución:**
Conéctate a la base de datos y verifica manualmente:

```bash
# Instalar sqlcmd (si no lo tienes)
# Windows: https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility

sqlcmd -S fluentis-sqlserver.database.windows.net \
  -d fluentis-db-pruebas \
  -U adminfluentis \
  -P "TuPasswordSeguro123!" \
  -Q "SELECT * FROM __EFMigrationsHistory"
```

### Error: "dotnet ef command not found"

El workflow ya instala `dotnet-ef` automáticamente. Si persiste, agrega al `cd.yml`:

```yaml
- name: Install EF Core Tools (Global)
  run: |
    dotnet tool install --global dotnet-ef
    echo "$HOME/.dotnet/tools" >> $GITHUB_PATH
```

---

## 💰 Costos Estimados (Azure for Students)

| Recurso | Tier | Costo Mensual |
|---------|------|---------------|
| SQL Database (Pruebas) | Basic | ~$5 |
| SQL Database (Producción) | Basic | ~$5 |
| App Service (Pruebas) | F1 (Free) | $0 |
| App Service (Producción) | B1 (Basic) | ~$13 |
| **TOTAL** | | **~$23/mes** |

Con el crédito de $100 de Azure for Students, puedes mantener esto por **4+ meses**.

---

## 📚 Recursos Adicionales

- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [GitHub Actions for Azure](https://github.com/marketplace?type=actions&query=azure)
- [EF Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Azure App Service for .NET](https://docs.microsoft.com/en-us/azure/app-service/)

---

## ✅ Checklist Final

Antes de hacer deploy a producción, verifica:

- [ ] Todos los secrets de GitHub están configurados
- [ ] Connection strings de Azure SQL están correctos
- [ ] CORS permite tu frontend de producción
- [ ] ClientSecret de Azure AD está en variables de entorno
- [ ] Migrations se ejecutan correctamente en local
- [ ] Tests pasan en el pipeline de CI
- [ ] Firewall de SQL Server permite conexiones de Azure
- [ ] Variables de entorno están configuradas en Azure Web App

---

🎉 **¡Listo! Tu backend ahora tiene CI/CD completo con Azure.**

Si tienes problemas, revisa los logs en:
- **GitHub Actions**: https://github.com/Fluentis-BPM/fluentis-backend/actions
- **Azure Portal**: https://portal.azure.com → App Services → Logs
