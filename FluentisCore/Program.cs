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
        Console.WriteLine(builder.Configuration["Cors:AllowedOrigins"]);
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"])
              .AllowAnyHeader()
              .AllowAnyMethod();
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

    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    return new GraphServiceClient(credential, new[] { "[invalid url, do not cite]" });
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
// app.UseHttpsRedirection();
app.UseAuthentication();
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