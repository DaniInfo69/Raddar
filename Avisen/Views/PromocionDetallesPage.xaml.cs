using Avisen.Models;

namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    public PromocionDetallesPage(Promocion promocion)
    {
        InitializeComponent();

        // Configuramos el BindingContext con la promoción
        BindingContext = promocion;

        // Mostramos los detalles directamente desde el Binding
        PromocionesLabel.FormattedText = ObtenerDetallesPromocion(promocion);
        VigenciaLabel.Text = promocion.VigenciaInicio.ToShortDateString();
        VigenciaLabel2.Text = promocion.VigenciaFin.ToShortDateString();

        // Opcional: Si necesitas el nombre de la empresa, podrías pasarlo como parámetro adicional
        // NombreNegocioLabel.Text = nombreEmpresa;
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
        await DisplayAlert("Oferta", "Aquí puedes redirigir a la oferta específica.", "OK");
    }

    private void OnHeartTapped(object sender, EventArgs e)
    {
        if (HeartAnimation.IsAnimationEnabled)
        {
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(0);
            HeartAnimation.IsAnimationEnabled = false;
        }
        else
        {
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(1100);
            HeartAnimation.IsAnimationEnabled = true;
        }
    }
}