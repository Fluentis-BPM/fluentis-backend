using FluentisCore.Models.UserManagement;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace FluentisCore.Tests
{
    public class UsuarioValidationTests
    {
        [Fact]
        public void UsuarioEmailIsRequired()
        {
            var usuario = new Usuario
            {
                Nombre = "NoEmail"
                // Email is missing
            };

            var context = new ValidationContext(usuario, null, null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(usuario, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }
    }

}
