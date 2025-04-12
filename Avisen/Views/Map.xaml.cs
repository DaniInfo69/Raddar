using Avisen.Models;
using Avisen.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    private Location userLocation;
    private List<Matriz> negocios;
    private readonly NegocioService negocioService;
    private bool isUpdatingLocation;
    private int updateDelayFrequency = 1000;


    public static List<Matriz> OfertasVistas { get; private set; } = new List<Matriz>();
    public static List<Matriz> OfertasActuales = new List<Matriz>();
    public static List<Matriz> TodasLasOfertas = new List<Matriz>();



    public Map(NegocioService negocioService)
    {
        InitializeComponent();
        this.negocioService = negocioService;
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);

        // —————— BLOQUE “Ir a la oferta” ——————
        if (NavigationService.LocationToGo is Location loc)
        {
            // Limpia cualquier pin anterior
            map.Pins.Clear();

            // Agrega el pin temporal
            map.Pins.Add(new Pin
            {
                Label = "Oferta seleccionada",
                Location = loc,
                Type = PinType.Place
            });

            // Centra el mapa
            map.MoveToRegion(MapSpan.FromCenterAndRadius(loc, Distance.FromMeters(200)));

            // Resetea para que no lo ejecute de nuevo
            NavigationService.LocationToGo = null;
        }

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
    //map.Pins.Clear();
    //}


    private async void LoadData()
    {
        try
        {
            negocios = await negocioService.ObtenerMatricesConPromocionesAsync();
            var currentTime = DateTime.Now.ToString("o");
            await SecureStorage.SetAsync("lastLoadDataTime", currentTime);
            TodasLasOfertas.Clear();
            TodasLasOfertas.AddRange(negocios);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");

            Console.WriteLine("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
    }

    private void CheckForPromotions()
    {
        if (userLocation == null)
        {
            DisplayAlert("GPS no disponible", "No se puede verificar promociones porque el GPS no está activado o no se pudo obtener la ubicación.", "OK");
            return;
        }

        var negociosEnRango = new List<Matriz>();

        foreach (var negocio in negocios)
        {
            var distance = userLocation.CalculateDistance(negocio.Location, DistanceUnits.Kilometers);

            if (distance <= 0.1)
            {
                if (!map.Pins.Any(pin => pin.Label == negocio.Nombre))
                {
                    Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.2));

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

    private void ShowPromotionAlert(Matriz negocio)
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
            Location = negocio.Location
        };

        promotionPin.MarkerClicked += (s, e) => DisplayPromotionDetails(negocio);
        map.Pins.Add(promotionPin);
    }

    private async void DisplayPromotionDetails(Matriz negocio)
    {
        if (negocio?.Promociones?.Any() == true)
        {
            // Mostrar todas las promociones en un carrusel o lista seleccionable
            var action = await DisplayActionSheet(
                "Selecciona una promoción",
                "Cancelar",
                null,
                negocio.Promociones.Select(p => p.Nombre).ToArray());

            if (action != "Cancelar" && action != null)
            {
                var promocionSeleccionada = negocio.Promociones.FirstOrDefault(p => p.Nombre == action);
                if (promocionSeleccionada != null)
                {
                    var detallesPage = new PromocionDetallesPage(promocionSeleccionada, default);
                    await Navigation.PushModalAsync(detallesPage);
                }
            }
        }
        else
        {
            await DisplayAlert("Sin promociones", "Este negocio no tiene promociones disponibles", "OK");
        }
    }
}
