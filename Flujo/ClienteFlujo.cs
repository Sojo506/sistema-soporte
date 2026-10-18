using Abtracciones.Modelos;
using Abtracciones.Interfaces;

namespace Flujo
{
    public class ClienteFlujo : IClienteFlujo
    {
        private readonly IClienteDA _clienteDA;
        
        public ClienteFlujo(IClienteDA clienteDA) => _clienteDA = clienteDA;

        public Task<List<Cliente>> Obtener() => _clienteDA.Obtener();

        public Task<Cliente?> ObtenerPorId(int id) => _clienteDA.ObtenerPorId(id);

        public Task<Cliente> Agregar(Cliente cliente)
        {
            //regla de negocio que ek correo se guarde normalizado
            cliente.Correo = cliente.Correo.Trim().ToLowerInvariant();

            return _clienteDA.Agregar(cliente);
        }

        public Task<bool> Actualizar(int id,Cliente datos)
        {
            //regla de negocio que ek correo se guarde normalizado
            datos.Correo = datos.Correo.Trim().ToLowerInvariant();

            return _clienteDA.Actualizar(id,datos);
        }

        public Task<bool> Eliminar(int id) => _clienteDA.Eliminar(id);

    }
}
