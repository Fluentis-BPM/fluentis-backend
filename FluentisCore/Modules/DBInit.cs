using Newtonsoft.Json;
using FluentisCore.Models;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.WorkflowManagement;

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

        public void InsertMockUsers()
        {
            if (_context.Usuarios.Any())
                return; // Ya hay datos, no hace nada

            // Get existing cargo IDs to ensure we only use valid ones
            var existingCargoIds = _context.Cargo.Select(c => c.IdCargo).ToList();
            var existingDepartmentIds = _context.Departamentos.Select(d => d.IdDepartamento).ToList();
            var existingRoleIds = _context.Roles.Select(r => r.IdRol).ToList();

            if (!existingCargoIds.Any() || !existingDepartmentIds.Any() || !existingRoleIds.Any())
            {
                Console.WriteLine("⚠️ Warning: Required data (Cargos, Departamentos, or Roles) not found. Skipping user creation.");
                return;
            }

            // Método auxiliar para obtener un ID seguro o el primero disponible
            int GetSafeCargoId(int index) => index < existingCargoIds.Count ? existingCargoIds[index] : existingCargoIds[0];
            int GetSafeDepartmentId(int index) => index < existingDepartmentIds.Count ? existingDepartmentIds[index] : existingDepartmentIds[0];
            int GetSafeRoleId(int index) => index < existingRoleIds.Count ? existingRoleIds[index] : existingRoleIds[0];

            var mockUsers = new List<Usuario>
            {
                // Director General
                new Usuario { Nombre = "Carlos Mendoza", Email = "carlos.mendoza@empresa.com", Oid = "5fb72c16-adf0-4dc0-a6eb-a375484f6377", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(0) },
                
                // Gerentes de primer nivel
                new Usuario { Nombre = "Ana Rodriguez", Email = "ana.rodriguez@empresa.com", Oid = "6799efc3-b8b5-493e-a812-32e8d7573f2c", DepartamentoId = GetSafeDepartmentId(1), RolId = GetSafeRoleId(1), CargoId = GetSafeCargoId(1) },
                new Usuario { Nombre = "Miguel Santos", Email = "miguel.santos@empresa.com", Oid = "24eef8aa-bd39-42a5-862f-407d496f1323", DepartamentoId = GetSafeDepartmentId(2), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(2) },
                new Usuario { Nombre = "Lucia Fernandez", Email = "lucia.fernandez@empresa.com", Oid = "0811a22d-ce6b-4215-b48c-72c6492621e5", DepartamentoId = GetSafeDepartmentId(3), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(3) },
                new Usuario { Nombre = "Roberto Castro", Email = "roberto.castro@empresa.com", Oid = "05faca75-f907-4da0-af83-060bc5a0ce4d", DepartamentoId = GetSafeDepartmentId(4), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(4) },
                new Usuario { Nombre = "Patricia Morales", Email = "patricia.morales@empresa.com", Oid = "dbfc34c5-eaf7-4af1-a92d-42a837543471", DepartamentoId = GetSafeDepartmentId(5), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(5) },
                new Usuario { Nombre = "Fernando Aguilar", Email = "fernando.aguilar@empresa.com", Oid = "bce6364c-d0e3-45d0-961e-103ca925a33d", DepartamentoId = GetSafeDepartmentId(6), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(6) },
                new Usuario { Nombre = "Sandra Vega", Email = "sandra.vega@empresa.com", Oid = "ae75266a-e11d-48b7-9534-22e480ed3503", DepartamentoId = GetSafeDepartmentId(7), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(7) },
                
                // Gerentes de segundo nivel
                new Usuario { Nombre = "Daniel Herrera", Email = "daniel.herrera@empresa.com", Oid = "61a6c2b2-bf1a-44f2-a75f-7d14b58be4c1", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(8) },
                new Usuario { Nombre = "Claudia Jimenez", Email = "claudia.jimenez@empresa.com", Oid = "fa578505-615a-4c59-b7cc-605eab837a0b", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(0), CargoId = GetSafeCargoId(9) },
                
                // Especialistas y coordinadores - usando roles no administrativos si existen
                new Usuario { Nombre = "Eduardo Ramirez", Email = "eduardo.ramirez@empresa.com", Oid = "e1ebabc9-c809-4ab2-8e13-dae82b446cf2", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(10) },
                new Usuario { Nombre = "Monica Torres", Email = "monica.torres@empresa.com", Oid = "7160091f-de25-46e6-b939-507b231e4c5e", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(11) },
                new Usuario { Nombre = "Rafael Gutierrez", Email = "rafael.gutierrez@empresa.com", Oid = "a59216f4-ea08-4130-a994-f49fc3496539", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(12) },
                new Usuario { Nombre = "Gloria Medina", Email = "gloria.medina@empresa.com", Oid = "438397f0-8902-4657-bf50-cf1dcc725099", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(13) },
                new Usuario { Nombre = "Alejandro Vargas", Email = "alejandro.vargas@empresa.com", Oid = "c1e2d26f-7cf0-40d0-b884-ae6b7b24e03f", DepartamentoId = GetSafeDepartmentId(1), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(14) },
                new Usuario { Nombre = "Carmen Silva", Email = "carmen.silva@empresa.com", Oid = "0501601b-78f3-4ac7-b5ea-cb0098feee80", DepartamentoId = GetSafeDepartmentId(1), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(15) },
                new Usuario { Nombre = "Pedro Ortega", Email = "pedro.ortega@empresa.com", Oid = "9b4136c6-7dac-4e5d-9aaf-836f83b32e40", DepartamentoId = GetSafeDepartmentId(1), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(16) },
                
                // Personal adicional con cargos existentes
                new Usuario { Nombre = "Beatriz Romero", Email = "beatriz.romero@empresa.com", Oid = "d151b884-288d-4394-a687-103715d856b6", DepartamentoId = GetSafeDepartmentId(2), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(0) },
                new Usuario { Nombre = "Oscar Delgado", Email = "oscar.delgado@empresa.com", Oid = "ad0f83bc-1d4c-4ec6-8676-463ab00c70a0", DepartamentoId = GetSafeDepartmentId(2), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(1) },
                new Usuario { Nombre = "Valeria Peña", Email = "valeria.pena@empresa.com", Oid = "85d5bfae-f494-409f-b458-32f127b50d35", DepartamentoId = GetSafeDepartmentId(2), RolId = GetSafeRoleId(2), CargoId = GetSafeCargoId(2) },
                
                // Administradores del sistema
                new Usuario { Nombre = "Admin Sistema", Email = "admin@empresa.com", Oid = "admin-001-sistema-fluentis", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(1), CargoId = GetSafeCargoId(0) },
                new Usuario { Nombre = "Visualizador General", Email = "visualizador@empresa.com", Oid = "visual-001-general-fluentis", DepartamentoId = GetSafeDepartmentId(0), RolId = GetSafeRoleId(3), CargoId = GetSafeCargoId(0) }
            };

            _context.Usuarios.AddRange(mockUsers);
            _context.SaveChanges();
            
            Console.WriteLine($"✅ Inserted {mockUsers.Count} mock users successfully.");
        }

        public void InsertMockApprovalGroups()
        {
            if (_context.GruposAprobacion.Any())
                return; // Ya hay datos, no hace nada

            var mockApprovalGroups = new List<GrupoAprobacion>
            {
                new GrupoAprobacion 
                { 
                    Nombre = "Grupo Gerencial", 
                    Fecha = DateTime.Now.AddDays(-30), 
                    EsGlobal = true 
                },
                new GrupoAprobacion 
                { 
                    Nombre = "Grupo TI", 
                    Fecha = DateTime.Now.AddDays(-25), 
                    EsGlobal = false 
                },
                new GrupoAprobacion 
                { 
                    Nombre = "Grupo Finanzas", 
                    Fecha = DateTime.Now.AddDays(-20), 
                    EsGlobal = false 
                },
                new GrupoAprobacion 
                { 
                    Nombre = "Grupo RRHH", 
                    Fecha = DateTime.Now.AddDays(-15), 
                    EsGlobal = false 
                },
                new GrupoAprobacion 
                { 
                    Nombre = "Grupo Dirección", 
                    Fecha = DateTime.Now.AddDays(-10), 
                    EsGlobal = true 
                }
            };

            _context.GruposAprobacion.AddRange(mockApprovalGroups);
            _context.SaveChanges();
        }

        public void InsertMockUserGroupRelations()
        {
            if (_context.RelacionesUsuarioGrupo.Any())
                return; // Ya hay datos, no hace nada

            // Obtener IDs reales de usuarios de la base de datos mediante coincidencia de direcciones de email
            var users = _context.Usuarios.ToList();
            if (!users.Any())
            {
                Console.WriteLine("⚠️ Warning: No users found in database. Skipping user group relations creation.");
                return;
            }

            // Obtener IDs de usuario por su email/nombre para un mapeo más confiable
            var carlosMendoza = users.FirstOrDefault(u => u.Email == "carlos.mendoza@empresa.com")?.IdUsuario;
            var anaRodriguez = users.FirstOrDefault(u => u.Email == "ana.rodriguez@empresa.com")?.IdUsuario;
            var miguelSantos = users.FirstOrDefault(u => u.Email == "miguel.santos@empresa.com")?.IdUsuario;
            var luciaFernandez = users.FirstOrDefault(u => u.Email == "lucia.fernandez@empresa.com")?.IdUsuario;
            var robertoCastro = users.FirstOrDefault(u => u.Email == "roberto.castro@empresa.com")?.IdUsuario;
            var patriciaMoreales = users.FirstOrDefault(u => u.Email == "patricia.morales@empresa.com")?.IdUsuario;
            var fernandoAguilar = users.FirstOrDefault(u => u.Email == "fernando.aguilar@empresa.com")?.IdUsuario;
            var sandraVega = users.FirstOrDefault(u => u.Email == "sandra.vega@empresa.com")?.IdUsuario;
            var adminSistema = users.FirstOrDefault(u => u.Email == "admin@empresa.com")?.IdUsuario;

            var mockUserGroupRelations = new List<RelacionUsuarioGrupo>();

            // Grupo Gerencial (ID: 1) - Gerentes principales
            if (carlosMendoza.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 1, UsuarioId = carlosMendoza.Value });
            if (anaRodriguez.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 1, UsuarioId = anaRodriguez.Value });
            if (miguelSantos.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 1, UsuarioId = miguelSantos.Value });
            if (robertoCastro.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 1, UsuarioId = robertoCastro.Value });
            if (patriciaMoreales.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 1, UsuarioId = patriciaMoreales.Value });

            // Grupo TI (ID: 2)
            if (sandraVega.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 2, UsuarioId = sandraVega.Value });
            if (adminSistema.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 2, UsuarioId = adminSistema.Value });

            // Grupo Finanzas (ID: 3)
            if (robertoCastro.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 3, UsuarioId = robertoCastro.Value });

            // Grupo RRHH (ID: 4)
            if (patriciaMoreales.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 4, UsuarioId = patriciaMoreales.Value });

            // Grupo Dirección (ID: 5) - Alta dirección
            if (carlosMendoza.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 5, UsuarioId = carlosMendoza.Value });
            if (anaRodriguez.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 5, UsuarioId = anaRodriguez.Value });
            if (robertoCastro.HasValue)
                mockUserGroupRelations.Add(new RelacionUsuarioGrupo { GrupoAprobacionId = 5, UsuarioId = robertoCastro.Value });

            if (mockUserGroupRelations.Any())
            {
                _context.RelacionesUsuarioGrupo.AddRange(mockUserGroupRelations);
                _context.SaveChanges();
                Console.WriteLine($"✅ Inserted {mockUserGroupRelations.Count} user-group relations successfully.");
            }
            else
            {
                Console.WriteLine("⚠️ Warning: No valid user-group relations could be created.");
            }
        }

        public void InsertMockInputs()
        {
            // Asegura que todos los valores del enum TipoInput existan en la tabla Inputs.
            // Idempotente: solo inserta los que faltan; no duplica existentes.
            var existing = _context.Inputs
                .Select(i => i.TipoInput)
                .ToHashSet();

            var defaults = new Dictionary<TipoInput, bool>
            {
                { TipoInput.TextoCorto, false },
                { TipoInput.TextoLargo, false },
                { TipoInput.Combobox, true },
                { TipoInput.MultipleCheckbox, true },
                { TipoInput.Date, false },
                { TipoInput.Number, false },
                { TipoInput.Archivo, false },
            };

            var toAdd = new List<Inputs>();
            foreach (var kvp in defaults)
            {
                if (!existing.Contains(kvp.Key))
                {
                    toAdd.Add(new Inputs { TipoInput = kvp.Key, EsJson = kvp.Value });
                }
            }

            if (toAdd.Count > 0)
            {
                _context.Inputs.AddRange(toAdd);
                _context.SaveChanges();
                Console.WriteLine($"✅ Inserted {toAdd.Count} input catalog item(s) (Tipos de Input).");
            }
            else
            {
                Console.WriteLine("ℹ️ Input catalog already up to date (Tipos de Input).");
            }
        }

        public void InsertMockWorkflows()
        {
            if (_context.FlujosAprobacion.Any())
                return; // Ya hay datos, no hace nada

            // Get the first available user to be the creator
            var firstUserId = _context.Usuarios.Select(u => u.IdUsuario).FirstOrDefault();
            if (firstUserId == 0)
            {
                Console.WriteLine("⚠️ Warning: No users found in database. Skipping workflow creation.");
                return;
            }

            var mockWorkflows = new List<FlujoAprobacion>
            {
                new FlujoAprobacion 
                { 
                    Nombre = "Aprobación de Solicitudes de TI", 
                    Descripcion = "Proceso para aprobar solicitudes relacionadas con tecnología", 
                    VersionActual = 1, 
                    EsPlantilla = true,
                    CreadoPor = firstUserId
                },
                new FlujoAprobacion 
                { 
                    Nombre = "Aprobación de Gastos", 
                    Descripcion = "Proceso para aprobar gastos y presupuestos", 
                    VersionActual = 1, 
                    EsPlantilla = true,
                    CreadoPor = firstUserId
                },
                new FlujoAprobacion 
                { 
                    Nombre = "Contratación de Personal", 
                    Descripcion = "Proceso para aprobación de contrataciones", 
                    VersionActual = 1, 
                    EsPlantilla = true,
                    CreadoPor = firstUserId
                }
            };

            _context.FlujosAprobacion.AddRange(mockWorkflows);
            _context.SaveChanges();
            Console.WriteLine($"✅ Inserted {mockWorkflows.Count} mock workflows successfully.");
        }

        public void InsertMockSolicitudes()
        {
            if (_context.Solicitudes.Any())
                return; // Ya hay datos, no hace nada

            var mockSolicitudes = new List<Solicitud>
            {
                new Solicitud
                {
                    Nombre = "Solicitud de nuevo servidor",
                    Descripcion = "Requerimiento de un nuevo servidor para el departamento de TI para manejar el incremento de la carga de trabajo",
                    SolicitanteId = 21, // Francisco Luna - Personal de TI
                    FlujoBaseId = 1, // Flujo de TI
                    Estado = EstadoSolicitud.Pendiente
                },
                new Solicitud
                {
                    Nombre = "Aprobación de presupuesto anual Marketing",
                    Descripcion = "Solicitud de aprobación del presupuesto anual para campañas de marketing digital",
                    SolicitanteId = 15, // Alejandro Vargas - Marketing
                    FlujoBaseId = 2, // Flujo de gastos
                    Estado = EstadoSolicitud.Pendiente
                },
                new Solicitud
                {
                    Nombre = "Contratación Desarrollador Senior",
                    Descripcion = "Solicitud para contratar un desarrollador senior para el equipo de TI",
                    SolicitanteId = 8, // Sandra Vega - Gerente de TI
                    FlujoBaseId = 3, // Flujo de contratación
                    Estado = EstadoSolicitud.Pendiente
                },
                new Solicitud
                {
                    Nombre = "Compra de licencias software",
                    Descripcion = "Adquisición de licencias de software de desarrollo para el equipo",
                    SolicitanteId = 22, // Isabel Navarro - Personal de TI
                    FlujoBaseId = 2, // Flujo de gastos
                    Estado = EstadoSolicitud.Aprobado
                },
                new Solicitud
                {
                    Nombre = "Solicitud vacaciones extendidas",
                    Descripcion = "Solicitud de vacaciones extendidas por motivos personales",
                    SolicitanteId = 27, // Hugo Mendez - Finanzas
                    Estado = EstadoSolicitud.Rechazado
                }
            };

            _context.Solicitudes.AddRange(mockSolicitudes);
            _context.SaveChanges();
        }

        public void InsertMockRelacionInputs()
        {
            if (_context.RelacionesInput.Any())
                return; // Ya hay datos, no hace nada

            var mockRelacionInputs = new List<RelacionInput>
            {
                // Para la solicitud de servidor (ID: 1)
                new RelacionInput
                {
                    InputId = 1, // TextoCorto
                    Nombre = "Especificaciones del servidor",
                    Valor = "Intel Xeon, 32GB RAM, 1TB SSD",
                    PlaceHolder = "Ingrese las especificaciones técnicas",
                    Requerido = true,
                    SolicitudId = 1
                },
                new RelacionInput
                {
                    InputId = 6, // Number
                    Nombre = "Costo estimado",
                    Valor = "15000",
                    PlaceHolder = "Costo en USD",
                    Requerido = true,
                    SolicitudId = 1
                },
                
                // Para la solicitud de presupuesto marketing (ID: 2)
                new RelacionInput
                {
                    InputId = 2, // TextoLargo
                    Nombre = "Justificación del presupuesto",
                    Valor = "El presupuesto permitirá ejecutar campañas digitales enfocadas en redes sociales, Google Ads y marketing de contenidos para incrementar la visibilidad de marca en un 25%",
                    PlaceHolder = "Describa la justificación detallada",
                    Requerido = true,
                    SolicitudId = 2
                },
                new RelacionInput
                {
                    InputId = 6, // Number
                    Nombre = "Monto solicitado",
                    Valor = "50000",
                    PlaceHolder = "Monto en USD",
                    Requerido = true,
                    SolicitudId = 2
                },

                // Para la solicitud de contratación (ID: 3)
                new RelacionInput
                {
                    InputId = 1, // TextoCorto
                    Nombre = "Posición a cubrir",
                    Valor = "Desarrollador Senior Full Stack",
                    PlaceHolder = "Título del puesto",
                    Requerido = true,
                    SolicitudId = 3
                },
                new RelacionInput
                {
                    InputId = 2, // TextoLargo
                    Nombre = "Perfil requerido",
                    Valor = "5+ años de experiencia en .NET, React, SQL Server. Experiencia en metodologías ágiles y liderazgo técnico",
                    PlaceHolder = "Describa el perfil ideal",
                    Requerido = true,
                    SolicitudId = 3
                }
            };

            _context.RelacionesInput.AddRange(mockRelacionInputs);
            _context.SaveChanges();
        }
    }
}