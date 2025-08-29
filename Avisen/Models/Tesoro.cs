using System;
using System.Collections.Generic;

namespace Avisen.Models
{
    public class Tesoro
    {
        public int idtesoro { get; set; }
        public int empresa_idempresa { get; set; }
        public int categoria_idcategoria { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public string ubicacion { get; set; }  // POINT(-103.4621 19.7045)
        public int maximousuarios { get; set; }
        public DateTime vigenciainicio { get; set; }
        public DateTime vigenciafin { get; set; }
        public string tipo { get; set; }
        public List<TesoroQR> qr { get; set; } = new();
    }

    public class TesoroQR
    {
        public string token { get; set; }
        public string url { get; set; }
        public string imageBase64 { get; set; }
        public DateTime fechaexpiracion { get; set; }
        public bool usado { get; set; }
    }
}
