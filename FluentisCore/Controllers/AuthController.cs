using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using FluentisCore.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models.LoginRequestManagement;
using FluentisCore.Models.UserManagement;
using Microsoft.Graph.Models;

namespace FluentisCore.Controllers{
[ApiController]
[Route("/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly FluentisContext _context;
    private readonly GraphServiceClient _graphClient;
    
    public AuthController(FluentisContext context, GraphServiceClient graphClient)
    {
        _context = context;
        _graphClient = graphClient;
    }

    [HttpPost("login")]
    [Authorize]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var oid = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            Console.WriteLine("User Claims:");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }
                if (string.IsNullOrEmpty(oid))
            {
                return BadRequest(new { Error = "No se pudo obtener el OID del usuario desde el token." });
            }

            // Busca el usuario en la base de datos
            var user = await _context.Usuarios
                .Include(u => u.Departamento)
                .Include(u => u.Cargo)
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Oid == oid);

            if (user == null)
            {
                // Consulta Microsoft Graph para obtener información del usuario
                var graphUser = await _graphClient.Users[oid]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "displayName", "mail", "jobTitle", "department" };
                    });
                if (graphUser == null)
                {
                    return BadRequest(new { Error = "No se pudo obtener la información del usuario desde Microsoft Graph." });
                }

                    // Obtiene los grupos del usuario
                var groups = await _graphClient.Users[oid]
                .MemberOf
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "id", "displayName" };
                });

                var groupIds = groups?.Value?
                    .OfType<Microsoft.Graph.Models.DirectoryObject>()
                    .Where(g => g is Microsoft.Graph.Models.Group)
                    .Select(g => g.Id)
                    .ToList();

                // Busca el departamento existente
                var departamento = await _context.Departamentos.FirstOrDefaultAsync(d => d.Nombre == graphUser.Department);
                if (departamento == null && !string.IsNullOrEmpty(graphUser.Department))
                {
                    return BadRequest(new { Error = "El departamento no existe en la base de datos." });
                }

                // Busca el cargo existente
                var cargo = await _context.Cargos.FirstOrDefaultAsync(c => c.Nombre == graphUser.JobTitle);
                if (cargo == null && !string.IsNullOrEmpty(graphUser.JobTitle))
                {
                    return BadRequest(new { Error = "El cargo no existe en la base de datos." });
                }

                // Busca el rol "Miembro"
                var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Miembro");
                if (rol == null)
                {
                    return BadRequest(new { Error = "El rol 'Miembro' no existe en la base de datos." });
                }

                // Crea el usuario en la base de datos
                user = new Usuario
                {
                    Oid = oid,
                    Email = graphUser.Mail ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "unknown@example.com",
                    Nombre = graphUser.DisplayName ?? "Unknown",
                    DepartamentoId = departamento?.IdDepartamento,
                    CargoId = cargo?.IdCargo,
                    RolId = rol.IdRol
                };

                _context.Usuarios.Add(user);
                await _context.SaveChangesAsync();

                // Recarga el usuario con las relaciones
                user = await _context.Usuarios
                    .Include(u => u.Departamento)
                    .Include(u => u.Cargo)
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Oid == oid);

                if (user == null)
                    {
                        return BadRequest(new { Error = "No se pudo cargar el usuario en la base de datos." });
                    }
            }

            // Devuelve la información del usuario
            return Ok(new
            {
                User = new
                {
                    user.Oid,
                    user.Email,
                    user.Nombre,
                    Cargo = user.Cargo?.Nombre,
                    Departamento = user.Departamento?.Nombre,
                    Rol = user.Rol?.Nombre,
                }
            });
        }
        catch (Microsoft.Graph.ServiceException ex)
        {
            return this.StatusCode((int)ex.ResponseStatusCode, new { Error = "Error al consultar Microsoft Graph", Details = ex.Message });
        }
        catch (Exception ex)
        {
           Console.WriteLine($"Error interno en Login: {ex}");
            var errorDetails = ex.InnerException != null ? $"{ex.Message} | Inner: {ex.InnerException.Message}" : ex.Message;
            return this.StatusCode(500, new { Error = "Error interno", Details = errorDetails });
        }
    }
}
}