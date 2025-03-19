namespace Avisen.Models
{
    public class Promocion
    {
        public int empresa_idempresa { get; set; }
        public string Nombre { get; set; }       // Nombre de la promoción
        public string Descripcion { get; set; }  // Descripción de la promoción
        public string Precio { get; set; }      // Precio de la promoción
        public DateTime VigenciaInicio { get; set; }  // Fecha de inicio
        public DateTime VigenciaFin { get; set; }     // Fecha de fin
        public string Tipo { get; set; }  // Tipo de promoción
        public string ImagenUrl { get; set; }    // URL de la imagen de la promoción
    }
}
