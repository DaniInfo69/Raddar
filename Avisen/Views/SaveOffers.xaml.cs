using Avisen.Models;
using System.Collections.ObjectModel;

namespace Avisen.Views;

public partial class SaveOffers : ContentPage
{
    public List<Promocion> Favoritos { get; set; }

    public SaveOffers()
	{
		InitializeComponent();
        CargarFavoritos();
    }

    private void CargarFavoritos()
    {
        Favoritos = FavoritosService.ObtenerFavoritos();
        FavoritosCollection.ItemsSource = Favoritos;
    }

    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}