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
        VigenciaLabel.Text = promocion.VigenciaInicio.HasValue ? promocion.VigenciaInicio.Value.ToShortDateString() : "Sin fecha de inicio";
        VigenciaLabel2.Text = promocion.VigenciaFin.HasValue ? promocion.VigenciaFin.Value.ToShortDateString() : "Sin fecha de fin";

        _esFavorita = FavoritosService.EsFavorita(promocion);
        ActualizarCorazon();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is Promocion promo
            && promo.qrs != null
            && promo.qrs.Count > 0
            && !string.IsNullOrEmpty(promo.qrs[0].imageBase64))
        {
            try
            {
                // Quitamos el prefijo si existe
                var base64Data = promo.qrs[0].imageBase64;
                var commaIndex = base64Data.IndexOf(',');
                if (commaIndex >= 0)
                {
                    base64Data = base64Data.Substring(commaIndex + 1);
                }

                byte[] imageBytes = Convert.FromBase64String(base64Data);
                QrImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar QR: {ex.Message}");
            }
        }
    }



    private FormattedString ObtenerDetallesPromocion(Promocion promocion)
    {
        var formattedString = new FormattedString();

        // Nombre en negritas
        formattedString.Spans.Add(new Span
        {
            Text = (promocion.Nombre ?? "Sin nombre") + "\n\n",
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            TextColor = Color.FromArgb("#19535F"),
        });

        // Descripción normal
        formattedString.Spans.Add(new Span
        {
            Text = (promocion.Descripcion ?? "Sin descripción") + "\n\n",
            FontSize = 16,
            TextColor = Color.FromArgb("#602020")
        });

        // Precio
        formattedString.Spans.Add(new Span
        {
            Text = $"Precio: {(string.IsNullOrWhiteSpace(promocion.Precio) ? "Oferta especial" : $"${promocion.Precio} mxn")}\n\n",
            FontSize = 16,
            TextColor = Color.FromArgb("#19535F")
        });

        // Tipo de promoción
        formattedString.Spans.Add(new Span
        {
            Text = $"Tipo de promoción: {(promocion.Tipo ?? "No especificado")}",
            FontSize = 16,
            TextColor = Color.FromArgb("#19535F")
        });

        return formattedString;
    }


    // Mantenemos los mismos métodos de eventos
    private async void CerrarModal(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void IrAOfertaClicked(object sender, EventArgs e)
    {
        NavigationService.LocationToGo = _ubicacionPromocion;

        bool answer = await DisplayAlert("Indicaciones", "¿Indicaciones para ir a la promoción?", "Si", "No");

        if (answer)
        {
            await NavigationService.AbrirNavegacion(_ubicacionPromocion);
        }

        await Shell.Current.GoToAsync("..");

        await Shell.Current.GoToAsync("//Map");
    }


    private async void OnHeartTapped(object sender, EventArgs e)
    {
        var accessType = Connectivity.Current.NetworkAccess;

        if (accessType != NetworkAccess.Internet)
        {
            await DisplayAlert("Sin conexión", "Necesitas conexión a internet para guardar promociones.", "OK");
            return;
        }

        var promocion = BindingContext as Promocion;
        if (promocion == null) return;

        _esFavorita = !_esFavorita;
        await FavoritosService.AlternarFavoritoAsync(promocion);
        ActualizarCorazon();

        if (_esFavorita)
        {
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(1100);
            HeartAnimation.IsAnimationEnabled = true;
        }
        else
        {
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(0);
            HeartAnimation.IsAnimationEnabled = false;
        }
    }



    private void ActualizarCorazon()
    {
        HeartAnimation.Progress = _esFavorita ? TimeSpan.FromMilliseconds(1000) : TimeSpan.FromMilliseconds(2000);
        HeartAnimation.IsAnimationEnabled = _esFavorita;
    }


}

