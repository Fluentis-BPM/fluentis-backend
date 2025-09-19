using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Azure.Identity;
using Microsoft.Graph;
using FluentisCore.Modules.DBInit;
using FluentisCore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine("CORS configurado para desarrollo: Permitiendo cualquier origen, encabezado y método.");
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            Console.WriteLine("CORS configurado para producción: Permitiendo solo el origen específico de la aplicación frontend.");
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Configurar autenticación con Azure AD
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        jwtOptions =>
        {
            builder.Configuration.Bind("AzureAd", jwtOptions);
            jwtOptions.TokenValidationParameters.RoleClaimType = "roles";
            jwtOptions.TokenValidationParameters.ValidateAudience = true;
            jwtOptions.TokenValidationParameters.ValidateIssuer = true;
            // Accept multiple audiences
            jwtOptions.TokenValidationParameters.ValidAudiences = new[]
            {
                builder.Configuration["AzureAd:Audience"], // https://asofarmabpm.onmicrosoft.com/...
                $"api://{builder.Configuration["AzureAd:ClientId"]}" // api://badd1a2d-...
            };
            // Accept multiple issuers
            jwtOptions.TokenValidationParameters.ValidIssuers = new[]
            {
                $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0",
                $"https://sts.windows.net/{builder.Configuration["AzureAd:TenantId"]}/"
            };
            // Map scope claim to "scp"
            jwtOptions.TokenValidationParameters.NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
            jwtOptions.TokenValidationParameters.RoleClaimType = "roles";
            jwtOptions.TokenValidationParameters.ValidateLifetime = true;

            Console.WriteLine($"[JwtBearer] Configured ValidAudiences: {string.Join(", ", jwtOptions.TokenValidationParameters.ValidAudiences)}");
            Console.WriteLine($"[JwtBearer] Configured ValidIssuers: {string.Join(", ", jwtOptions.TokenValidationParameters.ValidIssuers)}");
        },
        identityOptions =>
        {
            builder.Configuration.Bind("AzureAd", identityOptions);
            if (string.IsNullOrEmpty(identityOptions.ClientId))
            {
                throw new InvalidOperationException("ClientId no se estableció en MicrosoftIdentityOptions después del binding");
            }
        });

// Configurar autorización con políticas basadas en scopes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAccessAsUser", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<WorkflowInitializationService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddHttpContextAccessor();


// Configura Microsoft Graph
builder.Services.AddScoped<GraphServiceClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var clientId = configuration["AzureAd:ClientId"];
    var clientSecret = configuration["AzureAd:ClientSecret"];
    var tenantId = configuration["AzureAd:TenantId"];

    Console.WriteLine($"GraphServiceClient - TenantId: {tenantId}");
    Console.WriteLine($"GraphServiceClient - ClientId: {clientId}");
    Console.WriteLine($"GraphServiceClient - ClientSecret: {(string.IsNullOrEmpty(clientSecret) ? "VACÍO" : "CONFIGURADO")}");

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
    {
        throw new InvalidOperationException("Faltan credenciales de Azure AD para GraphServiceClient");
    }

    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Uniformizar convenciones de nombres a camelCase para el frontend
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddOpenApi();
builder.Services.AddDbContext<FluentisContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fluentis API",
        Version = "v1",
        Description = "API de Fluentis con soporte para plantillas de solicitudes"
    });

    // JWT Bearer auth in Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Ingrese el token JWT con el esquema Bearer. Ejemplo: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });

    // XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fluentis API v1");
    // Restaurar ruta clásica de Swagger UI
    c.RoutePrefix = "swagger";
});
app.UseCors("AllowFrontend");
app.UseAuthentication(); // Always enable authentication
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FluentisContext>();
    db.Database.Migrate();

    var initDB = new FluentisCore.Modules.DBInit.DBInit(db);

    // Siempre asegurar catálogo de Inputs, independientemente de datos de ejemplo.
    initDB.InsertMockInputs(); // Garantiza que todos los TipoInput existan

    var jsonPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Cargos.json");
    if (File.Exists(jsonPath))
    {
        var jsonData = File.ReadAllText(jsonPath);
        initDB.InsertCargosFromJson(jsonData);
        initDB.InsertRols();
        initDB.InsertDepartamentos();
        initDB.InsertMockUsers(); // Insert mock users with relationships
        initDB.InsertMockApprovalGroups(); // Insert approval groups
        initDB.InsertMockUserGroupRelations(); // Link users to approval groups
        initDB.InsertMockWorkflows(); // Insert sample workflows
        Console.WriteLine("✅ Base de datos inicializada con datos de prueba completos");
        Console.WriteLine("👥 Usuarios creados con relaciones a departamentos, roles y cargos");
        Console.WriteLine("🔄 Flujos de aprobación y grupos configurados");
        Console.WriteLine("📋 Tipos de inputs configurados");
    }
    else
    {
        Console.WriteLine("⚠️ No se encontró el archivo Resources/Cargos.json");
    }
}

app.Run();