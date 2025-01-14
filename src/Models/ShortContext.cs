using Microsoft.EntityFrameworkCore;
namespace Shorten.Models;
/// <summary>
/// Clase de infraestructura que representa el contexto de la base de datos
/// </summary>
public class ShortContext : DbContext
{
    /// <summary>
    /// Constructor de la clase
    /// </summary>
    /// <param name="options">opciones de conexiòn de BD</param>
    public ShortContext(DbContextOptions<ShortContext> options) : base(options)
    {
    }
    /// <summary>
    /// Propiedad que representa la tabla de mapeo de urls
    /// </summary>
    /// <value>Conjunto de UrlMapping</value>
    public DbSet<UrlMapping> UrlMappings { get; set; }
}