using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Avisen.Models;
using System.Net.Http.Json;

namespace Avisen.Services
{
    public class ApiService : NegocioService
    {
        private static readonly HttpClient httpClient;

        // Inicializador estático para configurar HttpClient una sola vez
        static ApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://dapi-production-ca9b.up.railway.app/api/")
            };
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object jsonRequest)
        {
            var content = new StringContent(JsonSerializer.Serialize(jsonRequest), Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(endpoint, content);
        }

        public async Task<List<Negocio>> ObtenerNegociosAsync()
        {
            try
            {
                var negocios = await httpClient.GetFromJsonAsync<List<Negocio>>("empresa") ?? new List<Negocio>();
                var promociones = await ObtenerPromocionesAsync();

                // Relacionar promociones con negocios
                foreach (var negocio in negocios)
                {
                    negocio.Promociones = promociones.Where(p => p.empresa_idempresa == negocio.idempresa).ToList();
                }

                return negocios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener negocios: {ex.Message}");
                return new List<Negocio>();
            }
        }


        public async Task<List<Promocion>> ObtenerPromocionesAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var promociones = await httpClient.GetFromJsonAsync<List<Promocion>>("promocion", options);
                return promociones ?? new List<Promocion>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener promociones: {ex.Message}");
                return new List<Promocion>();
            }
        }
    }
}
