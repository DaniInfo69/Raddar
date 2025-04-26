using System.Text.Json;
using System.Text;
using System.Net.Http.Json;
using Avisen.Models;

public static class FavoritosService
{
    private const string FavoritosKey = "promociones_favoritas";
    private const string FavoritosIdsKey = "promociones_favoritas_ids";

    private static readonly HttpClient _httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://dapi-production-ca9b.up.railway.app/api/")
    };

    public static List<Promocion> ObtenerFavoritos()
    {
        var json = Preferences.Default.Get(FavoritosKey, string.Empty);
        return string.IsNullOrWhiteSpace(json) ? new List<Promocion>() : JsonSerializer.Deserialize<List<Promocion>>(json);
    }

    public static void GuardarFavoritos(List<Promocion> favoritos)
    {
        var json = JsonSerializer.Serialize(favoritos);
        Preferences.Default.Set(FavoritosKey, json);
    }

    public static void AgregarAFavoritos(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        if (!favoritos.Any(p => p.idpromocion == promocion.idpromocion))
        {
            favoritos.Add(promocion);
            GuardarFavoritos(favoritos);
        }
    }

    public static void MarcarComoEliminada(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        var index = favoritos.FindIndex(p => p.idpromocion == promocion.idpromocion);
        if (index >= 0)
        {
            favoritos[index].eliminado = true;
            GuardarFavoritos(favoritos);
        }
    }

    public static void DesmarcarEliminado(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        var index = favoritos.FindIndex(p => p.idpromocion == promocion.idpromocion);
        if (index >= 0)
        {
            favoritos[index].eliminado = false;
            GuardarFavoritos(favoritos);
        }
        else
        {
            promocion.eliminado = false;
            favoritos.Add(promocion);
            GuardarFavoritos(favoritos);
        }
    }

    public static bool EsFavorita(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        return favoritos.Any(p => p.idpromocion == promocion.idpromocion && !p.eliminado);
    }

    public static async Task AlternarFavoritoAsync(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        var idUsuario = await ObtenerIdUsuarioAsync();
        if (idUsuario == -1) return;

        var favoritoExistente = favoritos.FirstOrDefault(p => p.idpromocion == promocion.idpromocion);

        if (favoritoExistente != null)
        {
            if (!favoritoExistente.eliminado)
            {
                MarcarComoEliminada(promocion);
                await EliminarGuardadoEnServidorAsync(idUsuario, promocion.idpromocion);
            }
            else
            {
                DesmarcarEliminado(promocion);
                await ReactivarGuardadoEnServidorAsync(idUsuario, promocion.idpromocion);
            }
        }
        else
        {
            promocion.eliminado = false;
            AgregarAFavoritos(promocion);
            await GuardarEnServidorAsync(idUsuario, promocion.idpromocion);
        }
    }

    private static async Task GuardarEnServidorAsync(int clienteId, int promocionId)
    {
        try
        {
            var guardado = new GuardadoDTO
            {
                cliente_idcliente = clienteId,
                promocion_idpromocion = promocionId
            };

            var response = await _httpClient.PostAsJsonAsync("guardado", guardado);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al guardar en servidor: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excepción al guardar en servidor: {ex.Message}");
        }
    }

    private static async Task EliminarGuardadoEnServidorAsync(int clienteId, int promocionId)
    {
        var url = $"promocion/{promocionId}/cliente/{clienteId}/eliminar";
        await _httpClient.PutAsync(url, null);
    }

    private static async Task ReactivarGuardadoEnServidorAsync(int clienteId, int promocionId)
    {
        var url = $"deseliminar/promocion/{promocionId}/cliente/{clienteId}/eliminar";
        await _httpClient.PutAsync(url, null);
    }

    public static async Task<int> ObtenerIdUsuarioAsync()
    {
        var userJson = await SecureStorage.GetAsync("UserData");
        if (string.IsNullOrWhiteSpace(userJson)) return -1;

        var user = JsonSerializer.Deserialize<UserData>(userJson);
        return user?.IdUsuario ?? -1;
    }


}




