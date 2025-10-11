using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace FluentisCore.Tests;

/// <summary>
/// Contiene tests de integración para los casos de uso principales de la aplicación.
/// Hereda de PlaywrightTest para obtener acceso a la infraestructura de Playwright.
/// </summary>
public class IntegrationTests : PlaywrightTest
{
    // Constante para la URL base de la API. Reemplaza con la URL real de tu entorno de pruebas.
    private const string BaseApiUrl = "http://localhost:5155";
    private IAPIRequestContext? _apiContext;
    private static readonly IConfiguration _configuration;

    // Static constructor to load configuration from user secrets
    static IntegrationTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<IntegrationTests>()
            .Build();
    }

    /// <summary>
    /// Método de ayuda para obtener un token de acceso y crear un contexto de API autenticado.
    /// Este método se asegura de que cada test se ejecute con un token válido.
    /// </summary>
    private async Task<IAPIRequestContext> GetAuthenticatedApiContextAsync()
    {
        if (_apiContext != null)
        {
            return _apiContext;
        }

        var accessToken = Environment.GetEnvironmentVariable("FLUENTIS_TEST_TOKEN");
        if (string.IsNullOrEmpty(accessToken))
        {
            // If token is not in environment variables, fetch it using MSAL
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var apiScope = _configuration["AzureAd:ApiScope"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(apiScope))
            {
                Assert.Fail("Las credenciales de Azure AD no están configuradas en los secretos de usuario. Ejecute 'dotnet user-secrets set'.");
            }

            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            var scopes = new[] { apiScope };
            AuthenticationResult result;
            try
            {
                result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                accessToken = result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                Assert.Fail($"Error al obtener el token de Azure AD: {ex.Message}");
                throw; // Unreachable code
            }
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            Assert.Fail("No se pudo obtener el token de acceso.");
            // La línea anterior fallará el test, pero para que el compilador no se queje:
            throw new InvalidOperationException("No se puede proceder sin un token de prueba.");
        }

        _apiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseApiUrl,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            }
        });

        return _apiContext;
    }

    /// <summary>
    /// F-40: Test para consumir el endpoint de usuarios (CU-40).
    /// Escenario: Un cliente autenticado solicita la lista de usuarios.
    /// Resultado Esperado: Se recibe una respuesta exitosa con una lista de usuarios.
    /// </summary>
    [Fact]
    public async Task F40_ConsumirEndpointDeUsuarios()
    {
        // 1. Arrange: Obtener el contexto de API autenticado.
        var apiContext = await GetAuthenticatedApiContextAsync();

        // 2. Act: Realizar una solicitud GET al endpoint de usuarios.
        // (Ajusta la ruta '/api/usuarios' si es diferente en tu API)
        var response = await apiContext.GetAsync("/api/usuarios");

        // 3. Assert: Verificar que la respuesta es exitosa y contiene datos.
        Assert.True(response.Ok, $"La solicitud falló con el estado: {response.Status}");
        var jsonResponse = await response.JsonAsync();
        Assert.NotNull(jsonResponse);
        // Verifica que la respuesta es un array (una lista).
        Assert.Equal(JsonValueKind.Array, jsonResponse.Value.ValueKind);
    }

    /// <summary>
    /// F-03: Test para gestionar usuarios (CU-03).
    /// Escenario: Un cliente autenticado crea un nuevo usuario.
    /// Resultado Esperado: El usuario es creado y se devuelve en la respuesta.
    /// </summary>
    [Fact]
    public async Task F03_GestionarUsuarios()
    {
        // 1. Arrange: Obtener el contexto de API autenticado.
        var apiContext = await GetAuthenticatedApiContextAsync();
        var newUserEmail = $"testuser-{Guid.NewGuid()}@fluentis.com";

        // First, get existing departments, cargos, and roles to use valid IDs and full objects
        var departamentosResponse = await apiContext.GetAsync("/api/departamentos");
        var cargosResponse = await apiContext.GetAsync("/api/cargos");
        var rolesResponse = await apiContext.GetAsync("/api/rols");

        Assert.True(departamentosResponse.Ok, "Failed to get departments");
        Assert.True(cargosResponse.Ok, "Failed to get cargos");
        Assert.True(rolesResponse.Ok, "Failed to get roles");

        var departamentos = await departamentosResponse.JsonAsync();
        var cargos = await cargosResponse.JsonAsync();
        var roles = await rolesResponse.JsonAsync();

        var firstDept = departamentos.Value.EnumerateArray().First();
        var firstCargo = cargos.Value.EnumerateArray().First();
        var firstRole = roles.Value.EnumerateArray().First();

        // 2. Act: Enviar una solicitud POST para crear un nuevo usuario.
        // Send only the IDs, not the full navigation objects
        var response = await apiContext.PostAsync("/api/usuarios", new()
        {
            DataObject = new
            {
                // Asegúrate de que estos campos coincidan con tu DTO de creación de usuario.
                Nombre = "Usuario de Prueba API",
                Email = newUserEmail,
                Oid = Guid.NewGuid().ToString(), // OID de prueba
                DepartamentoId = firstDept.GetProperty("idDepartamento").GetInt32(),
                CargoId = firstCargo.GetProperty("idCargo").GetInt32(),
                RolId = firstRole.GetProperty("idRol").GetInt32()
                // Don't include navigation properties - only IDs
            }
        });

        // 3. Assert: Verificar que el usuario fue creado exitosamente.
        if (!response.Ok)
        {
            var responseBody = await response.TextAsync();
            Assert.True(response.Ok, $"La creación del usuario falló con el estado: {response.Status}. Cuerpo: {responseBody}");
        }
        Assert.Equal(201, response.Status); // 201 Created

        var jsonResponse = await response.JsonAsync();
        Assert.Equal(newUserEmail, jsonResponse?.GetProperty("email").GetString());
    }

    /// <summary>
    /// F-22: Test para configurar roles y permisos (CU-22).
    /// Escenario: Un cliente autenticado solicita la lista de roles disponibles.
    /// Resultado Esperado: Se recibe una respuesta exitosa con la lista de roles.
    /// </summary>
    [Fact]
    public async Task F22_RolesYPermisos_Lista()
    {
        // 1. Arrange: Obtener el contexto de API autenticado.
        var apiContext = await GetAuthenticatedApiContextAsync();

        // 2. Act: Realizar una solicitud GET al endpoint de roles.
        // (Ajusta la ruta '/api/Rols' si es diferente en tu API)
        var response = await apiContext.GetAsync("/api/Rols");

        // 3. Assert: Verificar que la respuesta es exitosa y no está vacía.
        Assert.True(response.Ok, $"La solicitud de roles falló con el estado: {response.Status}");
        var jsonResponse = await response.JsonAsync();
        Assert.NotNull(jsonResponse);
        Assert.Equal(JsonValueKind.Array, jsonResponse.Value.ValueKind);
        // Opcional: verificar que la lista contiene al menos un rol.
        Assert.True(jsonResponse.Value.GetArrayLength() > 0, "La lista de roles no debería estar vacía.");
    }

    /// <summary>
    /// F-05: Test para el caso de uso de crear una nueva solicitud (CU-05) vía API.
    /// Escenario: Un cliente autenticado envía los datos de una nueva solicitud a la API.
    /// Resultado Esperado: El flujo de la solicitud se guarda y se recibe una respuesta exitosa.
    /// </summary>
    [Fact]
    public async Task F05_CreateNuevaSolicitud_APIOnly()
    {
        // 1. Arrange: Obtener el contexto de API autenticado.
        // El método de ayuda ahora se encarga de obtener el token automáticamente.
        var apiContext = await GetAuthenticatedApiContextAsync();

        // First, create a user to use as the solicitor
        var userCreateData = new
        {
            Nombre = $"Usuario Solicitud Test {DateTime.Now:yyyyMMddHHmmss}",
            Email = $"testsolicitud{DateTime.Now:yyyyMMddHHmmss}@example.com",
            DepartamentoId = 1, // Assume department with ID 1 exists
            RolId = 1, // Assume role with ID 1 exists
            CargoId = 1, // Assume cargo with ID 1 exists
            Oid = $"oid_solicitud_{DateTime.Now:yyyyMMddHHmmss}"
        };

        var userResponse = await apiContext.PostAsync("/api/usuarios", new()
        {
            DataObject = userCreateData
        });

        // Ensure user creation was successful
        Assert.True(userResponse.Ok, $"User creation failed with status: {userResponse.Status}");
        
        var userResponseBody = await userResponse.JsonAsync();
        var userId = userResponseBody?.GetProperty("idUsuario").GetInt32() ?? 0;
        Assert.True(userId > 0, "Created user should have a valid ID");

        // 2. Act: Enviar una solicitud POST para crear la nueva solicitud.
        var response = await apiContext.PostAsync("/api/solicitudes", new()
        {
            DataObject = new
            {
                SolicitanteId = userId, // Use the created user
                FlujoBaseId = (int?)null,   // Null is allowed for testing API flow
                Inputs = new object[] { }, // Lista vacía de inputs
                GrupoAprobacionId = (int?)null      // Null is allowed for testing
            }
        }
        );

        // 3. Assert: Verificar que la solicitud fue creada exitosamente.
        if (!response.Ok)
        {
            var responseBody = await response.TextAsync();
            Assert.True(response.Ok, $"La creación de la solicitud falló con el estado: {response.Status}. Cuerpo: {responseBody}");
        }
        
        Assert.Equal(201, response.Status); // 201 Created es una respuesta común para creaciones exitosas.

        var jsonResponse = await response.JsonAsync();
        var createdId = jsonResponse?.GetProperty("idSolicitud").GetInt32();

        Assert.True(createdId > 0, "Should return a valid solicitud ID");
    }

    /// <summary>
    /// F-XX: Crear plantilla e instanciar solicitud desde plantilla.
    /// </summary>
    [Fact]
    public async Task CreateTemplateAndInstantiateSolicitud()
    {
        var apiContext = await GetAuthenticatedApiContextAsync();

        // Create a simple template referencing an existing Input (assume Id 1 exists from DBInit)
        var plantillaResponse = await apiContext.PostAsync("/api/plantillas", new()
        {
            DataObject = new
            {
                Nombre = "Plantilla API Test",
                Descripcion = "Creada por test",
                FlujoBaseId = (int?)null,
                GrupoAprobacionId = (int?)null,
                Inputs = new[]
                {
                    new { InputId = 1, Nombre = "Campo 1", PlaceHolder = "Ingrese...", Requerido = true, ValorPorDefecto = "valor" }
                }
            }
        });

        if (!plantillaResponse.Ok)
        {
            var responseBody = await plantillaResponse.TextAsync();
            Assert.True(plantillaResponse.Ok, $"Creación de plantilla falló: {plantillaResponse.Status}. Cuerpo: {responseBody}");
        }

        var plantillaJson = await plantillaResponse.JsonAsync();
        var plantillaId = plantillaJson?.GetProperty("idPlantilla").GetInt32() ?? 0;
        Assert.True(plantillaId > 0);

        // Create a user to own the solicitud
        var userResponse = await apiContext.PostAsync("/api/usuarios", new()
        {
            DataObject = new
            {
                Nombre = $"Usuario Plantilla {DateTime.Now:yyyyMMddHHmmss}",
                Email = $"tpl{DateTime.Now:yyyyMMddHHmmss}@example.com",
                DepartamentoId = 1,
                RolId = 1,
                CargoId = 1,
                Oid = $"oid_tpl_{DateTime.Now:yyyyMMddHHmmss}"
            }
        });
        Assert.True(userResponse.Ok, $"User creation failed with status: {userResponse.Status}");
        var userJson = await userResponse.JsonAsync();
        var uid = userJson?.GetProperty("idUsuario").GetInt32() ?? 0;
        Assert.True(uid > 0);

        // Instantiate solicitud from template
        var instResponse = await apiContext.PostAsync("/api/plantillas/instanciar-solicitud", new()
        {
            DataObject = new
            {
                PlantillaId = plantillaId,
                SolicitanteId = uid,
                Nombre = "Solicitud desde plantilla",
                Descripcion = "Creada por test",
                OverridesValores = new Dictionary<int, string> { { 1, "override" } }
            }
        });

        if (!instResponse.Ok)
        {
            var responseBody = await instResponse.TextAsync();
            Assert.True(instResponse.Ok, $"Instanciación de solicitud falló: {instResponse.Status}. Cuerpo: {responseBody}");
        }

        var solJson = await instResponse.JsonAsync();
        var sid = solJson?.GetProperty("idSolicitud").GetInt32() ?? 0;
        Assert.True(sid > 0);
    }
}