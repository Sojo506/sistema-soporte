using Abtracciones.Modelos;

namespace Abtracciones.Interfaces
{
    public interface IClienteFlujo
    {
        Task<List<Cliente>> Obtener();

        Task<Cliente?> ObtenerPorId(int id);

        Task<Cliente> Agregar(Cliente cliente);

        Task<bool> Actualizar(int id,Cliente cliente);

        Task<bool> Eliminar(int id);
    }
}
