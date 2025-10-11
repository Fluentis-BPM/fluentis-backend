# 📚 Explicación Detallada del CI/CD

## 🔍 ¿Qué hace cada GitHub Action?

---

## 📋 **CI.YML - Continuous Integration (Integración Continua)**

Este workflow se ejecuta **cada vez** que haces push o abres un PR en las ramas `main` o `develop`.

### Paso a paso:

```yaml
- name: Checkout code
  uses: actions/checkout@v4
```
**¿Qué hace?** Descarga tu código del repositorio a la máquina virtual de GitHub.
**¿Por qué?** Para poder compilar y probar tu código.

---

```yaml
- name: Setup .NET 9.0
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'
```
**¿Qué hace?** Instala .NET 9.0 en la máquina virtual.
**¿Por qué?** Tu proyecto usa .NET 9.0 y necesita el SDK para compilar.

---

```yaml
- name: Restore dependencies
  run: dotnet restore FluentisCore.sln
```
**¿Qué hace?** Descarga todos los paquetes NuGet (Entity Framework, Azure AD, etc.).
**¿Por qué?** Tu proyecto depende de librerías externas que necesitan descargarse antes de compilar.

---

```yaml
- name: Build
  run: dotnet build FluentisCore.sln --no-restore --configuration Release
```
**¿Qué hace?** Compila todo tu proyecto en modo Release (optimizado).
**¿Por qué?** Para verificar que no hay errores de compilación antes de hacer deploy.

---

```yaml
    - name: Run tests
      run: dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~IntegrationTests"
```
**¿Qué hace?** Ejecuta solo los tests unitarios (excluye tests de integración).
**¿Por qué el filtro?** Los tests de integración requieren Azure AD y base de datos real, que no están disponibles en CI.
**¿Qué es `FullyQualifiedName!~IntegrationTests`?** Excluye todas las clases que contengan "IntegrationTests" en su nombre.
**¿Por qué?** Para asegurar que tu código funciona correctamente antes de desplegarlo, sin depender de recursos externos.---

```yaml
- name: Install EF Core Tools
  run: dotnet tool install --global dotnet-ef
```
**¿Qué hace?** Instala la herramienta de línea de comandos de Entity Framework.
**¿Por qué?** Para poder verificar las migraciones de base de datos.

---

```yaml
- name: Check Migrations
  run: dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj
```
**¿Qué hace?** Lista todas las migraciones de base de datos.
**¿Por qué?** Para verificar que las migraciones están correctas y no hay problemas.

---

## 🚀 **CD.YML - Continuous Deployment (Despliegue Continuo)**

Este workflow se ejecuta **solo cuando** haces push a la rama `main` (o manualmente).

### Job 1: Deploy a PRUEBAS

```yaml
- name: Checkout code
  uses: actions/checkout@v4
```
**¿Qué hace?** Igual que en CI, descarga el código.

---

```yaml
- name: Setup .NET 9.0
  uses: actions/setup-dotnet@v4
```
**¿Qué hace?** Instala .NET 9.0.

---

```yaml
- name: Restore dependencies
  run: dotnet restore FluentisCore/FluentisCore.csproj
```
**¿Qué hace?** Descarga paquetes NuGet.

---

```yaml
- name: Build
  run: dotnet build FluentisCore/FluentisCore.csproj --configuration Release --no-restore
```
**¿Qué hace?** Compila el proyecto en modo Release.

---

```yaml
- name: Publish
  run: dotnet publish FluentisCore/FluentisCore.csproj --configuration Release --output ./publish --no-build
```
**¿Qué hace?** Crea un paquete optimizado listo para desplegar.
**¿Por qué?** `publish` prepara todos los archivos necesarios (DLLs, appsettings, etc.) en una carpeta `./publish`.

---

```yaml
- name: Install EF Core Tools
  run: dotnet tool install --global dotnet-ef
```
**¿Qué hace?** Instala herramientas de Entity Framework.

---

```yaml
- name: Generate Migration Script
  run: |
    dotnet ef migrations script \
      --project FluentisCore/FluentisCore.csproj \
      --startup-project FluentisCore/FluentisCore.csproj \
      --idempotent \
      --output ./publish/migrations.sql
```
**¿Qué hace?** Genera un script SQL con TODAS las migraciones.
**¿Qué es `--idempotent`?** Significa que el script puede ejecutarse múltiples veces sin errores (verifica qué migraciones ya están aplicadas).
**¿Por qué?** Para actualizar automáticamente el esquema de la base de datos en Azure.

---

```yaml
- name: Deploy to Azure Web App (Pruebas)
  uses: azure/webapps-deploy@v3
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME_PRUEBAS }}
    publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE_PRUEBAS }}
    package: ./publish
```
**¿Qué hace?** Sube el paquete `./publish` a tu Azure App Service de pruebas.
**¿Cómo funciona?** Usa el "publish profile" (credenciales de Azure) para autenticarse y desplegar.

---

```yaml
- name: Run Migrations on Azure SQL (Pruebas)
  uses: azure/sql-action@v2.2
  with:
    connection-string: ${{ secrets.AZURE_SQL_CONNECTION_STRING_PRUEBAS }}
    path: './publish/migrations.sql'
```
**¿Qué hace?** Se conecta a tu base de datos Azure SQL y ejecuta el script `migrations.sql`.
**¿Por qué?** Para actualizar automáticamente las tablas, columnas, relaciones, etc.

---

```yaml
- name: Deployment Summary
  run: |
    echo "✅ Backend desplegado en ambiente de PRUEBAS"
    echo "🔗 URL: https://${{ secrets.AZURE_WEBAPP_NAME_PRUEBAS }}.azurewebsites.net"
    echo "📊 Migraciones aplicadas exitosamente"
```
**¿Qué hace?** Imprime un mensaje de éxito en los logs.
**¿Por qué?** Para que veas en GitHub Actions que todo salió bien.

---

### Job 2: Deploy a PRODUCCIÓN

**Exactamente igual que pruebas**, pero:
- Usa diferentes secrets (PROD en lugar de PRUEBAS)
- Solo se ejecuta si el deploy a PRUEBAS fue exitoso (`needs: deploy-pruebas`)

---

## ❓ ¿Por qué había error con `environment`?

```yaml
environment: pruebas  # ❌ Error si no existe este environment en GitHub
```

**El problema:** Los "environments" son una configuración especial de GitHub que debes crear manualmente.

**La solución:** Los comenté para que funcione de inmediato. Si quieres usarlos (recomendado para producción):

1. Ve a tu repo en GitHub
2. **Settings → Environments**
3. Click en **"New environment"**
4. Crea uno llamado `pruebas` y otro `produccion`
5. En `produccion`, activa **"Required reviewers"** y agrega tu usuario
6. Descomenta las líneas en `cd.yml`

**Beneficio:** Con esto, el deploy a producción pedirá tu aprobación manual antes de ejecutarse.

---

## 🔄 Flujo Completo de CI/CD

```
📝 Haces cambios en el código
    ↓
💾 git commit -m "feat: nueva funcionalidad"
    ↓
🚀 git push origin develop
    ↓
✅ CI se ejecuta automáticamente:
    - Compila
    - Ejecuta tests
    - Verifica migraciones
    ↓
✅ Tests pasan → Todo bien ✅
    ↓
🔀 git checkout main && git merge develop
    ↓
🚀 git push origin main
    ↓
🚀 CD se ejecuta automáticamente:
    ↓
📦 Job 1: Deploy a PRUEBAS
    - Compila
    - Publica
    - Genera SQL de migraciones
    - Despliega a Azure App Service (pruebas)
    - Ejecuta migraciones en SQL (pruebas)
    ↓
✅ Pruebas exitosas
    ↓
📦 Job 2: Deploy a PRODUCCIÓN
    - Compila
    - Publica
    - Genera SQL de migraciones
    - Despliega a Azure App Service (producción)
    - Ejecuta migraciones en SQL (producción)
    ↓
✅ ¡En producción! 🎉
```

---

## 🎯 Resumen de Actions Usadas

| Action | Propósito | Link |
|--------|-----------|------|
| `actions/checkout@v4` | Descargar código del repo | [GitHub](https://github.com/actions/checkout) |
| `actions/setup-dotnet@v4` | Instalar .NET SDK | [GitHub](https://github.com/actions/setup-dotnet) |
| `azure/webapps-deploy@v3` | Desplegar a Azure App Service | [GitHub](https://github.com/Azure/webapps-deploy) |
| `azure/sql-action@v2.2` | Ejecutar SQL en Azure SQL Database | [GitHub](https://github.com/Azure/sql-action) |

---

## 💡 Comandos Equivalentes (si los corrieras en tu PC)

### CI:
```bash
dotnet restore
dotnet build --configuration Release
dotnet test
dotnet ef migrations list
```

### CD:
```bash
dotnet publish --configuration Release --output ./publish
dotnet ef migrations script --output migrations.sql

# Subir a Azure (lo hace la action)
# Ejecutar SQL (lo hace la action)
```

---

## ✅ Lo que SÍ necesitas configurar manualmente

1. **Recursos de Azure** (SQL Database + App Service)
2. **GitHub Secrets** (conexiones, credenciales)
3. **Variables de entorno en Azure** (CORS, connection strings)
4. **Cambio en `Program.cs`** (CORS dinámico)

Todo esto está explicado en `DEPLOYMENT_AZURE_PORTAL.md` (siguiente archivo).

---

¿Alguna action específica que quieras entender más a fondo? 🤔
