namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    public PromocionDetallesPage(Negocio negocio)
    {
        InitializeComponent();

        // Mostrar los detalles del negocio
        NombreNegocioLabel.Text = negocio.Nombre;
        PromocionesLabel.Text = ObtenerDetallesPromociones(negocio);
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
        await Navigation.PopModalAsync(); // Cerrar la página modal
    }
}