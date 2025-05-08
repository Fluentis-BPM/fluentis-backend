using Newtonsoft.Json;
using FluentisCore.Models;
using FluentisCore.Models.UserManagement;

namespace FluentisCore.Modules.DBInit
{
    /// <summary>
    /// Clase para inicializar la base de datos con datos de ejemplo.
    /// </summary>
    /// <remarks>
    /// Esta clase se encarga de insertar datos de ejemplo en la base de datos al iniciar la aplicación.
    /// </remarks>
    public class DBInit
    {
        private readonly FluentisContext _context;

        public DBInit(FluentisContext context)
        {
            _context = context;
        }

        public void InsertCargosFromJson(string jsonData)
        {
            if (_context.Cargo.Any())
                return; // Ya hay datos, no hace nada
            dynamic jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonData);
            foreach (var cargoData in jsonObj.jerarquia)
            {
                InsertCargo(cargoData, null);
            }
            _context.SaveChanges();
        }

        private void InsertCargo(dynamic cargoData, int? jefeId)
        {
            var cargo = new Cargo
            {
                Nombre = (string)cargoData.rol,
                IdJefeCargo = jefeId            // ahora puede ser null
            };
            _context.Cargo.Add(cargo);
            _context.SaveChanges();             // Genere IdCargo

            foreach (var sub in cargoData.subordinados)
            {
                InsertCargo(sub, cargo.IdCargo);
            }
        }

        public void InsertRols()
        {
            if (_context.Roles.Any())
                return; // Ya hay datos, no hace nada
            var roles = new List<Rol>
            {
                new Rol { Nombre = "Miembro" },
                new Rol { Nombre = "Administrador" },
                new Rol { Nombre = "Visualizador General" },
                new Rol { Nombre = "Visualizador Departamental" },
            };
            _context.Roles.AddRange(roles);
            _context.SaveChanges();
        }

        public void InsertDepartamentos()
        {
            if (_context.Departamentos.Any())
                return; // Ya hay datos, no hace nada
            var departamentos = new List<Departamento>
            {
                new Departamento { Nombre = "Marketing" },
                new Departamento { Nombre = "Finanzas y Administración" },
                new Departamento { Nombre = "Tecnología de la Información" },
                new Departamento { Nombre = "Logística y Distribución" },
                new Departamento { Nombre = "Regulatorio y Cumplimiento" },
                new Departamento { Nombre = "Gerencia Médica" },
                new Departamento { Nombre = "Gestión de Procesos" },
                new Departamento { Nombre = "Dirección General" },
                new Departamento { Nombre = "Área Comercial" },
                new Departamento { Nombre = "Recursos Humanos" },
            };
            _context.Departamentos.AddRange(departamentos);
            _context.SaveChanges();
        }
    }
}