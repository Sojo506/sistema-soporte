using System.ComponentModel.DataAnnotations;

namespace Abtracciones.Modelos
{
    public class TicketCrearDto
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 100 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Descripcion { get; set; } = string.Empty;

        public PrioridadTicket Prioridad { get; set; } = PrioridadTicket.Media;

        [Range(1, int.MaxValue, ErrorMessage = "El ClienteId debe ser un identificador válido mayor o igual a 1.")]
        public int ClienteId { get; set; }
    }

    public class TicketCambioEstadoDto
    {
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public EstadoTicket Estado { get; set; }

        public int? AgenteId { get; set; }

        public PrioridadTicket? Prioridad { get; set; }
    }

    public class TicketFiltroDto
    {
        public EstadoTicket? Estado { get; set; }
        public PrioridadTicket? Prioridad { get; set; }
        public int? ClienteId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Q { get; set; }
        public string? Orden { get; set; } = "-fecha";
        public int Pagina { get; set; } = 1;
        public int Tamano { get; set; } = 10;
    }

    public class TicketPaginadoRespuestaDto
    {
        public List<Ticket> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Pagina { get; set; }
        public int Tamano { get; set; }
    }

    public class TicketResumenDto
    {
        public int Total { get; set; }
        public Dictionary<string, int> PorEstado { get; set; } = new();
        public Dictionary<string, int> PorPrioridad { get; set; } = new();
    }
}
