using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avisen.Services
{
    public class ApiService
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
    }
}