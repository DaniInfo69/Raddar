using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    private Location userLocation;
    private List<Negocio> negocios;
    private NegocioService negocioService;
    private bool isUpdatingLocation;
    private int updateDelayFrequency = 1000;

    public static List<Negocio> OfertasVistas { get; private set; } = new List<Negocio>();

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
        // Actualizar UpdateFrequency cuando se accede a la página
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);


    }

    private async void StartLocationUpdates()
    {
        isUpdatingLocation = true;



        while (isUpdatingLocation)
        {
            // Leer el dato lastLoadDataTime desde SecureStorage
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

            await DisplayAlert("DelayUpdateFrequency", frequency.ToString(), "OK");
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
            await DisplayAlert("gps", "gps", "OK");
            if (location != null)
            {
                userLocation = new Location(location.Latitude, location.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromMiles(0.5)));
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

    private async void LoadData()
    {
        try
        {
            negocios = await negocioService.ObtenerNegociosAsync();
            await DisplayAlert("api", "api", "OK");
            // Guardar la hora en que LoadData se ejecuta correctamente en SecureStorage
            var currentTime = DateTime.Now.ToString("o"); // Formato de cadena ISO 8601
            await SecureStorage.SetAsync("lastLoadDataTime", currentTime);
            return;
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