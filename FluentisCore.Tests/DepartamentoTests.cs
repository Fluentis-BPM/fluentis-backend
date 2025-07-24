using FluentisCore.Models;
using FluentisCore.Models.UserManagement;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FluentisCore.Tests
{
    public class DepartamentoTests
    {
        [Fact]
        public void CanCreateUpdateDeleteDepartamento()
        {
            var options = new DbContextOptionsBuilder<FluentisContext>()
                .UseInMemoryDatabase(databaseName: "TestDb2")
                .Options;

            // Create
            using (var context = new FluentisContext(options))
            {
                var dept = new Departamento { Nombre = "IT" };
                context.Departamentos.Add(dept);
                context.SaveChanges();
            }

            // Update
            using (var context = new FluentisContext(options))
            {
                var dept = context.Departamentos.First();
                dept.Nombre = "HR";
                context.SaveChanges();
            }

            // Delete
            using (var context = new FluentisContext(options))
            {
                var dept = context.Departamentos.First();
                context.Departamentos.Remove(dept);
                context.SaveChanges();

                Assert.Empty(context.Departamentos);
            }
        }
    }
}
