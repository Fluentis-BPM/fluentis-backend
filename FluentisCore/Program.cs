using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Azure.Identity;
using Microsoft.Graph;
using FluentisCore.Modules.DBInit;

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
        policy.RequireClaim("scp", "access_as_user"));
});

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
});
builder.Services.AddOpenApi();
builder.Services.AddDbContext<FluentisContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthentication(); // Always enable authentication
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FluentisContext>();
    db.Database.Migrate();

    var initDB = new FluentisCore.Modules.DBInit.DBInit(db);

    var jsonPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Cargos.json");
    if (File.Exists(jsonPath))
    {
        var jsonData = File.ReadAllText(jsonPath);
        initDB.InsertCargosFromJson(jsonData);
        initDB.InsertRols();
        initDB.InsertDepartamentos();
    }
    else
    {
        Console.WriteLine("⚠️ No se encontró el archivo Resources/Cargos.json");
    }
}

app.Run();