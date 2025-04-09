using System.Text.Json.Serialization;
namespace Avisen.Models
{
    public class UserData
    {
        [JsonPropertyName("idusuario")]
        public int IdUsuario { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("nombrecliente")]
        public string NombreCliente { get; set; }

        [JsonPropertyName("rol_idrol")]
        public int RolIdRol { get; set; }

        [JsonPropertyName("rol")]
        public string Rol { get; set; }
    }
}
