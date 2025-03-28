using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Avisen.Models;
using System.Net.Http.Json;
using System.Diagnostics;

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


        public async Task<List<Matriz>> ObtenerMatricesConPromocionesAsync()
        {
            try
            {
                var matrices = await httpClient.GetFromJsonAsync<List<Matriz>>("matriz") ?? new List<Matriz>();
                var empresas = await httpClient.GetFromJsonAsync<List<Negocio>>("empresa") ?? new List<Negocio>();
                var promociones = await ObtenerPromocionesAsync();

                foreach (var matriz in matrices)
                {
                    // Buscar la empresa que tiene esta matriz como su sede
                    var empresa = empresas.FirstOrDefault(e => e.matriz_idmatriz == matriz.idmatriz);
                    matriz.DescripcionEmpresa = empresa?.Descripcion ?? "Sin descripción";

                    // Obtener promociones de la empresa asociada a la matriz
                    matriz.Promociones = promociones.Where(p => p.empresa_idempresa == empresa?.idempresa).ToList();

                    Console.WriteLine($"Matriz: {matriz.Nombre}, Empresa: {empresa?.Nombre}, Descripción: {matriz.DescripcionEmpresa}, Ubicación: {matriz.Ubicacion}");
                    Console.WriteLine($"Matriz: {matriz.Nombre} tiene {matriz.Promociones.Count} promociones.");
                }

                return matrices;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerMatricesConPromocionesAsync: {ex.Message}");
                return new List<Matriz>();
            }
        }


    }
}
