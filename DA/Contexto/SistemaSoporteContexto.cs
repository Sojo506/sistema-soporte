using Microsoft.EntityFrameworkCore;
using Abtracciones.Modelos;
namespace DA.Contexto
{
    //sesion de trabajo contra la base de datos que vamos a crear SistemaSoporte
    //Solo la capa DA conoce este tipo, el resto del sistema habla con interfaces.
    public class SistemaSoporteContexto : DbContext
    {
        //Las opciones (proveedor, cadena de conexion) llegan por inyeccion de dependencas desde Program.cs
        public SistemaSoporteContexto(DbContextOptions<SistemaSoporteContexto> options) : base(options)
        {

        }
        
        //Cada dbSet se convierte en una tabla. El nombre de la propiedad define el nombre de la tabla
        public DbSet<Cliente> Clientes => Set<Cliente>();
    }
}
