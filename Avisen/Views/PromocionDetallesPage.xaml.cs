using Avisen.Models;
using Avisen.Services;
using Microsoft.Maui.Controls;
using QRCoder;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Avisen.Views;

public partial class PromocionDetallesPage : ContentPage
{
    private readonly Location _ubicacionPromocion;
    private bool _esFavorita;
    private bool _hasQr = false;
    private bool _qrVisible = false;
    private string _qrToken = string.Empty;
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly ApiService _apiService = new ApiService();
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
        VigenciaLabel.Text = promocion.VigenciaInicio?.ToShortDateString() ?? "Sin fecha";
        VigenciaLabel2.Text = promocion.VigenciaFin?.ToShortDateString() ?? "Sin fecha";

        AplicarEstilosTipo(promocion.Tipo);

        _esFavorita = FavoritosService.EsFavorita(promocion);
        ActualizarCorazon();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasQr = false;
        _qrVisible = false;
        QrContainer.IsVisible = false;
        LblNoQr.IsVisible = false;
        BtnToggleQr.IsEnabled = true;
    }


    private async Task<int> GetIdClienteAsync()
    {
        try
        {
            // Recuperar JSON del usuario almacenado
            var userDataJson = await SecureStorage.GetAsync("UserData");
            if (string.IsNullOrEmpty(userDataJson))
                return 0;

            var userData = JsonSerializer.Deserialize<UserData>(
                userDataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return userData?.IdCliente ?? 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al obtener IdCliente: {ex.Message}");
            return 0;
        }
    }

    private async void BtnToggleQr_Clicked(object sender, EventArgs e)
        => await HandleQrAction();

    private async void QrBorder_Tapped(object sender, EventArgs e)
        => await HandleQrAction();

    private async Task HandleQrAction()
    {
        if (_qrVisible)
        {
            _qrVisible = false;
            QrContainer.IsVisible = false;
            return;
        }

        if (BindingContext is not Promocion promo) return;

        bool confirm = await DisplayAlert("Reclamar Promoción",
            "¿Quieres reclamar la promoción?", "Sí", "No");

        if (!confirm) return;

        try
        {
            int idCliente = await GetIdClienteAsync();
            string tokenPromocion = promo.qrs?.FirstOrDefault()?.token ?? string.Empty;

            string qrToken = await _apiService.ReclamarPromocionAsync(promo.idpromocion, idCliente, tokenPromocion);

            await GenerarImagenQr(qrToken);
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlert("Aviso", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo generar el QR: {ex.Message}", "OK");
        }
    }

    private async Task GenerarImagenQr(string token)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
        var pngWriter = new PngByteQRCode(qrData);
        byte[] pngBytes = pngWriter.GetGraphic(20);

        QrImage.Source = ImageSource.FromStream(() => new MemoryStream(pngBytes));
        _hasQr = true;
        _qrVisible = true;
        QrContainer.IsVisible = true;
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

        // SOLUCIÓN: Usar solo una navegación
        await Shell.Current.GoToAsync("//Map", true); // true = animated
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
            TipoFrame.Background = Color.FromArgb("#FFF0F0");
            TipoFrame.Stroke = Color.FromArgb("#F4C2C2");
            TipoBadge.TextColor = Color.FromArgb("#C94A4A");
            return;
        }

        var lower = tipo.ToLowerInvariant();

        if (lower.Contains("venta") || lower.Contains("venta") || lower.Contains("sell"))
        {
            // rojo (venta)
            TipoFrame.Background = Color.FromArgb("#FDECEA"); // suave
            TipoFrame.Stroke = Color.FromArgb("#F5B4B4");
            TipoBadge.TextColor = Color.FromArgb("#C94A4A");
        }
        else if (lower.Contains("informativa") || lower.Contains("informativo") || lower.Contains("info"))
        {
            // naranja (informativa)
            TipoFrame.Background = Color.FromArgb("#FFF6EA"); // suave naranja
            TipoFrame.Stroke = Color.FromArgb("#F8D6A6");
            TipoBadge.TextColor = Color.FromArgb("#D97706");
        }
        else
        {
            // estilo neutro
            TipoFrame.Background = Color.FromArgb("#F0F7F7");
            TipoFrame.Stroke = Color.FromArgb("#D0EDEA");
            TipoBadge.TextColor = Color.FromArgb("#19535F");
        }
    }
}
