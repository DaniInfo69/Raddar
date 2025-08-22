using Avisen.Models;
using Avisen.Services;
using Microsoft.Maui.Controls;
using System;
using System.IO;

namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    private readonly Location _ubicacionPromocion;
    private bool _esFavorita;
    private bool _hasQr = false;
    private bool _qrVisible = false;

    public PromocionDetallesPage(Promocion promocion, Location ubicacionPromocion)
    {
        InitializeComponent();

        BindingContext = promocion;
        _ubicacionPromocion = ubicacionPromocion;

        // Rellenar textos
        TituloPromocion.Text = promocion.Nombre ?? "Sin nombre";
        Subtitulo.Text = promocion.NombreEmpresa ?? string.Empty;
        DescripcionText.Text = promocion.Descripcion ?? string.Empty;

        PrecioLabel.Text = string.IsNullOrWhiteSpace(promocion.Precio) ? "Oferta especial" : $"${promocion.Precio} mxn";
        TipoBadge.Text = promocion.Tipo ?? "No especificado";

        VigenciaLabel.Text = promocion.VigenciaInicio.HasValue ? promocion.VigenciaInicio.Value.ToShortDateString() : "Sin fecha";
        VigenciaLabel2.Text = promocion.VigenciaFin.HasValue ? promocion.VigenciaFin.Value.ToShortDateString() : "Sin fecha";

        // Ajustamos estilos del badge según tipo
        AplicarEstilosTipo(promocion.Tipo);

        _esFavorita = FavoritosService.EsFavorita(promocion);
        ActualizarCorazon();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Intentar cargar QR y configurar disponibilidad
        _hasQr = false;
        LblNoQr.IsVisible = false;
        BtnToggleQr.IsEnabled = true;
        QrContainer.IsVisible = false;
        _qrVisible = false;


        if (BindingContext is Promocion promo
            && promo.qrs != null
            && promo.qrs.Count > 0
            && !string.IsNullOrEmpty(promo.qrs[0].imageBase64))
        {
            try
            {
                var base64Data = promo.qrs[0].imageBase64;
                var commaIndex = base64Data.IndexOf(',');
                if (commaIndex >= 0)
                {
                    base64Data = base64Data.Substring(commaIndex + 1);
                }

                byte[] imageBytes = Convert.FromBase64String(base64Data);
                QrImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                _hasQr = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar QR: {ex.Message}");
                _hasQr = false;
            }
        }

        // Actualizamos los controles del QR
        if (!_hasQr)
        {
            BtnToggleQr.IsEnabled = false;
            LblNoQr.IsVisible = true;
        }
        else
        {
            BtnToggleQr.IsEnabled = true;
            LblNoQr.IsVisible = false;
        }
    }

    private void BtnToggleQr_Clicked(object sender, EventArgs e)
    {
        ToggleQr();
    }

    private void QrBorder_Tapped(object sender, EventArgs e)
    {
        ToggleQr();
    }

    private void ToggleQr()
    {
        if (!_hasQr)
        {
            DisplayAlert("QR", "No hay QR disponible para esta promoción.", "OK");
            return;
        }

        _qrVisible = !_qrVisible;
        QrContainer.IsVisible = _qrVisible;

    }

    // Mantén tus métodos existentes
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

    // Ajusta colores y estilos del badge según tipo de promoción
    private void AplicarEstilosTipo(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            // Estilo por defecto
            TipoFrame.BackgroundColor = Color.FromArgb("#FFF0F0");
            TipoFrame.BorderColor = Color.FromArgb("#F4C2C2");
            TipoBadge.TextColor = Color.FromArgb("#C94A4A");
            return;
        }

        var lower = tipo.ToLowerInvariant();

        if (lower.Contains("venta") || lower.Contains("venta") || lower.Contains("sell"))
        {
            // rojo (venta)
            TipoFrame.BackgroundColor = Color.FromArgb("#FDECEA"); // suave
            TipoFrame.BorderColor = Color.FromArgb("#F5B4B4");
            TipoBadge.TextColor = Color.FromArgb("#C94A4A");
        }
        else if (lower.Contains("informativa") || lower.Contains("informativo") || lower.Contains("info"))
        {
            // naranja (informativa)
            TipoFrame.BackgroundColor = Color.FromArgb("#FFF6EA"); // suave naranja
            TipoFrame.BorderColor = Color.FromArgb("#F8D6A6");
            TipoBadge.TextColor = Color.FromArgb("#D97706");
        }
        else
        {
            // estilo neutro
            TipoFrame.BackgroundColor = Color.FromArgb("#F0F7F7");
            TipoFrame.BorderColor = Color.FromArgb("#D0EDEA");
            TipoBadge.TextColor = Color.FromArgb("#19535F");
        }
    }
}
