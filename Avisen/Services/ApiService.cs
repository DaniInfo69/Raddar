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
                BaseAddress = new Uri("https://raddar.softwareinsights.com.mx/api/")
            };
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object jsonRequest)
        {
            var content = new StringContent(JsonSerializer.Serialize(jsonRequest), Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(endpoint, content);
        }

        public async Task<List<Favorito>> ObtenerFavoritosPorUsuarioAsync(int idUsuario)
        {
            try
            {
                var response = await httpClient.GetAsync($"favorito/usuario/{idUsuario}");
                Debug.WriteLine($"Obteniendo favoritos para el usuario {idUsuario} desde la API: {response.RequestMessage?.RequestUri}");
                if (response.IsSuccessStatusCode)
                {
                    var favoritos = await response.Content.ReadFromJsonAsync<List<Favorito>>();
                    return favoritos ?? new List<Favorito>();
                }

                return new List<Favorito>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener favoritos: {ex.Message}");
                return new List<Favorito>();
            }
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

                var promociones = await httpClient.GetFromJsonAsync<List<Promocion>>("promocion", options) ?? new List<Promocion>();
                var empresas = await httpClient.GetFromJsonAsync<List<Negocio>>("empresa") ?? new List<Negocio>();

                // Enriquecer promociones con datos de la empresa
                foreach (var promocion in promociones)
                {
                    var empresa = empresas.FirstOrDefault(e => e.idempresa == promocion.empresa_idempresa);
                    if (empresa != null)
                    {
                        promocion.NombreEmpresa = empresa.Nombre;
                        promocion.DescripcionEmpresa = empresa.Descripcion;
                    }
                }

                return promociones;
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

        public async Task<List<Categoria>> ObtenerCategoriaAsync()
        {
            try
            {
                var categorias = await httpClient.GetFromJsonAsync<List<Categoria>>("categoria") ?? new List<Categoria>();

                return categorias;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener categorias: {ex.Message}");
                return new List<Categoria>();
            }
        }


        public async Task<List<Promocion>> ObtenerFavoritosDesdeServidorAsync(int idUsuario)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var guardados = await httpClient.GetFromJsonAsync<List<Guardado>>($"guardadoUsuario/{idUsuario}", options)
                                 ?? new List<Guardado>();

                var promociones = await ObtenerPromocionesAsync();

                var idsPromosGuardadas = guardados
                    .Where(g => g.eliminado == 0)
                    .Select(g => g.promocion_idpromocion)
                    .ToHashSet();

                var promocionesGuardadas = promociones
                    .Where(p => idsPromosGuardadas.Contains(p.idpromocion))
                    .ToList();

                return promocionesGuardadas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener promociones guardadas: {ex.Message}");
                return new List<Promocion>();
            }
        }


        public async Task<List<Matriz>> ObtenerPromocionesEnRangoAsync(double lat, double lng, double rango)
        {
            try
            {
                var url = $"https://TU_API_URL/promocionRango?lat={lat}&lng={lng}&rango={rango}";
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Matriz>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ObtenerPromocionesEnRangoAsync: {ex.Message}");
                return new List<Matriz>();
            }
        }

    }
}
