namespace Avisen.Models
{
    public class Guardado
    {
        public int idguardado { get; set; }
        public int promocion_idpromocion { get; set; }
        public int cliente_idcliente { get; set; }
        public DateTime fechaguardada { get; set; }
        public int eliminado { get; set; }
    }
}
