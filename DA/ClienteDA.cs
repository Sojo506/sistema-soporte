using Abtracciones.Interfaces;
using Abtracciones.Modelos;
using DA.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DA
{
    public class ClienteDA : IClienteDA
    {
        private readonly SistemaSoporteContexto _contexto;

        public ClienteDA(SistemaSoporteContexto contexto) => _contexto = contexto;

        public async Task<List<Cliente>> Obtener() => 
            await _contexto.Clientes.AsNoTracking().ToListAsync();

        public async Task<Cliente?> ObtenerPorId(int id) =>
            await _contexto.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id );

        public async Task<Cliente> Agregar(Cliente cliente)
        {
            _contexto.Clientes.Add(cliente);
            await _contexto.SaveChangesAsync();
            return cliente;
        }

        public async Task<bool> Actualizar(int id, Cliente datos)
        {
            var existe = await _contexto.Clientes.FindAsync(id);
            if (existe is null) return false;

            existe.Nombre = datos.Nombre;
            existe.Correo = datos.Correo;
            existe.Telefono = datos.Telefono;
            existe.Empresa = datos.Empresa;

            //TODO revisar si realmente modifica
            //_contexto.Clientes.Update(datos);
            await _contexto.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Eliminar(int id)
        {
            var existe = await _contexto.Clientes.FindAsync(id);
            if (existe is null) return false;

            _contexto.Clientes.Remove(existe);
            await _contexto.SaveChangesAsync();
            return true;
        }

    }
}
