using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices.Sensors;

namespace Avisen.Models
{
    public class Matriz
    {
        public int idmatriz { get; set; }
        public string Nombre { get; set; }

        // Cambiado para reflejar exactamente lo que devuelve la API
        public UbicacionApi Ubicacion { get; set; }

        public string Telefono { get; set; }
        public string Email { get; set; }
        public int Eliminado { get; set; }
        public List<Promocion> Promociones { get; set; } = new List<Promocion>();
        public string DescripcionEmpresa { get; set; }

        // Propiedad calculada para compatibilidad con MAUI
        [JsonIgnore]
        public Location Location
        {
            get
            {
                if (Ubicacion.x < -90 || Ubicacion.x > 90)
                {
                    Console.WriteLine($"¡Coordenadas invertidas detectadas! Matriz ID: {idmatriz}");
                    return new Location(Ubicacion.y, Ubicacion.x);
                }

                return new Location(Ubicacion.x, Ubicacion.y);
            }
        }
    }

    // Modelo que refleja EXACTAMENTE la estructura de la API
    public class UbicacionApi
    {
        public double x { get; set; }
        public double y { get; set; }
    }
}