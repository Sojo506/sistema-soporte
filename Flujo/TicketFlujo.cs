using Abtracciones.Interfaces;
using Abtracciones.Modelos;

namespace Flujo
{
    public class TicketFlujo : ITicketFlujo
    {
        private readonly ITicketDA _ticketDA;
        private readonly IClienteDA _clienteDA;

        // Tamaño máximo de página según Variante 0
        public const int TamanoMaximoPagina = 20;

        public TicketFlujo(ITicketDA ticketDA, IClienteDA clienteDA)
        {
            _ticketDA = ticketDA;
            _clienteDA = clienteDA;
        }

        public async Task<ResultadoOperacion<TicketPaginadoRespuestaDto>> ObtenerPaginado(TicketFiltroDto filtro)
        {
            if (filtro.Pagina < 1)
            {
                return ResultadoOperacion<TicketPaginadoRespuestaDto>.Invalido("El número de página debe ser mayor o igual a 1.");
            }

            if (filtro.Tamano < 1)
            {
                return ResultadoOperacion<TicketPaginadoRespuestaDto>.Invalido("El tamaño de página debe ser mayor o igual a 1.");
            }

            if (filtro.Tamano > TamanoMaximoPagina)
            {
                return ResultadoOperacion<TicketPaginadoRespuestaDto>.Invalido(
                    $"El tamaño de página no puede exceder {TamanoMaximoPagina} elementos (Variante 0).");
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            {
                return ResultadoOperacion<TicketPaginadoRespuestaDto>.Invalido(
                    "La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
            }

            var resultado = await _ticketDA.ObtenerPaginado(filtro);
            return ResultadoOperacion<TicketPaginadoRespuestaDto>.Ok(resultado);
        }

        public async Task<ResultadoOperacion<Ticket>> ObtenerPorId(int id)
        {
            if (id < 1)
            {
                return ResultadoOperacion<Ticket>.Invalido("El identificador del ticket debe ser un número entero mayor o igual a 1.");
            }

            var ticket = await _ticketDA.ObtenerPorId(id);
            if (ticket is null)
            {
                return ResultadoOperacion<Ticket>.NoEncontrado($"No existe ningún ticket con el Id {id}.");
            }

            return ResultadoOperacion<Ticket>.Ok(ticket);
        }

        public async Task<ResultadoOperacion<Ticket>> ObtenerPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return ResultadoOperacion<Ticket>.Invalido("El código del ticket no puede estar vacío.");
            }

            var ticket = await _ticketDA.ObtenerPorCodigo(codigo.Trim());
            if (ticket is null)
            {
                return ResultadoOperacion<Ticket>.NoEncontrado($"No existe ningún ticket con el código '{codigo}'.");
            }

            return ResultadoOperacion<Ticket>.Ok(ticket);
        }

        public async Task<ResultadoOperacion<Ticket>> Crear(TicketCrearDto dto)
        {
            // Validar existencia del cliente
            var cliente = await _clienteDA.ObtenerPorId(dto.ClienteId);
            if (cliente is null)
            {
                return ResultadoOperacion<Ticket>.Invalido($"El ClienteId {dto.ClienteId} especificado no existe en el sistema.");
            }

            var anio = DateTime.UtcNow.Year;
            var siguienteSecuencia = await _ticketDA.ObtenerSiguienteSecuenciaAnual(anio);
            var codigoGenerado = $"TK-{anio}-{siguienteSecuencia:D4}";

            var ahora = DateTime.UtcNow;
            var nuevoTicket = new Ticket
            {
                Codigo = codigoGenerado,
                Titulo = dto.Titulo.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                Prioridad = dto.Prioridad,
                Estado = EstadoTicket.Abierto, // Estado inicial siempre es Abierto
                ClienteId = dto.ClienteId,
                AgenteId = null,
                Reaperturas = 0,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            var creado = await _ticketDA.Agregar(nuevoTicket);
            return ResultadoOperacion<Ticket>.Ok(creado);
        }

        public async Task<ResultadoOperacion<Ticket>> CambiarEstado(int id, TicketCambioEstadoDto dto)
        {
            var ticket = await _ticketDA.ObtenerPorId(id);
            if (ticket is null)
            {
                return ResultadoOperacion<Ticket>.NoEncontrado($"No se encontró ningún ticket con el Id {id}.");
            }

            var estadoActual = ticket.Estado;
            var nuevoEstado = dto.Estado;

            // Determinar prioridad y agente resultantes
            var prioridadResultante = dto.Prioridad ?? ticket.Prioridad;
            var agenteResultante = dto.AgenteId ?? ticket.AgenteId;

            // Validación de transiciones de estado válidas:
            // - Abierto -> EnProceso
            // - EnProceso -> Resuelto
            // - Resuelto -> Cerrado
            // - Resuelto -> EnProceso (reapertura)
            // - Cerrado es un estado final. Cualquier otra transición es inválida (409)
            if (estadoActual == EstadoTicket.Cerrado)
            {
                return ResultadoOperacion<Ticket>.Conflicto(
                    "El ticket se encuentra Cerrado. Cerrado es un estado final y no admite transiciones.");
            }

            var transicionValida = (estadoActual, nuevoEstado) switch
            {
                (EstadoTicket.Abierto, EstadoTicket.EnProceso) => true,
                (EstadoTicket.EnProceso, EstadoTicket.Resuelto) => true,
                (EstadoTicket.Resuelto, EstadoTicket.Cerrado) => true,
                (EstadoTicket.Resuelto, EstadoTicket.EnProceso) => true,
                _ when estadoActual == nuevoEstado => true, // Mantener mismo estado permitiendo cambiar agente/prioridad
                _ => false
            };

            if (!transicionValida)
            {
                return ResultadoOperacion<Ticket>.Conflicto(
                    $"Transición de estado no permitida: no es posible pasar de '{estadoActual}' a '{nuevoEstado}'.");
            }

            // Regla de Negocio Especial - Variante 0:
            // "Un ticket de prioridad Crítica no puede pasar a Resuelto sin tener AgenteId asignado (responde 409 al violarse)."
            if (nuevoEstado == EstadoTicket.Resuelto && prioridadResultante == PrioridadTicket.Critica && !agenteResultante.HasValue)
            {
                return ResultadoOperacion<Ticket>.Conflicto(
                    "Regla de negocio (Variante 0): Un ticket de prioridad Crítica no puede pasar a Resuelto sin tener un AgenteId asignado.");
            }

            // Si es una reapertura (Resuelto -> EnProceso), incrementar contador
            if (estadoActual == EstadoTicket.Resuelto && nuevoEstado == EstadoTicket.EnProceso)
            {
                ticket.Reaperturas++;
            }

            ticket.Estado = nuevoEstado;
            if (dto.AgenteId.HasValue)
            {
                ticket.AgenteId = dto.AgenteId.Value;
            }
            if (dto.Prioridad.HasValue)
            {
                ticket.Prioridad = dto.Prioridad.Value;
            }

            ticket.FechaActualizacion = DateTime.UtcNow;

            await _ticketDA.Actualizar(ticket);
            return ResultadoOperacion<Ticket>.Ok(ticket);
        }

        public async Task<ResultadoOperacion<Ticket>> Reabrir(int id)
        {
            var ticket = await _ticketDA.ObtenerPorId(id);
            if (ticket is null)
            {
                return ResultadoOperacion<Ticket>.NoEncontrado($"No se encontró ningún ticket con el Id {id}.");
            }

            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                return ResultadoOperacion<Ticket>.Conflicto(
                    $"No se puede reabrir el ticket porque su estado actual es '{ticket.Estado}'. Solo se pueden reabrir tickets en estado Resuelto.");
            }

            ticket.Estado = EstadoTicket.EnProceso;
            ticket.Reaperturas++;
            ticket.FechaActualizacion = DateTime.UtcNow;

            await _ticketDA.Actualizar(ticket);
            return ResultadoOperacion<Ticket>.Ok(ticket);
        }

        public async Task<ResultadoOperacion<TicketResumenDto>> ObtenerResumen()
        {
            var resumen = await _ticketDA.ObtenerResumen();
            return ResultadoOperacion<TicketResumenDto>.Ok(resumen);
        }
    }
}
