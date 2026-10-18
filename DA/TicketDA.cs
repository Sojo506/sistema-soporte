using Abtracciones.Interfaces;
using Abtracciones.Modelos;

namespace DA
{
    public class TicketDA : ITicketDA
    {
        private static readonly List<Ticket> _tickets = new();
        private static readonly object _lock = new();
        private static bool _semillaInicializada = false;
        private readonly IClienteDA _clienteDA;

        public TicketDA(IClienteDA clienteDA)
        {
            _clienteDA = clienteDA;
            GarantizarSemilla().GetAwaiter().GetResult();
        }

        private async Task GarantizarSemilla()
        {
            if (_semillaInicializada) return;

            // Obtener o sembrar clientes para asegurar que ClienteId corresponda a un cliente existente
            var clientes = await _clienteDA.Obtener();
            if (clientes == null || clientes.Count == 0)
            {
                var c1 = await _clienteDA.Agregar(new Cliente { Nombre = "Banco Futuro", Correo = "soporte@bancofuturo.com", Telefono = "2222-0001", Empresa = "Banco Futuro S.A." });
                var c2 = await _clienteDA.Agregar(new Cliente { Nombre = "Distribuidora Sol", Correo = "contacto@distribuidorasol.com", Telefono = "2222-0002", Empresa = "Distribuidora Sol Ltda." });
                var c3 = await _clienteDA.Agregar(new Cliente { Nombre = "Logística Central", Correo = "admin@logisticacentral.com", Telefono = "2222-0003", Empresa = "Logística Central" });
                clientes = new List<Cliente> { c1, c2, c3 };
            }

            lock (_lock)
            {
                if (_semillaInicializada) return;

                var clientIds = clientes.Select(c => c.Id).ToList();
                var anio = DateTime.UtcNow.Year;

                // Generar 26 tickets de ejemplo con variedad de estados, prioridades, fechas y agentes
                var titulosEjemplo = new[]
                {
                    ("Fallo de autenticación en portal web", "Los usuarios no pueden iniciar sesión usando credenciales corporativas.", PrioridadTicket.Critica, EstadoTicket.EnProceso, 101),
                    ("Error 500 al descargar reporte mensual", "Al presionar el botón de exportación a PDF el servicio arroja error 500.", PrioridadTicket.Alta, EstadoTicket.Abierto, (int?)null),
                    ("Lentitud en procesamiento de transferencias", "Tiempos de respuesta superiores a 15 segundos en horas pico.", PrioridadTicket.Critica, EstadoTicket.Resuelto, 102),
                    ("Solicitud de reseteo de contraseña", "El usuario bloqueó su cuenta tras varios intentos fallidos.", PrioridadTicket.Baja, EstadoTicket.Cerrado, 103),
                    ("Actualización de certificado SSL", "El certificado del dominio principal vence en 10 días.", PrioridadTicket.Alta, EstadoTicket.EnProceso, 101),
                    ("Impresora de sucursal desconectada", "La impresora de red no recibe trabajos desde la actualización de firmware.", PrioridadTicket.Media, EstadoTicket.Abierto, (int?)null),
                    ("Error de sincronización con pasarela de pagos", "Transacciones intermitentes no reflejadas en tiempo real.", PrioridadTicket.Critica, EstadoTicket.EnProceso, 104),
                    ("Solicitud de nuevo acceso VPN", "Ingreso de colaborador nuevo al departamento contable.", PrioridadTicket.Baja, EstadoTicket.Resuelto, 103),
                    ("Falla en módulo de facturación electrónica", "Rechazo de comprobantes por timeout con hacienda.", PrioridadTicket.Critica, EstadoTicket.Abierto, (int?)null),
                    ("Problema de visualización en Safari", "La barra de navegación queda superpuesta en dispositivos iOS.", PrioridadTicket.Media, EstadoTicket.EnProceso, 102),
                    ("Configuración de correo corporativo en móvil", "Asistencia técnica para configurar cuenta Exchange en Android.", PrioridadTicket.Baja, EstadoTicket.Cerrado, 105),
                    ("Bloqueo de base de datos por deadlock", "Consultas concurrentes generan bloqueos en la tabla de órdenes.", PrioridadTicket.Alta, EstadoTicket.Resuelto, 101),
                    ("Copia de seguridad nocturna fallida", "Falta de espacio en disco durante el backup diferencial.", PrioridadTicket.Alta, EstadoTicket.Abierto, (int?)null),
                    ("Solicitud de ampliación de cuota de correo", "Buzón superó el 95% de almacenamiento permitido.", PrioridadTicket.Baja, EstadoTicket.EnProceso, 105),
                    ("Discrepancia en saldos de inventario", "El reporte de existencias no coincide con las órdenes procesadas.", PrioridadTicket.Alta, EstadoTicket.Resuelto, 104),
                    ("Caída de conexión en oficina regional", "Pérdida de conectividad WAN en sede norte.", PrioridadTicket.Critica, EstadoTicket.Cerrado, 102),
                    ("Alerta de intento de fuerza bruta", "Múltiples accesos denegados desde IP no reconocida.", PrioridadTicket.Alta, EstadoTicket.EnProceso, 101),
                    ("Consulta sobre uso de API REST", "Duda sobre formato de fechas en cabeceras HTTP.", PrioridadTicket.Baja, EstadoTicket.Cerrado, 103),
                    ("Falla al generar etiquetas de envío", "El servicio de paquetería externa retorna código 401.", PrioridadTicket.Media, EstadoTicket.Abierto, (int?)null),
                    ("Capacitación sobre nuevo sistema de soporte", "Solicitud de manual de usuario para agentes de nivel 1.", PrioridadTicket.Baja, EstadoTicket.Abierto, (int?)null),
                    ("Error al adjuntar archivos mayores a 5MB", "El sistema rechaza imágenes de diagnóstico de gran tamaño.", PrioridadTicket.Media, EstadoTicket.EnProceso, 102),
                    ("Monitoreo de CPU al 98% en servidor web", "Picos de uso anormales detectados por Prometheus.", PrioridadTicket.Critica, EstadoTicket.EnProceso, 101),
                    ("Solicitud de auditoría de permisos", "Revisión trimestral de accesos de administradores.", PrioridadTicket.Media, EstadoTicket.Resuelto, 104),
                    ("Inconsistencia en zona horaria de logs", "Eventos registrados con desfase de UTC.", PrioridadTicket.Baja, EstadoTicket.Cerrado, 105),
                    ("Problema de resolución DNS interno", "Servidores no resuelven nombres de dominio locales.", PrioridadTicket.Alta, EstadoTicket.Abierto, (int?)null),
                    ("Revisión de vulnerabilidades dependencias NuGet", "Alerta automática de seguridad en librería de criptografía.", PrioridadTicket.Media, EstadoTicket.EnProceso, 101)
                };

                var baseFecha = DateTime.UtcNow.AddDays(-30);

                for (int i = 0; i < titulosEjemplo.Length; i++)
                {
                    var item = titulosEjemplo[i];
                    var fechaCreacion = baseFecha.AddHours(i * 26);
                    var fechaActualizacion = fechaCreacion.AddHours(2);
                    var clienteId = clientIds[i % clientIds.Count];
                    var id = i + 1;
                    var codigo = $"TK-{anio}-{id:D4}";

                    _tickets.Add(new Ticket
                    {
                        Id = id,
                        Codigo = codigo,
                        Titulo = item.Item1,
                        Descripcion = item.Item2,
                        Prioridad = item.Item3,
                        Estado = item.Item4,
                        ClienteId = clienteId,
                        AgenteId = item.Item5,
                        Reaperturas = item.Item4 == EstadoTicket.EnProceso && i % 3 == 0 ? 1 : 0,
                        FechaCreacion = fechaCreacion,
                        FechaActualizacion = fechaActualizacion
                    });
                }

                _semillaInicializada = true;
            }
        }

        public Task<TicketPaginadoRespuestaDto> ObtenerPaginado(TicketFiltroDto filtro)
        {
            lock (_lock)
            {
                IEnumerable<Ticket> query = _tickets;

                if (filtro.Estado.HasValue)
                {
                    query = query.Where(t => t.Estado == filtro.Estado.Value);
                }

                if (filtro.Prioridad.HasValue)
                {
                    query = query.Where(t => t.Prioridad == filtro.Prioridad.Value);
                }

                if (filtro.ClienteId.HasValue)
                {
                    query = query.Where(t => t.ClienteId == filtro.ClienteId.Value);
                }

                if (filtro.Desde.HasValue)
                {
                    query = query.Where(t => t.FechaCreacion >= filtro.Desde.Value);
                }

                if (filtro.Hasta.HasValue)
                {
                    query = query.Where(t => t.FechaCreacion <= filtro.Hasta.Value);
                }

                if (!string.IsNullOrWhiteSpace(filtro.Q))
                {
                    var busqueda = filtro.Q.Trim();
                    query = query.Where(t =>
                        t.Titulo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        t.Descripcion.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
                }

                // Ordenación: fecha, -fecha, prioridad, -prioridad. Por defecto: -fecha
                var orden = (filtro.Orden ?? string.Empty).Trim().ToLowerInvariant();
                query = orden switch
                {
                    "fecha" => query.OrderBy(t => t.FechaCreacion),
                    "-fecha" => query.OrderByDescending(t => t.FechaCreacion),
                    "prioridad" => query.OrderBy(t => t.Prioridad),
                    "-prioridad" => query.OrderByDescending(t => t.Prioridad),
                    _ => query.OrderByDescending(t => t.FechaCreacion)
                };

                var totalItems = query.Count();
                var pagina = filtro.Pagina >= 1 ? filtro.Pagina : 1;
                var tamano = filtro.Tamano >= 1 ? filtro.Tamano : 10;

                var items = query
                    .Skip((pagina - 1) * tamano)
                    .Take(tamano)
                    .Select(ClonarTicket)
                    .ToList();

                return Task.FromResult(new TicketPaginadoRespuestaDto
                {
                    Items = items,
                    TotalItems = totalItems,
                    Pagina = pagina,
                    Tamano = tamano
                });
            }
        }

        public Task<Ticket?> ObtenerPorId(int id)
        {
            lock (_lock)
            {
                var ticket = _tickets.FirstOrDefault(t => t.Id == id);
                return Task.FromResult(ticket is null ? null : ClonarTicket(ticket));
            }
        }

        public Task<Ticket?> ObtenerPorCodigo(string codigo)
        {
            lock (_lock)
            {
                var ticket = _tickets.FirstOrDefault(t =>
                    t.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(ticket is null ? null : ClonarTicket(ticket));
            }
        }

        public Task<Ticket> Agregar(Ticket ticket)
        {
            lock (_lock)
            {
                ticket.Id = _tickets.Count > 0 ? _tickets.Max(t => t.Id) + 1 : 1;
                _tickets.Add(ClonarTicket(ticket));
                return Task.FromResult(ClonarTicket(ticket));
            }
        }

        public Task<bool> Actualizar(Ticket ticket)
        {
            lock (_lock)
            {
                var existente = _tickets.FirstOrDefault(t => t.Id == ticket.Id);
                if (existente is null) return Task.FromResult(false);

                existente.Titulo = ticket.Titulo;
                existente.Descripcion = ticket.Descripcion;
                existente.Prioridad = ticket.Prioridad;
                existente.Estado = ticket.Estado;
                existente.AgenteId = ticket.AgenteId;
                existente.Reaperturas = ticket.Reaperturas;
                existente.FechaActualizacion = ticket.FechaActualizacion;

                return Task.FromResult(true);
            }
        }

        public Task<TicketResumenDto> ObtenerResumen()
        {
            lock (_lock)
            {
                var resumen = new TicketResumenDto
                {
                    Total = _tickets.Count,
                    PorEstado = _tickets
                        .GroupBy(t => t.Estado.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    PorPrioridad = _tickets
                        .GroupBy(t => t.Prioridad.ToString())
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Task.FromResult(resumen);
            }
        }

        public Task<int> ObtenerSiguienteSecuenciaAnual(int anio)
        {
            lock (_lock)
            {
                var prefijo = $"TK-{anio}-";
                var maxSecuencia = _tickets
                    .Where(t => t.Codigo.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                    .Select(t =>
                    {
                        var partes = t.Codigo.Split('-');
                        return partes.Length == 3 && int.TryParse(partes[2], out var n) ? n : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                return Task.FromResult(maxSecuencia + 1);
            }
        }

        private static Ticket ClonarTicket(Ticket t) => new()
        {
            Id = t.Id,
            Codigo = t.Codigo,
            Titulo = t.Titulo,
            Descripcion = t.Descripcion,
            Prioridad = t.Prioridad,
            Estado = t.Estado,
            ClienteId = t.ClienteId,
            AgenteId = t.AgenteId,
            Reaperturas = t.Reaperturas,
            FechaCreacion = t.FechaCreacion,
            FechaActualizacion = t.FechaActualizacion
        };
    }
}
