using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avisen
{
    public class Negocio
    {
        public string ImagenUrl { get; set; }
        public string Nombre { get; set; }               // Nombre del negocio
        public Location Ubicacion { get; set; }          // Ubicación del negocio
        public List<Promocion> Promociones { get; set; } // Lista de promociones
    }

}
