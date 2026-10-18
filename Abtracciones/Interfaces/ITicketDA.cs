using Abtracciones.Modelos;

namespace Abtracciones.Interfaces
{
    public interface ITicketDA
    {
        Task<TicketPaginadoRespuestaDto> ObtenerPaginado(TicketFiltroDto filtro);
        Task<Ticket?> ObtenerPorId(int id);
        Task<Ticket?> ObtenerPorCodigo(string codigo);
        Task<Ticket> Agregar(Ticket ticket);
        Task<bool> Actualizar(Ticket ticket);
        Task<TicketResumenDto> ObtenerResumen();
        Task<int> ObtenerSiguienteSecuenciaAnual(int anio);
    }
}
