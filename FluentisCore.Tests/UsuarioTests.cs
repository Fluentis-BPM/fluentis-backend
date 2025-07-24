namespace FluentisCore.Tests
{
    using FluentisCore.Models;
    using FluentisCore.Models.UserManagement;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    public class UsuarioTests
    {
        [Fact]
        public void CanAddAndRetrieveUsuario()
        {
            var options = new DbContextOptionsBuilder<FluentisContext>()
                .UseInMemoryDatabase(databaseName: "TestDb1")
                .Options;

            using (var context = new FluentisContext(options))
            {
                var usuario = new Usuario
                {
                    Nombre = "Test User",
                    Email = "test@example.com",
                    Oid = "12345"
                };
                context.Usuarios.Add(usuario);
                context.SaveChanges();
            }

            using (var context = new FluentisContext(options))
            {
                var usuario = context.Usuarios.FirstOrDefault(u => u.Email == "test@example.com");
                Assert.NotNull(usuario);
                Assert.Equal("Test User", usuario.Nombre);
            }
        }
    }
}
