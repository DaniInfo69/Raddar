namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    public PromocionDetallesPage(Negocio negocio)
    {
        InitializeComponent();

        // Mostrar los detalles del negocio
        NombreNegocioLabel.Text = negocio.Nombre;

        // Si hay promociones, mostrar la imagen de la primera promoción
        if (negocio.Promociones.Count > 0)
        {
            var promocion = negocio.Promociones[0]; // Tomamos la primera promoción
            PromocionesLabel.FormattedText = ObtenerDetallesPromociones(negocio);
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
                Text = $"Precio: {(promocion.Precio == 0 ? "Oferta especial" : $"${promocion.Precio} mxn")}\n",
                FontSize = 16,
                TextColor = Color.FromArgb("#19535F") // Color verde oscuro
            });

            // Vigencia
            formattedString.Spans.Add(new Span
            {
                Text = $"Vigencia: {promocion.Vigencia.ToShortDateString()}\n\n",
                FontSize = 16,
                TextColor = Color.FromArgb("#19535F") // Color verde oscuro
            });
        }

        return formattedString;
    }

    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}