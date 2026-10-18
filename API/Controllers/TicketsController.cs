using System.Security.Cryptography;
using System.Text;
using Abtracciones.Interfaces;
using Abtracciones.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketFlujo _ticketFlujo;

        public TicketsController(ITicketFlujo ticketFlujo)
        {
            _ticketFlujo = ticketFlujo;
        }

        /// <summary>
        /// Obtiene una lista paginada de tickets con filtros y ordenación.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Ticket>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Obtener([FromQuery] TicketFiltroDto filtro)
        {
            var resultado = await _ticketFlujo.ObtenerPaginado(filtro);
            if (!resultado.Exito || resultado.Valor is null)
            {
                return TraducirFalla(resultado);
            }

            var paginado = resultado.Valor;
            Response.Headers["X-Total-Count"] = paginado.TotalItems.ToString();
            Response.Headers["X-Page"] = paginado.Pagina.ToString();
            Response.Headers["X-Page-Size"] = paginado.Tamano.ToString();

            return Ok(paginado.Items);
        }

        /// <summary>
        /// Obtiene un ticket por su identificador numérico (solo enteros >= 1).
        /// Soporta caché condicional con ETag y responde 304 si el recurso no ha cambiado.
        /// </summary>
        [HttpGet("{id:int:min(1)}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Ticket))]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var resultado = await _ticketFlujo.ObtenerPorId(id);
            if (!resultado.Exito || resultado.Valor is null)
            {
                return TraducirFalla(resultado);
            }

            var ticket = resultado.Valor;
            var etag = CalcularETag(ticket);

            // Verificación de caché condicional If-None-Match
            if (Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch) &&
                string.Equals(ifNoneMatch.ToString(), etag, StringComparison.Ordinal))
            {
                Response.Headers.ETag = etag;
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = etag;
            return Ok(ticket);
        }

        /// <summary>
        /// Obtiene un ticket por su código usando restricción de ruta personalizada para Variante 0 (TK-AAAA-NNNN).
        /// Si el formato no coincide, la ruta no empareja y se responde 404 antes de ejecutar este método.
        /// </summary>
        [HttpGet("codigo/{codigo:codigoTicket}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Ticket))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> ObtenerPorCodigo(string codigo)
        {
            var resultado = await _ticketFlujo.ObtenerPorCodigo(codigo);
            if (!resultado.Exito || resultado.Valor is null)
            {
                return TraducirFalla(resultado);
            }

            return Ok(resultado.Valor);
        }

        /// <summary>
        /// Crea un nuevo ticket con estado inicial Abierto y código autogenerado.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Ticket))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Crear([FromBody] TicketCrearDto dto)
        {
            var resultado = await _ticketFlujo.Crear(dto);
            if (!resultado.Exito || resultado.Valor is null)
            {
                return TraducirFalla(resultado);
            }

            var creado = resultado.Valor;
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
        }

        /// <summary>
        /// Modifica el estado, agente o prioridad de un ticket existente validando reglas de transición y Variante 0.
        /// </summary>
        [HttpPatch("{id:int:min(1)}/estado")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Ticket))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] TicketCambioEstadoDto dto)
        {
            var resultado = await _ticketFlujo.CambiarEstado(id, dto);
            if (!resultado.Exito || resultado.Valor is null)
            {
                return TraducirFalla(resultado);
            }

            return Ok(resultado.Valor);
        }

        private static string CalcularETag(Ticket ticket)
        {
            var raw = $"{ticket.Id}:{ticket.Codigo}:{ticket.FechaActualizacion.Ticks}:{ticket.Estado}:{ticket.Prioridad}:{ticket.AgenteId}:{ticket.Reaperturas}";
            var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"\"{hash}\"";
        }

        private ActionResult TraducirFalla<T>(ResultadoOperacion<T> resultado)
        {
            return resultado.TipoError switch
            {
                TipoErrorOperacion.NoEncontrado => Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Recurso no encontrado",
                    detail: resultado.Mensaje),
                TipoErrorOperacion.ReglaNegocio => Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflicto con regla de negocio",
                    detail: resultado.Mensaje),
                _ => Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Solicitud inválida",
                    detail: resultado.Mensaje)
            };
        }
    }
}
