
namespace Avisen.Models
{
    public class Negocio
    {
        public int idempresa { get; set; }
        public string ImagenUrl { get; set; }
        public string Nombre { get; set; }               // Nombre del negocio
        public string Descripcion { get; set; }
        public Location Ubicacion { get; set; }          // Ubicación del negocio
        public List<Promocion> Promociones { get; set; } // Lista de promociones
    }

}
