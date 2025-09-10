using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Avisen.Models;
using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.Maui.Storage; // Preferences

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
                    var empresasDeLaMatriz = empresas.Where(e => e.matriz_idmatriz == matriz.idmatriz).ToList();

                    matriz.Promociones = promociones
                        .Where(p => empresasDeLaMatriz.Any(emp => emp.idempresa == p.empresa_idempresa))
                        .ToList();

                    matriz.DescripcionEmpresa = empresasDeLaMatriz.FirstOrDefault()?.Descripcion ?? "Sin descripción";
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



        public async Task<List<Negocio>> ObtenerNegociosConPromocionesAsync()
        {
            try
            {
                var negocios = await httpClient.GetFromJsonAsync<List<Negocio>>("empresa") ?? new List<Negocio>();
                var promociones = await ObtenerPromocionesAsync();

                foreach (var negocio in negocios)
                {
                    // Validar si Ubicacion viene nula
                    if (negocio?.Ubicacion == null)
                    {
                        Console.WriteLine($"Negocio sin ubicación: ID={negocio?.idempresa}, Nombre={negocio?.Nombre ?? "Sin nombre"}");
                    }

                    negocio.Promociones = promociones
                        .Where(p => p.empresa_idempresa == negocio.idempresa)
                        .ToList();
                }

                // Filtrar solo los negocios que sí tienen promociones y ubicación válida
                return negocios
                    .Where(n => n.Promociones != null && n.Promociones.Any() && n.Ubicacion != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener negocios con promociones: {ex.Message}");
                return new List<Negocio>();
            }
        }



        public async Task<List<Promocion>> ObtenerPromocionesPremiumAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                // 1️⃣ Obtener promociones premium desde el endpoint
                var promocionesPremium = await httpClient
                    .GetFromJsonAsync<List<Promocion>>("promocionPremium", options)
                    ?? new List<Promocion>();

                // 2️⃣ Enriquecer datos con info de la empresa (igual que en ObtenerPromocionesAsync)
                var empresas = await httpClient
                    .GetFromJsonAsync<List<Negocio>>("empresa")
                    ?? new List<Negocio>();

                foreach (var promo in promocionesPremium)
                {
                    var empresa = empresas.FirstOrDefault(e => e.idempresa == promo.empresa_idempresa);
                    if (empresa != null)
                    {
                        promo.NombreEmpresa = empresa.Nombre;
                        promo.DescripcionEmpresa = empresa.Descripcion;
                    }
                }

                // 3️⃣ Devolver la lista lista para bindear en el Home
                return promocionesPremium;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener promociones premium: {ex.Message}");
                return new List<Promocion>();
            }
        }


        public async Task<List<Promocion>> ObtenerPromocionesPorRangoAsync(double lat, double lng, int? rango = null)
        {
            try
            {
                // Leer preference en KM
                double prefKm = Preferences.Get("OfferDistance", 0.0);
                if (prefKm <= 0) prefKm = 0.5; // fallback por defecto (0.5 km = 500 m)

                // Si el llamador pasó rango explícito lo respetamos (se asume ya en METROS),
                // si no, usamos la preference convertida a metros
                int rangoAUsar = rango ?? (int)Math.Round(prefKm * 1000); // 🔑 conversión km -> m

                // Validación de límites en metros
                if (rangoAUsar < 50) rangoAUsar = 50;
                if (rangoAUsar > 10000) rangoAUsar = 10000;

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var payload = new
                {
                    lat = lat,
                    lng = lng,
                    rango = rangoAUsar
                };

                var json = JsonSerializer.Serialize(payload, options);
                Debug.WriteLine($"[ObtenerPromocionesPorRangoAsync] Payload: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("promocionRango", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[ObtenerPromocionesPorRangoAsync] Status: {(int)response.StatusCode} - {response.ReasonPhrase}");
                Debug.WriteLine($"[ObtenerPromocionesPorRangoAsync] Response content: {responseJson}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error en promocionRango: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    return new List<Promocion>();
                }

                var promociones = JsonSerializer.Deserialize<List<Promocion>>(responseJson, options)
                                 ?? new List<Promocion>();

                // Enriquecer con empresa
                var empresas = await httpClient.GetFromJsonAsync<List<Negocio>>("empresa", options) ?? new List<Negocio>();
                foreach (var promo in promociones)
                {
                    var empresa = empresas.FirstOrDefault(e => e.idempresa == promo.empresa_idempresa);
                    if (empresa != null)
                    {
                        promo.NombreEmpresa = empresa.Nombre;
                        promo.DescripcionEmpresa = empresa.Descripcion;
                    }
                }

                return promociones;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en ObtenerPromocionesPorRangoAsync: {ex.Message}");
                return new List<Promocion>();
            }
        }


        public async Task<List<Promocion>> ObtenerPromocionesPorCategoriaAsync(int idCategoria)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var promociones = await httpClient.GetFromJsonAsync<List<Promocion>>(
                    $"promocion/categoria/{idCategoria}", options
                ) ?? new List<Promocion>();

                // Enriquecer con datos de empresa
                var empresas = await httpClient.GetFromJsonAsync<List<Negocio>>("empresa", options)
                              ?? new List<Negocio>();

                foreach (var promo in promociones)
                {
                    var empresa = empresas.FirstOrDefault(e => e.idempresa == promo.empresa_idempresa);
                    if (empresa != null)
                    {
                        promo.NombreEmpresa = empresa.Nombre;
                        promo.DescripcionEmpresa = empresa.Descripcion;
                    }
                }

                return promociones;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerPromocionesPorCategoriaAsync: {ex.Message}");
                return new List<Promocion>();
            }
        }


        public async Task<List<Tesoro>> ObtenerTesorosAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var tesoros = await httpClient
                    .GetFromJsonAsync<List<Tesoro>>("tesoro", options)
                    ?? new List<Tesoro>();

                return tesoros;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener tesoros: {ex.Message}");
                return new List<Tesoro>();
            }
        }


        public async Task<string> ReclamarPromocionAsync(int idPromocion, int idCliente, string tokenPromocion)
        {
            if (string.IsNullOrEmpty(tokenPromocion))
                throw new InvalidOperationException("La promoción no tiene código QR.");

            var body = new { idcliente = idCliente, idpromocion = idPromocion };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var url = $"promocion/reclamar/{tokenPromocion}";
            var response = await httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    throw new InvalidOperationException("Promoción ya reclamada.");

                throw new Exception($"Error en el servidor: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("qr", out var qrElement))
                throw new Exception("No se recibió el QR en la respuesta.");

            return qrElement.GetProperty("token").GetString();
        }

    }
}
