using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avisen.Models
{
    public class Ubicacion
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class Favorito
    {
        public int idfavorito { get; set; }
        public string Nombre { get; set; }
        public Ubicacion Ubicacion { get; set; }
        public int cliente_idcliente { get; set; }
        public int eliminado { get; set; }
    }
}