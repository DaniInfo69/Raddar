using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    private Location userLocation;
    private List<Negocio> negocios;
    private NegocioService negocioService;
    private bool isUpdatingLocation;
    private int updateDelayFrequency = 1000;

    public static List<Negocio> OfertasVistas { get; private set; } = new List<Negocio>();
    public static List<Negocio> OfertasActuales = new List<Negocio>();

    public Map()
    {
        InitializeComponent();
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
        negocioService = new NegocioService();
        LoadData();
        StartLocationUpdates();
    }

    private double _UpdateFrequency;
    public double UpdateFrequency
    {
        get => _UpdateFrequency;
        set
        {
            _UpdateFrequency = value;
            OnPropertyChanged();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
    }

    private async void StartLocationUpdates()
    {
        isUpdatingLocation = true;

        while (isUpdatingLocation)
        {
            var lastLoadDataTimeString = await SecureStorage.GetAsync("lastLoadDataTime");
            DateTime lastLoadDataTime;
            int frequency = updateDelayFrequency * Convert.ToInt32(UpdateFrequency);

            if (DateTime.TryParse(lastLoadDataTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastLoadDataTime))
            {
                var timeSinceLastLoad = DateTime.Now - lastLoadDataTime;
                if (timeSinceLastLoad.TotalSeconds >= 60)
                {
                    LoadData();
                }
            }

            await UpdateUserLocationAsync();
            await Task.Delay(frequency);
        }
    }

    private async Task UpdateUserLocationAsync()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best))
                ?? await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                userLocation = new Location(location.Latitude, location.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromMiles(0.5)));
                CheckForPromotions();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al obtener la ubicación: {ex.Message}", "OK");
        }
    }

    //protected override void OnDisappearing()
    //{
      //  base.OnDisappearing();
       // isUpdatingLocation = false;
    //}

    private async void LoadData()
    {
        try
        {
            negocios = await negocioService.ObtenerNegociosAsync();
            var currentTime = DateTime.Now.ToString("o");
            await SecureStorage.SetAsync("lastLoadDataTime", currentTime);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
    }

    private void CheckForPromotions()
    {
        if (userLocation == null)
        {
            DisplayAlert("GPS no disponible", "No se puede verificar promociones porque el GPS no está activado o no se pudo obtener la ubicación.", "OK");
            return;
        }

        var negociosEnRango = new List<Negocio>();

        foreach (var negocio in negocios)
        {
            var distance = userLocation.CalculateDistance(negocio.Ubicacion, DistanceUnits.Kilometers);
            if (distance <= 0.1)
            {
                if (!map.Pins.Any(pin => pin.Label == negocio.Nombre))
                {
                    ShowPromotionAlert(negocio);
                }
                if (!OfertasActuales.Contains(negocio))
                {
                    OfertasActuales.Add(negocio);
                }
                negociosEnRango.Add(negocio);
            }
            else
            {
                var pinToRemove = map.Pins.FirstOrDefault(pin => pin.Label == negocio.Nombre);
                if (pinToRemove != null)
                {
                    map.Pins.Remove(pinToRemove);
                }
            }

            Debug.WriteLine("Ejecutando CheckForPromotions...");

            if (userLocation == null)
            {
                Debug.WriteLine("Ubicación del usuario es NULL. No se pueden verificar promociones.");
                return;
            }

            Debug.WriteLine($"Ubicación actual: {userLocation.Latitude}, {userLocation.Longitude}");
        }

        // Remover ofertas que ya no están en rango
        var ofertasFueraDeRango = OfertasActuales.Except(negociosEnRango).ToList();
        foreach (var oferta in ofertasFueraDeRango)
        {
            OfertasActuales.Remove(oferta);
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
