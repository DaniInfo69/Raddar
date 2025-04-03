namespace Avisen.Models
{
    public class Promocion
    {
        public int empresa_idempresa { get; set; }
        public int categoria_idcategoria { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Precio { get; set; }
        public DateTime VigenciaInicio { get; set; }
        public DateTime VigenciaFin { get; set; }
        public string Tipo { get; set; }
        public string ImagenUrl { get; set; }

        public string NombreEmpresa { get; set; }
        public string DescripcionEmpresa { get; set; }

    }
}
