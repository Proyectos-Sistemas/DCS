using Homer_MVC;
using System.Data.Entity;

public class OracleDbContext : DbContext
{
    public OracleDbContext()
        : base("name=OracleDbContext") // cadena en Web.config
    {
    }
    public  DbSet<ProductoModel> Producto { get; set; }
    public  DbSet<CategoriaModel> Categoria { get; set; }
    public  DbSet<DepartamentoModel> Departamento { get; set; }
    public DbSet<ParametroModel> Parametro { get; set; }
    public DbSet<SolicitudModel> Solicitud { get; set; }



}
