using Avisen.Models;
using Avisen.Services;

namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    private readonly Location _ubicacionPromocion;
    private bool _esFavorita;


    public PromocionDetallesPage(Promocion promocion, Location ubicacionPromocion)
    {
        InitializeComponent();

        // Configuramos el BindingContext con la promoción
        BindingContext = promocion;

        _ubicacionPromocion = ubicacionPromocion;

        // Mostramos los detalles directamente desde el Binding
        PromocionesLabel.FormattedText = ObtenerDetallesPromocion(promocion);
        VigenciaLabel.Text = promocion.VigenciaInicio.ToShortDateString();
        VigenciaLabel2.Text = promocion.VigenciaFin.ToShortDateString();

        _esFavorita = FavoritosService.EstaMarcado(promocion.idpromocion);
        ActualizarCorazon();
    }

    private FormattedString ObtenerDetallesPromocion(Promocion promocion)
    {
        var formattedString = new FormattedString();

        // Nombre en negritas
        formattedString.Spans.Add(new Span
        {
            Text = promocion.Nombre + "\n\n",
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            TextColor = Color.FromArgb("#19535F"),
        });

        // Descripción normal
        formattedString.Spans.Add(new Span
        {
            Text = promocion.Descripcion + "\n\n",
            FontSize = 16,
            TextColor = Color.FromArgb("#602020")
        });

        // Precio
        formattedString.Spans.Add(new Span
        {
            Text = $"Precio: {(promocion.Precio == null ? "Oferta especial" : $"${promocion.Precio} mxn")}\n\n",
            FontSize = 16,
            TextColor = Color.FromArgb("#19535F")
        });

        // Tipo de promoción
        formattedString.Spans.Add(new Span
        {
            Text = $"Tipo de promoción: {promocion.Tipo}",
            FontSize = 16,
            TextColor = Color.FromArgb("#19535F")
        });

        return formattedString;
    }

    // Mantenemos los mismos métodos de eventos
    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void IrAOfertaClicked(object sender, EventArgs e)
    {
        NavigationService.LocationToGo = _ubicacionPromocion;

        bool answer = await DisplayAlert("Indicaciones", "¿Indicaciones para ir a la promoción?", "Si", "No");

        if (answer)
        {
            await NavigationService.AbrirNavegacion(_ubicacionPromocion);
        }

        await Navigation.PopModalAsync();

        await Shell.Current.GoToAsync("//Map");
    }


    private void OnHeartTapped(object sender, EventArgs e)
    {
        _esFavorita = !_esFavorita;

        FavoritosService.AlternarFavorito(((Promocion)BindingContext).idpromocion);
        ActualizarCorazon();

        var promocion = BindingContext as Promocion;

        if (promocion == null)
            return;

        if (FavoritosService.EsFavorita(promocion))
        {
            FavoritosService.EliminarDeFavoritos(promocion);
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(0);
            HeartAnimation.IsAnimationEnabled = false;
        }
        else
        {
            FavoritosService.AgregarAFavoritos(promocion);
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(1100);
            HeartAnimation.IsAnimationEnabled = true;
        }
    }

    private void ActualizarCorazon()
    {
        HeartAnimation.Progress = _esFavorita ? TimeSpan.FromMilliseconds(2000) : TimeSpan.FromMilliseconds(0);
        HeartAnimation.IsAnimationEnabled = _esFavorita;
    }


}