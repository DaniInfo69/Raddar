namespace Avisen.Models
{
    public class Promocion
    {
        public int idpromocion { get; set; }
        public int empresa_idempresa { get; set; }
        public int tipopromocion_idtipopromocion { get; set; } // Nuevo campo
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Precio { get; set; }
        public DateTime? VigenciaInicio { get; set; }
        public DateTime? VigenciaFin { get; set; }
        public string Tipo { get; set; }
        public List<PromocionImagen> imagenes { get; set; }
        public List<PromocionQR> qrs { get; set; } // Nuevo campo

        // ESTA PARTE NO SE QUITA de aquí para abajo
        public string NombreEmpresa { get; set; }
        public string DescripcionEmpresa { get; set; }
        public bool eliminado { get; set; }
        public string DiasRestantesTexto { get; set; }
    }

    public class PromocionImagen
    {
        public int id { get; set; }
        public string url { get; set; }
        public string public_id { get; set; }
    }

    public class PromocionQR
    {
        public string token { get; set; }
        public string url { get; set; }
        public string imageBase64 { get; set; }
        public bool usado { get; set; }
    }
}
