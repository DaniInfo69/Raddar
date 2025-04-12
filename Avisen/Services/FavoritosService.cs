using System.Text.Json;
using Avisen.Models;

public static class FavoritosService
{
    private const string FavoritosKey = "promociones_favoritas";
    private const string FavoritosIdsKey = "promociones_favoritas_ids";


    public static List<Promocion>? ObtenerFavoritos()
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

    public static void EliminarDeFavoritos(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        favoritos.RemoveAll(p => p.idpromocion == promocion.idpromocion);
        GuardarFavoritos(favoritos);
    }

    public static bool EsFavorita(Promocion promocion)
    {
        var favoritos = ObtenerFavoritos();
        return favoritos.Any(p => p.idpromocion == promocion.idpromocion);
    }


    public static List<int> ObtenerIdsFavoritos()
    {
        var json = Preferences.Default.Get(FavoritosIdsKey, string.Empty);
        return string.IsNullOrEmpty(json)
            ? new List<int>()
            : JsonSerializer.Deserialize<List<int>>(json);
    }


    public static void GuardarIdsFavoritos(List<int> ids)
    {
        var json = JsonSerializer.Serialize(ids);
        Preferences.Default.Set(FavoritosIdsKey, json);
    }



    public static bool EstaMarcado(int promocionId)
    {
        return ObtenerIdsFavoritos().Contains(promocionId);
    }

    public static void AlternarFavorito(int promocionId)
    {
        var ids = ObtenerIdsFavoritos();

        if (ids.Contains(promocionId))
            ids.Remove(promocionId);
        else
            ids.Add(promocionId);

        GuardarIdsFavoritos(ids);
    }

}
