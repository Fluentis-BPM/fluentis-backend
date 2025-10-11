using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluentisCore.Auth
{
    public class ConditionalAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ConditionalAuthorizeFilter(IHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Console.WriteLine($"[ConditionalAuthorizeFilter] Environment: {_environment.EnvironmentName}");
            Console.WriteLine($"[ConditionalAuthorizeFilter] Initial IsAuthenticated: {context.HttpContext.User.Identity?.IsAuthenticated ?? false}");
            Console.WriteLine($"[ConditionalAuthorizeFilter] Initial Claims: {string.Join(", ", context.HttpContext.User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");

            if (_environment.IsDevelopment())
            {
                Console.WriteLine("[ConditionalAuthorizeFilter] Skipping authorization in Development mode.");
                return;
            }

            // Get the expected audience from configuration
            var expectedAudience = _configuration["AzureAd:Audience"];
            Console.WriteLine($"[ConditionalAuthorizeFilter] Expected Audience: {expectedAudience}");

            var authResult = await context.HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (authResult.Succeeded)
            {
                Console.WriteLine("[ConditionalAuthorizeFilter] Authentication succeeded. Setting User principal.");
                context.HttpContext.User = authResult.Principal;
            }
            else
            {
                Console.WriteLine($"[ConditionalAuthorizeFilter] Authentication failed: {authResult.Failure?.Message ?? "No failure message provided"}");
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                Console.WriteLine("[ConditionalAuthorizeFilter] User is not authenticated. Returning Unauthorized.");
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check for scope claim using possible claim types
            var hasScope = context.HttpContext.User.HasClaim(c =>
                (c.Type == "scp" || c.Type == "http://schemas.microsoft.com/identity/claims/scope" || c.Type == "scope") &&
                c.Value.Contains("access_as_user"));

            if (!hasScope)
            {
                Console.WriteLine("[ConditionalAuthorizeFilter] User lacks required scope: access_as_user. Claim types checked: scp, http://schemas.microsoft.com/identity/claims/scope, scope");
                Console.WriteLine($"[ConditionalAuthorizeFilter] Available scope claims: {string.Join(", ", context.HttpContext.User.Claims.Where(c => c.Type == "scp" || c.Type == "http://schemas.microsoft.com/identity/claims/scope" || c.Type == "scope").Select(c => $"{c.Type}: {c.Value}"))}");
                context.Result = new UnauthorizedResult();
            }
        }
    }
}