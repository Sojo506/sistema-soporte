using Abtracciones.Modelos;

namespace Abtracciones.Interfaces
{
    public interface ITicketFlujo
    {
        Task<ResultadoOperacion<TicketPaginadoRespuestaDto>> ObtenerPaginado(TicketFiltroDto filtro);
        Task<ResultadoOperacion<Ticket>> ObtenerPorId(int id);
        Task<ResultadoOperacion<Ticket>> ObtenerPorCodigo(string codigo);
        Task<ResultadoOperacion<Ticket>> Crear(TicketCrearDto dto);
        Task<ResultadoOperacion<Ticket>> CambiarEstado(int id, TicketCambioEstadoDto dto);
        Task<ResultadoOperacion<Ticket>> Reabrir(int id);
        Task<ResultadoOperacion<TicketResumenDto>> ObtenerResumen();
    }
}
