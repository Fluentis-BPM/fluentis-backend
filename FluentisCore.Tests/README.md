# Tests de Fluentis Core

Este proyecto contiene dos tipos de tests:

## 🧪 Tests Unitarios (Siempre se ejecutan)

Estos tests **NO** requieren recursos externos y se ejecutan en CI/CD:

- **`UsuarioValidationTests.cs`** - Validaciones de modelos
- **`UsuarioTests.cs`** - Operaciones CRUD de usuarios
- **`DepartamentoTests.cs`** - Operaciones CRUD de departamentos

✅ **Se ejecutan en GitHub Actions CI** automáticamente

## 🔗 Tests de Integración (Solo local)

Estos tests **SÍ** requieren recursos externos:

- **`IntegrationTests.cs`** - Tests que usan:
  - Azure AD para autenticación
  - Base de datos real
  - Credenciales de usuario real

❌ **NO se ejecutan en GitHub Actions CI** (por diseño)

---

## 🚀 Cómo Ejecutar Tests

### Ejecutar SOLO tests unitarios (como en CI):

```bash
dotnet test --filter "FullyQualifiedName!~IntegrationTests"
```

### Ejecutar TODOS los tests (incluidos integración):

**Primero configura credenciales de Azure AD:**

```bash
dotnet user-secrets set "AzureAd:ClientId" "badd1a2d-8427-4f00-b56d-ddbbd9f1883e"
dotnet user-secrets set "AzureAd:ClientSecret" "HmV8Q~FcOqPZuhLdbLun3lz3rdXUPL-0cnYODaEC"
dotnet user-secrets set "AzureAd:TenantId" "846e3824-7539-4a0d-bfb6-00745fba3165"
```

**Luego ejecuta:**

```bash
dotnet test
```

### Ejecutar SOLO tests de integración:

```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

---

## 📊 Resultados Esperados

### En GitHub Actions (CI):
```
Total tests: 3
     Passed: 3 ✅
     Failed: 0
```

### En Local (con credenciales configuradas):
```
Total tests: 8
     Passed: 8 ✅
     Failed: 0
```

---

## ❓ ¿Por qué los tests de integración no se ejecutan en CI?

1. **Seguridad**: No queremos exponer credenciales reales en CI
2. **Velocidad**: Los tests de integración son más lentos
3. **Confiabilidad**: CI no debe depender de servicios externos (Azure AD, BD)
4. **Costo**: Menos tiempo de ejecución = menos uso del crédito de GitHub Actions

Los tests de integración se ejecutan **manualmente antes de hacer deploy** para validar que todo funcione correctamente con los servicios reales.

---

## 🔧 Agregar Nuevos Tests

### Test Unitario (recomendado):

```csharp
public class MiNuevoTest
{
    [Fact]
    public void MiTest_DeberiaFuncionar()
    {
        // Arrange
        var valor = 1;
        
        // Act
        var resultado = valor + 1;
        
        // Assert
        Assert.Equal(2, resultado);
    }
}
```

✅ Se ejecutará automáticamente en CI

### Test de Integración:

```csharp
public class IntegrationTests  // ← Debe tener "IntegrationTests" en el nombre
{
    [Fact]
    public async Task MiTestIntegracion_DeberiaFuncionar()
    {
        // Este test NO se ejecutará en CI
        // Solo en local con credenciales configuradas
    }
}
```

❌ No se ejecutará en CI (por el filtro)

---

## 📚 Recursos

- [xUnit Documentation](https://xunit.net/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [EF Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
