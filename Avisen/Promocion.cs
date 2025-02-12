namespace Avisen
{
    public class Promocion
    {
        public string Nombre { get; set; }       // Nombre de la promoción
        public string Descripcion { get; set; }  // Descripción de la promoción
        public decimal Precio { get; set; }      // Precio de la promoción
        public DateTime Vigencia { get; set; }   // Fecha de vigencia de la promoción
        public string ImagenUrl { get; set; }    // URL de la imagen de la promoción
    }
}
