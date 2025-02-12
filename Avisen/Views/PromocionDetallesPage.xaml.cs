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
            PromocionesLabel.Text = ObtenerDetallesPromociones(negocio);
            PromocionImagen.Source = promocion.ImagenUrl;
        }
        else
        {
            PromocionesLabel.Text = "No hay promociones disponibles.";
            PromocionImagen.IsVisible = false;
        }
    }

    private string ObtenerDetallesPromociones(Negocio negocio)
    {
        string mensaje = "";
        foreach (var promocion in negocio.Promociones)
        {
            mensaje += $"**{promocion.Nombre}**\n";
            mensaje += $"{promocion.Descripcion}\n";
            mensaje += $"Precio: {(promocion.Precio == 0 ? "Oferta especial" : $"${promocion.Precio}")}\n";
            mensaje += $"Vigencia: {promocion.Vigencia.ToShortDateString()}\n\n";
        }
        return mensaje;
    }

    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
