using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.TokenValidationParameters.RoleClaimType = "roles";
    }, options => { });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://login.microsoftonline.com/846e3824-7539-4a0d-bfb6-00745fba3165/oauth2/v2.0/authorize"),
                TokenUrl = new Uri("https://login.microsoftonline.com/846e3824-7539-4a0d-bfb6-00745fba3165/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "https://asofarmabpm.onmicrosoft.com/badd1a2d-8427-4f00-b56d-ddbbd9f1883e/access_as_user", "Access API as user" },
                },
            },
        },
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
            },
            new[] { "https://asofarmabpm.onmicrosoft.com/badd1a2d-8427-4f00-b56d-ddbbd9f1883e/access_as_user" }
        },
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.OAuthClientId("cbd0adaf-be24-4605-942b-d64ace0763a7"); //cliend id de frontend
    c.OAuthScopes("https://asofarmabpm.onmicrosoft.com/badd1a2d-8427-4f00-b56d-ddbbd9f1883e/access_as_user");
    c.OAuthUsePkce();
});

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FluentisContext>();
    db.Database.Migrate();
}

app.Run();