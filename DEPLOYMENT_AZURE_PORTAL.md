# 🌐 Guía de Configuración CI/CD - Portal de Azure (Web)

## 📋 Índice
1. [Requisitos Previos](#requisitos-previos)
2. [Crear SQL Server y Bases de Datos](#crear-sql-server-y-bases-de-datos)
3. [Crear App Services](#crear-app-services)
4. [Configurar Variables de Entorno](#configurar-variables-de-entorno)
5. [Obtener Connection Strings y Publish Profiles](#obtener-connection-strings-y-publish-profiles)
6. [Configurar GitHub Secrets](#configurar-github-secrets)
7. [Cambios en el Código](#cambios-en-el-código)
8. [Primer Deploy](#primer-deploy)

---

## 📦 Requisitos Previos

- ✅ Cuenta de Azure for Students activa
- ✅ Acceso al repositorio de GitHub como admin
- ✅ Este documento abierto mientras configuras

**⏱️ Tiempo estimado:** 30-45 minutos

---

## 🗄️ PASO 1: Crear SQL Server y Bases de Datos

### 1.1. Ir al Portal de Azure

1. Abre tu navegador y ve a: **https://portal.azure.com**
2. Inicia sesión con tu cuenta de estudiante

### 1.2. Crear Grupo de Recursos

1. En el buscador superior, escribe: **"Resource groups"**
2. Click en **"+ Create"**
3. Configuración:
   - **Subscription:** Azure for Students
   - **Resource group name:** `rg-fluentis`
   - **Region:** East US (o la más cercana)
4. Click en **"Review + create"** → **"Create"**

### 1.3. Crear SQL Server

1. En el buscador superior, escribe: **"SQL servers"**
2. Click en **"+ Create"**
3. **Basics:**
   - **Resource group:** `rg-fluentis`
   - **Server name:** `fluentis-sqlserver` (debe ser único globalmente)
   - **Location:** East US
   - **Authentication method:** Use SQL authentication
   - **Server admin login:** `adminfluentis`
   - **Password:** `TuPasswordSeguro123!` (⚠️ **GUARDA ESTE PASSWORD**)
   - **Confirm password:** `TuPasswordSeguro123!`
4. Click en **"Review + create"** → **"Create"**
5. ⏱️ **Espera 2-3 minutos** mientras se crea

### 1.4. Configurar Firewall del SQL Server

1. Ve al SQL Server recién creado: **Resource groups → rg-fluentis → fluentis-sqlserver**
2. En el menú izquierdo, busca **"Security" → "Networking"**
3. **Firewall rules:**
   - ✅ Marca: **"Allow Azure services and resources to access this server"**
4. Click en **"+ Add a firewall rule"** para agregar tu IP local:
   - **Rule name:** `MiPCLocal`
   - **Start IP:** (tu IP pública - búscala en https://www.whatismyip.com/)
   - **End IP:** (la misma IP)
5. Click en **"Save"**

### 1.5. Crear Base de Datos de PRUEBAS

1. Desde el SQL Server, click en **"+ Create database"** (arriba)
2. **Basics:**
   - **Database name:** `fluentis-db-pruebas`
   - **Server:** `fluentis-sqlserver` (ya seleccionado)
   - **Elastic pool:** No
   - **Workload environment:** Development
3. **Compute + storage:**
   - Click en **"Configure database"**
   - Selecciona: **"Basic"** (5 DTU, 2 GB) - ~$5/mes
   - **Backup storage redundancy:** Locally-redundant
   - Click en **"Apply"**
4. Click en **"Review + create"** → **"Create"**
5. ⏱️ **Espera 2-3 minutos**

### 1.6. Crear Base de Datos de PRODUCCIÓN

**Repite el paso 1.5 pero con:**
- **Database name:** `fluentis-db-prod`

### 1.7. Obtener Connection Strings

1. Ve a: **Resource groups → rg-fluentis → fluentis-db-pruebas**
2. En el menú izquierdo: **"Settings" → "Connection strings"**
3. Copia el **"ADO.NET (SQL authentication)"**
4. Se verá así:
   ```
   Server=tcp:fluentis-sqlserver.database.windows.net,1433;Initial Catalog=fluentis-db-pruebas;Persist Security Info=False;User ID=adminfluentis;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
5. **⚠️ IMPORTANTE:** Reemplaza `{your_password}` con `TuPasswordSeguro123!`
6. **📝 Guarda este connection string** en un archivo de texto temporal
7. **Repite para `fluentis-db-prod`**

---

## 🚀 PASO 2: Crear App Services

### 2.1. Crear App Service para PRUEBAS

1. En el buscador superior, escribe: **"App Services"**
2. Click en **"+ Create"**
3. **Basics:**
   - **Resource group:** `rg-fluentis`
   - **Name:** `fluentis-pruebas` (debe ser único globalmente)
   - **Publish:** Code
   - **Runtime stack:** .NET 9 (STS)
   - **Operating System:** Linux
   - **Region:** East US
4. **Pricing plans:**
   - Click en **"Explore pricing plans"**
   - Selecciona: **"Free F1"** (gratis para pruebas)
   - Click en **"Select"**
5. Click en **"Review + create"** → **"Create"**
6. ⏱️ **Espera 2-3 minutos**

### 2.2. Crear App Service para PRODUCCIÓN

**Repite el paso 2.1 pero con:**
- **Name:** `fluentis-prod`
- **Pricing plan:** **"Basic B1"** (~$13/mes, mejor rendimiento)

### 2.3. Obtener Publish Profiles

#### Para PRUEBAS:

1. Ve a: **Resource groups → rg-fluentis → fluentis-pruebas**
2. En la parte superior, click en **"Get publish profile"** (⬇️ icono de descarga)
3. Se descargará un archivo `.PublishSettings`
4. Ábrelo con Notepad/VSCode
5. **📝 Copia TODO el contenido XML** (desde `<publishData>` hasta `</publishData>`)
6. Guárdalo en un archivo temporal: `publish-profile-pruebas.txt`

#### Para PRODUCCIÓN:

1. **Repite el proceso anterior** para `fluentis-prod`
2. Guárdalo como: `publish-profile-prod.txt`

---

## ⚙️ PASO 3: Configurar Variables de Entorno en Azure

### 3.1. Configurar PRUEBAS

1. Ve a: **Resource groups → rg-fluentis → fluentis-pruebas**
2. En el menú izquierdo: **"Settings" → "Environment variables"**
3. Click en **"+ Add"** para cada variable:

#### Variable 1: Connection String
- **Name:** `ConnectionStrings__DefaultConnection`
- **Value:** (pega el connection string de pruebas que guardaste antes)
- **Type:** Connection string (SQL Azure)

#### Variable 2: Azure AD Client Secret
- **Name:** `AzureAd__ClientSecret`
- **Value:** `HmV8Q~FcOqPZuhLdbLun3lz3rdXUPL-0cnYODaEC`
- **Type:** Custom

#### Variable 3: CORS Origins
- **Name:** `Cors__AllowedOrigins`
- **Value:** `https://tu-frontend-pruebas.vercel.app,http://localhost:3000,http://localhost:5173`
- **Type:** Custom

⚠️ **IMPORTANTE:** Reemplaza `https://tu-frontend-pruebas.vercel.app` con la URL real de tu frontend de pruebas

#### Variable 4: Environment
- **Name:** `ASPNETCORE_ENVIRONMENT`
- **Value:** `Production`
- **Type:** Custom

4. Click en **"Apply"** (abajo) → **"Confirm"**

### 3.2. Configurar PRODUCCIÓN

**Repite el paso 3.1** para `fluentis-prod` pero con:
- Connection string de **PRODUCCIÓN**
- `Cors__AllowedOrigins`: Solo tu frontend de producción (sin localhost)
  - Ejemplo: `https://tu-frontend-prod.vercel.app`

---

## 🔐 PASO 4: Configurar GitHub Secrets

### 4.1. Ir a tu Repositorio en GitHub

1. Ve a: **https://github.com/Fluentis-BPM/fluentis-backend**
2. Click en **"Settings"** (arriba a la derecha)
3. En el menú izquierdo: **"Secrets and variables" → "Actions"**
4. Click en **"New repository secret"**

### 4.2. Crear Secrets para PRUEBAS

Crea los siguientes secrets (uno por uno):

#### Secret 1:
- **Name:** `AZURE_WEBAPP_NAME_PRUEBAS`
- **Value:** `fluentis-pruebas`
- Click en **"Add secret"**

#### Secret 2:
- **Name:** `AZURE_PUBLISH_PROFILE_PRUEBAS`
- **Value:** (pega TODO el contenido del archivo `publish-profile-pruebas.txt`)
- Click en **"Add secret"**

#### Secret 3:
- **Name:** `AZURE_SQL_CONNECTION_STRING_PRUEBAS`
- **Value:** (pega el connection string de pruebas)
- Click en **"Add secret"**

### 4.3. Crear Secrets para PRODUCCIÓN

**Repite el paso 4.2** con estos nombres:
- `AZURE_WEBAPP_NAME_PROD` → `fluentis-prod`
- `AZURE_PUBLISH_PROFILE_PROD` → (contenido del XML de prod)
- `AZURE_SQL_CONNECTION_STRING_PROD` → (connection string de prod)

---

## 🔧 PASO 5: Cambios en el Código

### 5.1. Modificar CORS en `Program.cs`

1. Abre: `FluentisCore/Program.cs`
2. Busca las líneas 15-30 (el bloque de CORS)
3. Reemplaza:

**❌ ANTES:**
```csharp
else
{
    Console.WriteLine("CORS configurado para producción: Permitiendo solo el origen específico de la aplicación frontend.");
    policy.WithOrigins("http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod();
}
```

**✅ DESPUÉS:**
```csharp
else
{
    Console.WriteLine("CORS configurado para producción: Leyendo orígenes desde configuración.");
    
    var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
        ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
        ?? Array.Empty<string>();
    
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    }
    else
    {
        Console.WriteLine("⚠️ ADVERTENCIA: No se configuraron orígenes permitidos para CORS.");
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }
}
```

4. Guarda el archivo

---

## 🚀 PASO 6: Hacer el Primer Deploy

### 6.1. Commit y Push

```powershell
git add .
git commit -m "feat: configurar CI/CD con Azure"
git push origin develop
```

### 6.2. Ver CI ejecutándose

1. Ve a: **https://github.com/Fluentis-BPM/fluentis-backend/actions**
2. Deberías ver el workflow **"CI Backend"** ejecutándose
3. Click en él para ver los logs
4. ✅ Verifica que todos los pasos pasen

### 6.3. Merge a master para Deploy

```powershell
git checkout master
git merge develop
git push origin master
```

### 6.4. Ver CD ejecutándose

1. Ve a: **https://github.com/Fluentis-BPM/fluentis-backend/actions**
2. Deberías ver el workflow **"CD Backend"** ejecutándose
3. Click en él para ver:
   - ✅ Job "deploy-pruebas"
   - ✅ Job "deploy-produccion" (después de pruebas)

### 6.5. Verificar el Deploy

#### Verificar Backend de PRUEBAS:
```
https://fluentis-pruebas.azurewebsites.net/api/solicitudes
```

#### Verificar Backend de PRODUCCIÓN:
```
https://fluentis-prod.azurewebsites.net/api/solicitudes
```

⚠️ Si ves un error 401/403, es normal (necesitas autenticación). Lo importante es que el servidor responda.

---

## 🎯 PASO 7: Configurar Frontend

### Si tu frontend está en Vercel/Netlify:

1. **Variables de entorno del frontend:**
   - `NEXT_PUBLIC_API_URL_PRUEBAS`: `https://fluentis-pruebas.azurewebsites.net`
   - `NEXT_PUBLIC_API_URL_PROD`: `https://fluentis-prod.azurewebsites.net`

2. **Obtener las URLs del frontend:**
   - Después de desplegar tu frontend, obtén sus URLs
   - Ejemplo: `https://fluentis-pruebas.vercel.app`

3. **Actualizar CORS en Azure:**
   - Ve a cada App Service en Azure
   - **Environment variables → Cors__AllowedOrigins**
   - Reemplaza el placeholder con las URLs reales de tu frontend
   - Click en **"Apply" → "Confirm"**
   - **Reinicia el App Service**: Click en **"Restart"** (arriba)

---

## ✅ Checklist Final

Antes de considerar que todo está listo:

- [ ] ✅ SQL Server creado con firewall configurado
- [ ] ✅ 2 bases de datos creadas (pruebas y prod)
- [ ] ✅ 2 App Services creados
- [ ] ✅ Variables de entorno configuradas en ambos App Services
- [ ] ✅ 6 GitHub Secrets creados
- [ ] ✅ CORS modificado en `Program.cs`
- [ ] ✅ CI pasa correctamente
- [ ] ✅ CD despliega a ambos ambientes
- [ ] ✅ Backend responde en las URLs de Azure
- [ ] ✅ CORS permite conexiones desde el frontend
- [ ] ✅ Migraciones se aplicaron correctamente

---

## 🧪 Testing en CI/CD

### ¿Por qué los tests de integración fallan en CI?

Los **tests de integración** (`IntegrationTests.cs`) requieren:
- ✅ Autenticación con Azure AD
- ✅ Conexión a base de datos real
- ✅ Credenciales reales de usuario

En el pipeline de CI (GitHub Actions), estos recursos **NO están disponibles** por diseño, ya que:
1. No queremos exponer credenciales reales en CI
2. Los tests de integración son lentos
3. CI debe ser rápido y no depender de servicios externos

### ¿Qué tests se ejecutan en CI?

Solo los **tests unitarios**:
- ✅ `UsuarioValidationTests` - Validaciones de modelo
- ✅ `UsuarioTests` - Operaciones CRUD simples
- ✅ `DepartamentoTests` - Operaciones CRUD simples

**Los tests de integración se ejecutan manualmente** en tu máquina local antes de hacer deploy.

### ¿Cómo ejecutar tests de integración localmente?

```bash
# Configurar credenciales de Azure AD (solo una vez)
dotnet user-secrets set "AzureAd:ClientId" "tu-client-id" --project FluentisCore.Tests
dotnet user-secrets set "AzureAd:ClientSecret" "tu-client-secret" --project FluentisCore.Tests
dotnet user-secrets set "AzureAd:TenantId" "tu-tenant-id" --project FluentisCore.Tests

# Ejecutar TODOS los tests (incluidos integración)
dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj

# Ejecutar SOLO tests unitarios (como en CI)
dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName!~IntegrationTests"

# Ejecutar SOLO tests de integración
dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName~IntegrationTests"
```

### Alternativa: Configurar credenciales en GitHub Secrets (Avanzado)

Si quieres ejecutar tests de integración en CI:

1. Crea secrets adicionales en GitHub:
   ```
   AZURE_AD_CLIENT_ID_TEST
   AZURE_AD_CLIENT_SECRET_TEST
   AZURE_AD_TENANT_ID_TEST
   ```

2. Modifica `ci.yml`:
   ```yaml
   - name: Run tests
     run: dotnet test --no-build --verbosity normal
     env:
       AzureAd__ClientId: ${{ secrets.AZURE_AD_CLIENT_ID_TEST }}
       AzureAd__ClientSecret: ${{ secrets.AZURE_AD_CLIENT_SECRET_TEST }}
       AzureAd__TenantId: ${{ secrets.AZURE_AD_TENANT_ID_TEST }}
   ```

⚠️ **No recomendado** para estudiantes: consume más tiempo de CI y es más complejo.

---

## 🐛 Troubleshooting

### Error: "Cannot connect to SQL Server"

**Solución:**
1. Ve a tu SQL Server en Azure
2. **Security → Networking**
3. Verifica que esté marcado: **"Allow Azure services and resources to access this server"**
4. Click en **"Save"**

### Error: "CORS blocked"

**Solución:**
1. Ve a tu App Service en Azure
2. **Environment variables**
3. Verifica que `Cors__AllowedOrigins` contenga la URL de tu frontend
4. **Restart** el App Service

### Error: "Migrations not applied"

**Solución:**
1. Ve a **GitHub Actions → CD Backend → Run Migrations**
2. Revisa los logs para ver el error específico
3. Verifica que el connection string sea correcto en los secrets

### Ver logs en tiempo real:

1. Ve a tu App Service en Azure
2. **Monitoring → Log stream**
3. Aquí verás todos los logs de tu aplicación en vivo

---

## 💰 Resumen de Costos

| Recurso | Tier | Costo/mes |
|---------|------|-----------|
| SQL Database (Pruebas) | Basic | ~$5 |
| SQL Database (Producción) | Basic | ~$5 |
| App Service (Pruebas) | F1 (Free) | $0 |
| App Service (Producción) | B1 (Basic) | ~$13 |
| **TOTAL** | | **~$23/mes** |

Con tu crédito de **$100 de Azure for Students**, esto te alcanza para **4+ meses**.

---

## 🎉 ¡Listo!

Tu backend ahora:
- ✅ Se compila y prueba automáticamente en cada push
- ✅ Se despliega automáticamente a Azure cuando haces push a `main`
- ✅ Ejecuta migraciones de base de datos automáticamente
- ✅ Está disponible en 2 ambientes (pruebas y producción)

**URLs finales:**
- **Backend Pruebas:** https://fluentis-pruebas.azurewebsites.net
- **Backend Producción:** https://fluentis-prod.azurewebsites.net

---

## 📚 Próximos Pasos

1. **Configurar CI/CD para el frontend** (si no lo tienes)
2. **Configurar dominios personalizados** (opcional)
3. **Configurar Azure Application Insights** (monitoreo)
4. **Configurar backups automáticos** de las bases de datos

---

¿Necesitas ayuda con algún paso? ¡Pregunta! 🤔
