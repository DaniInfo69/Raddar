using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    private Location userLocation;
    private List<Negocio> negocios;
    private NegocioService negocioService;
    private bool isUpdatingLocation;

    public static List<Negocio> OfertasVistas { get; private set; } = new List<Negocio>();

    public Map()
    {
        InitializeComponent();
        negocioService = new NegocioService();
        CargarNegocios();
        MoveToUserLocation();
        map.PropertyChanged += OnMapPropertyChanged;
        StartLocationUpdates();
    }

    private async void StartLocationUpdates()
    {
        isUpdatingLocation = true;
        while (isUpdatingLocation)
        {
            await GetUserLocationAsync();
            await Task.Delay(30000);
        }
    }

    private async Task GetUserLocationAsync()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));
            if (location != null)
            {
                userLocation = new Location(location.Latitude, location.Longitude);
                CheckForPromotions();
            }
        }
        catch (FeatureNotEnabledException)
        {
            await DisplayAlert("Ubicación desactivada", "Por favor activa el GPS para continuar.", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permiso de ubicación denegado", "Por favor concede permisos de ubicación para continuar.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al obtener la ubicación: {ex.Message}", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        isUpdatingLocation = false;
    }

    private async void CargarNegocios()
    {
        negocios = await negocioService.ObtenerNegociosAsync();
        CheckForPromotions();
    }

    private async void MoveToUserLocation()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location != null)
            {
                userLocation = new Location(location.Latitude, location.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromMiles(0.5)));
                CheckForPromotions();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la ubicación: {ex.Message}");
        }
    }

    private void OnMapPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(map.IsShowingUser) && map.IsShowingUser)
        {
            MoveToUserLocation();
        }
    }

    private void CheckForPromotions()
    {
        if (userLocation == null)
        {
            DisplayAlert("GPS no disponible", "No se puede verificar promociones porque el GPS no está activado o no se pudo obtener la ubicación.", "OK");
            return;
        }

        foreach (var negocio in negocios)
        {
            var distance = userLocation.CalculateDistance(negocio.Ubicacion, DistanceUnits.Kilometers);
            if (distance <= 0.1)
            {
                if (!map.Pins.Any(pin => pin.Label == negocio.Nombre))
                {
                    ShowPromotionAlert(negocio);
                }
            }
            else
            {
                var pinToRemove = map.Pins.FirstOrDefault(pin => pin.Label == negocio.Nombre);
                if (pinToRemove != null)
                {
                    map.Pins.Remove(pinToRemove);
                }
            }
        }
    }

    private void ShowPromotionAlert(Negocio negocio)
    {
        if (!OfertasVistas.Any(o => o.Nombre == negocio.Nombre))
        {
            OfertasVistas.Add(negocio);
        }

        var promotionPin = new Pin
        {
            Label = negocio.Nombre,
            Address = "¡Oferta!",
            Type = PinType.Place,
            Location = negocio.Ubicacion
        };

        promotionPin.MarkerClicked += (s, e) => DisplayPromotionDetails(negocio);
        map.Pins.Add(promotionPin);
    }

    private async void DisplayPromotionDetails(Negocio negocio)
    {
        var detallesPage = new PromocionDetallesPage(negocio);
        await Navigation.PushModalAsync(detallesPage);
    }
}
