using Avisen.Models;
using Avisen.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Networking;
using System.Text.Json; // Para Connectivity

namespace Avisen.Views;

public partial class SaveOffers : ContentPage
{
    public List<Promocion> Favoritos { get; set; }

    private readonly ApiService _apiService = new();

    public SaveOffers()
    {
        InitializeComponent();
        CargarFavoritos();
    }

    private async void CargarFavoritos()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                var userDataJson = await SecureStorage.GetAsync("UserData");

                if (!string.IsNullOrEmpty(userDataJson))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var userData = JsonSerializer.Deserialize<UserData>(userDataJson, options);
                    int idUsuario = userData?.IdUsuario ?? 0;

                    if (idUsuario > 0)
                    {
                        Favoritos = await _apiService.ObtenerFavoritosDesdeServidorAsync(idUsuario);
                        await DisplayAlert("Debug", $"Promociones encontradas: {Favoritos.Count}", "OK");

                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo obtener el ID del usuario.", "OK");
                        Favoritos = FavoritosService.ObtenerFavoritos(); // backup local
                    }
                }
                else
                {
                    await DisplayAlert("Advertencia", "No se encontró información del usuario en SecureStorage.", "OK");
                    Favoritos = FavoritosService.ObtenerFavoritos(); // backup local
                }
            }
            else
            {
                Favoritos = FavoritosService.ObtenerFavoritos(); // sin conexión
            }

            FavoritosCollection.ItemsSource = Favoritos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un error al cargar favoritos: {ex.Message}", "OK");
        }
    }


    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
