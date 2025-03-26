using Avisen.Models;

namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    public PromocionDetallesPage(Negocio negocio)
    {
        InitializeComponent();

        // Mostrar los detalles del negocio
        NombreNegocioLabel.Text = negocio.Nombre;
        DescripcionNegocioLabel.Text = negocio.Descripcion;

        // Si hay promociones, mostrar la imagen de la primera promoción
        if (negocio.Promociones.Count > 0)
        {
            var promocion = negocio.Promociones[0]; // Tomamos la primera promoción
            PromocionesLabel.FormattedText = ObtenerDetallesPromociones(negocio);
            VigenciaLabel.FormattedText = ObtenerVigenciaI(negocio);
            VigenciaLabel2.FormattedText = ObtenerVigenciaF(negocio);
            PromocionImagen.Source = promocion.ImagenUrl;
        }
        else
        {
            PromocionesLabel.Text = "No hay promociones disponibles.";
            PromocionImagen.IsVisible = false;
        }
    }

    private FormattedString ObtenerDetallesPromociones(Negocio negocio)
    {
        var formattedString = new FormattedString();

        foreach (var promocion in negocio.Promociones)
        {
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
                TextColor = Color.FromArgb("#602020") // Color rojo oscuro
            });

            // Precio
            formattedString.Spans.Add(new Span
            {
                Text = $"Precio: {(promocion.Precio == null ? "Oferta especial" : $"${promocion.Precio} mxn")}\n\n",
                FontSize = 16,
                TextColor = Color.FromArgb("#19535F") // Color verde oscuro
            });

            // Tipo de promoción
            formattedString.Spans.Add(new Span
            {
                Text = $"Tipo de promoción: {promocion.Tipo}\n",
                FontSize = 16,
                TextColor = Color.FromArgb("#19535F") // Color verde oscuro
            });
        }

        return formattedString;
    }

    private FormattedString ObtenerVigenciaI(Negocio negocio)
    {
        var formattedString = new FormattedString();

        foreach (var promocion in negocio.Promociones)
        {

            formattedString.Spans.Add(new Span
            {
                Text = promocion.VigenciaInicio.ToShortDateString(),
                FontSize = 16,
                TextColor = Color.FromArgb("#19535F")
            });
        }

        return formattedString;
    }

    private FormattedString ObtenerVigenciaF(Negocio negocio)
    {
        var formattedString = new FormattedString();

        foreach (var promocion in negocio.Promociones)
        {

            formattedString.Spans.Add(new Span
            {
                Text = promocion.VigenciaFin.ToShortDateString(),
                FontSize = 16,
                TextColor = Color.FromArgb("#19535F")
            });
        }

        return formattedString;
    }


    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void IrAOfertaClicked(object sender, EventArgs e)
    {
        // Aquí podrías redirigir a una URL, abrir un navegador o llevar a una nueva página dentro de la app
        await DisplayAlert("Oferta", "Aquí puedes redirigir a la oferta específica.", "OK");
    }

    private void OnHeartTapped(object sender, EventArgs e)
    {
        if (HeartAnimation.IsAnimationEnabled)
        {
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(0); // Reinicia la animación correctamente
            HeartAnimation.IsAnimationEnabled = false;
        }
        else
        {
            HeartAnimation.Progress = TimeSpan.FromMilliseconds(1100);
            HeartAnimation.IsAnimationEnabled = true;
        }
    }

}