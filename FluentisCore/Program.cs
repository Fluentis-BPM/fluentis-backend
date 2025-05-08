using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using FluentisCore.Modules.DBInit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configurar autenticación con Azure AD
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateIssuer = true;
    }, options => { });

// Configurar autorización con políticas basadas en scopes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAccessAsUser", policy =>
        policy.RequireClaim("scp", "access_as_user"));
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddDbContext<FluentisContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
;
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
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