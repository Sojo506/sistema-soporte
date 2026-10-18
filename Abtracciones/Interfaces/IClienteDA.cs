using Abtracciones.Modelos;

namespace Abtracciones.Interfaces
{
    public interface IClienteDA
    {
        Task<List<Cliente>> Obtener();
        Task<Cliente> ObtenerPorId(int id);
        Task<Cliente> Agregar(Cliente cliente);

        //<returns> true si se actualizo el registro en base al id
        Task<bool> Actualizar(int id,Cliente cliente);

        //return true si el cliente se elimino, de lo contrario retorna retorna false
        Task<bool> Eliminar(int id);
    }
}
