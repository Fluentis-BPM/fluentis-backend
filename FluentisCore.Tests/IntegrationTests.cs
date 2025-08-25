using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace FluentisCore.Tests;

[Trait("Category", "E2E")]
/// <summary>
/// Contiene tests de integración end-to-end usando HTTP reales contra la API.
/// Por defecto intentan conectar a API_BASE_URL (default http://localhost:8080).
/// Si E2E_AUTOSTART=1, intentan levantar docker compose automáticamente.
/// </summary>
public class IntegrationTests : PlaywrightTest
{
    // Base URL configurable: usa API_BASE_URL o fallback al puerto de docker compose (8080)
    private static readonly string BaseApiUrl =
        Environment.GetEnvironmentVariable("API_BASE_URL")?.TrimEnd('/')
        ?? "http://localhost:8080";
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
        await EnsureApiReachableAsync();
        if (_apiContext != null)
        {
            return _apiContext;
        }
        string? accessToken = Environment.GetEnvironmentVariable("FLUENTIS_TEST_TOKEN");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            // Fetch via MSAL using user-secrets config
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var apiScope = _configuration["AzureAd:ApiScope"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(apiScope))
            {
                Assert.Fail("Credenciales de Azure AD no configuradas. Use 'dotnet user-secrets set'.");
            }

            try
            {
                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();
                var result = await app.AcquireTokenForClient(new[] { apiScope }).ExecuteAsync();
                accessToken = result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                Assert.Fail($"Error al obtener token AAD: {ex.Message}");
            }
        }

        _apiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseApiUrl,
            ExtraHTTPHeaders = string.IsNullOrWhiteSpace(accessToken)
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["Authorization"] = $"Bearer {accessToken}" }
        });

        return _apiContext;
    }

    private static async Task EnsureApiReachableAsync()
    {
        // Quick probe first
        if (await IsAliveAsync()) return;

        var autostart = Environment.GetEnvironmentVariable("E2E_AUTOSTART");
        if (string.IsNullOrEmpty(autostart) || autostart == "1" || autostart.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            TryStartDockerCompose();
        }

        // Wait up to ~60s for readiness
        var started = await WaitUntilAsync(IsAliveAsync, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(2));
        if (!started)
        {
            Assert.Fail($"La API no está disponible en {BaseApiUrl}. Configure API_BASE_URL o inicie docker compose.");
        }
    }

    private static async Task<bool> IsAliveAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var url = BaseApiUrl + "/swagger/v1/swagger.json";
            var resp = await client.GetAsync(url);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan interval)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition()) return true;
            await Task.Delay(interval);
        }
        return false;
    }

    private static void TryStartDockerCompose()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose up -d",
                WorkingDirectory = GetRepoRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p!.WaitForExit(60_000);
        }
        catch { }
    }

    private static string GetRepoRoot()
    {
        var here = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(here);
        for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FluentisCore.sln")))
                return dir.FullName;
        }
        return here;
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

        // Prefer existing seeded data; if not present, try minimal creation.
        // Departamento
        var deptList = await apiContext.GetAsync("/api/departamentos");
        Assert.True(deptList.Ok, $"GET departamentos failed: {await deptList.TextAsync()}");
        int deptId;
        var deptJson = await deptList.JsonAsync();
        if (deptJson.Value.GetArrayLength() > 0)
        {
            deptId = deptJson.Value[0].GetProperty("idDepartamento").GetInt32();
        }
        else
        {
            var deptCreate = await apiContext.PostAsync("/api/departamentos", new()
            {
                DataObject = new Dictionary<string, object>
                {
                    ["nombre"] = "IT",
                    ["usuarios"] = Array.Empty<object>()
                }
            });
            Assert.True(deptCreate.Ok, $"Failed to create departamento: {await deptCreate.TextAsync()}");
            deptId = (await deptCreate.JsonAsync())!.Value.GetProperty("idDepartamento").GetInt32();
        }

        // Rol
        var rolList = await apiContext.GetAsync("/api/Rols");
        Assert.True(rolList.Ok, $"GET rols failed: {await rolList.TextAsync()}");
        int rolId;
        var rolJson = await rolList.JsonAsync();
        if (rolJson.Value.GetArrayLength() > 0)
        {
            rolId = rolJson.Value[0].GetProperty("idRol").GetInt32();
        }
        else
        {
            var rolCreate = await apiContext.PostAsync("/api/rols", new()
            {
                DataObject = new Dictionary<string, object>
                {
                    ["nombre"] = "User",
                    ["usuarios"] = Array.Empty<object>()
                }
            });
            Assert.True(rolCreate.Ok, $"Failed to create rol: {await rolCreate.TextAsync()}");
            rolId = (await rolCreate.JsonAsync())!.Value.GetProperty("idRol").GetInt32();
        }

        // 2. Act: Enviar una solicitud POST para crear un nuevo usuario.
        // Send only the IDs, not the full navigation objects
        var response = await apiContext.PostAsync("/api/usuarios", new()
        {
            DataObject = new Dictionary<string, object>
            {
                ["nombre"] = "Usuario de Prueba API",
                ["email"] = newUserEmail,
                ["oid"] = Guid.NewGuid().ToString(),
                ["departamentoId"] = deptId,
                ["rolId"] = rolId
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
        // Asegurar al menos un rol: si está vacío, crear uno y volver a listar
        if (jsonResponse.Value.GetArrayLength() == 0)
        {
            var created = await apiContext.PostAsync("/api/rols", new() { DataObject = new { Nombre = "User" } });
            Assert.True(created.Ok, $"No se pudo crear rol por defecto: {await created.TextAsync()}");
            response = await apiContext.GetAsync("/api/Rols");
            Assert.True(response.Ok);
            jsonResponse = await response.JsonAsync();
            Assert.True(jsonResponse.Value.GetArrayLength() > 0);
        }
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

        // Prefer existing seeded data for prerequisites
    int deptId2, rolId2;
        var deptList2 = await apiContext.GetAsync("/api/departamentos");
        Assert.True(deptList2.Ok, await deptList2.TextAsync());
        var deptJson2 = await deptList2.JsonAsync();
        if (deptJson2.Value.GetArrayLength() == 0)
        {
            var deptCreate2 = await apiContext.PostAsync("/api/departamentos", new() { DataObject = new Dictionary<string, object> { ["nombre"] = "Ops", ["usuarios"] = Array.Empty<object>() } });
            Assert.True(deptCreate2.Ok, await deptCreate2.TextAsync());
            deptId2 = (await deptCreate2.JsonAsync())!.Value.GetProperty("idDepartamento").GetInt32();
        }
        else
        {
            deptId2 = deptJson2.Value[0].GetProperty("idDepartamento").GetInt32();
        }

        var rolList2 = await apiContext.GetAsync("/api/Rols");
        Assert.True(rolList2.Ok, await rolList2.TextAsync());
        var rolJson2 = await rolList2.JsonAsync();
        if (rolJson2.Value.GetArrayLength() == 0)
        {
            var rolCreate2 = await apiContext.PostAsync("/api/rols", new() { DataObject = new Dictionary<string, object> { ["nombre"] = "User", ["usuarios"] = Array.Empty<object>() } });
            Assert.True(rolCreate2.Ok, await rolCreate2.TextAsync());
            rolId2 = (await rolCreate2.JsonAsync())!.Value.GetProperty("idRol").GetInt32();
        }
        else
        {
            rolId2 = rolJson2.Value[0].GetProperty("idRol").GetInt32();
        }

    // Cargo opcional: omitimos cargoId para evitar validación de JefeCargo.

        var userCreateData = new Dictionary<string, object>
        {
            ["nombre"] = $"Usuario Solicitud Test {DateTime.Now:yyyyMMddHHmmss}",
            ["email"] = $"testsolicitud{DateTime.Now:yyyyMMddHHmmss}@example.com",
            ["departamentoId"] = deptId2,
            ["rolId"] = rolId2,
            ["oid"] = $"oid_solicitud_{DateTime.Now:yyyyMMddHHmmss}"
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
                SolicitanteId = userId,
                Nombre = $"Solicitud API Test {DateTime.Now:HHmmss}",
                Descripcion = "Creada por E2E",
                FlujoBaseId = (int?)null,
                Inputs = new object[] { },
                GrupoAprobacionId = (int?)null
            }
        });

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
}