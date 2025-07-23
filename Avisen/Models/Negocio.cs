using Microsoft.Maui.Devices.Sensors; // para usar Location
using System.Text.Json.Serialization;
namespace Avisen.Models
{
    public class Negocio
    {
        public int idempresa { get; set; }
        public int usuario_idusuario { get; set; }
        public int matriz_idmatriz { get; set; }
        public string ImagenUrl { get; set; }
        public string Nombre { get; set; }               // Nombre del negocio
        public string Descripcion { get; set; }
        public UbicacionApi? Ubicacion { get; set; }

        public List<Promocion> Promociones { get; set; } = new();

        [JsonIgnore]
        public Location? Location
        {
            get
            {
                if (Ubicacion == null) return null;

                if (Ubicacion.x < -90 || Ubicacion.x > 90)
                {
                    Console.WriteLine($"¡Coordenadas invertidas detectadas! Empresa ID: {idempresa}");
                    return new Location(Ubicacion.y, Ubicacion.x);
                }

                return new Location(Ubicacion.x, Ubicacion.y);
            }
        }



        // Modelo que refleja EXACTAMENTE la estructura de la API
        public class UbicacionApi
        {
            public double x { get; set; }
            public double y { get; set; }
        }
    }
}