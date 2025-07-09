using Avisen.Models;
using Avisen.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;
using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    private Location userLocation;
    private List<Matriz> negocios;
    private readonly NegocioService negocioService;
    private bool isUpdatingLocation;
    private int updateDelayFrequency = 1000;
    private bool isAddingPin = false;
    private int UserId;
    private double? selectedLat = null;
    private double? selectedLng = null;



    public static List<Matriz> OfertasVistas { get; private set; } = new List<Matriz>();
    public static List<Matriz> OfertasActuales = new List<Matriz>();
    private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API



    public Map(NegocioService negocioService)
    {
        InitializeComponent();
        this.negocioService = negocioService;
        LoadData();
        StartAndUpdateLocation();
    }

    private bool _isRecenter;
    public bool IsRecenter
    {
        get => _isRecenter;
        set
        {
            _isRecenter = value;
        }
    }

    private double _UpdateFrequency;
    public double UpdateFrequency
    {
        get => _UpdateFrequency;
        set
        {
            _UpdateFrequency = value;
        }
    }

    private double _offerDistance;
    public double OfferDistance
    {
        get => _offerDistance;
        set
        {
            _offerDistance = value;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserDataAsync();
        IsRecenter = Preferences.Get("IsRecenter", false);
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
        OfferDistance = Preferences.Get("OfferDistance", 0.0);

        // —————— BLOQUE “Ir a la oferta” ——————
        if (NavigationService.LocationToGo is Location loc)
        {

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

    private async void StartAndUpdateLocation()
    {
        isUpdatingLocation = true;
        bool hasCenteredMapOnce = false;
        map.IsShowingUser = true;

        while (isUpdatingLocation)
        {
            try
            {
                Debug.WriteLine("Empieza ciclo.");
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best))
                    ?? await Geolocation.GetLastKnownLocationAsync();

                var lastLoadDataTimeString = await SecureStorage.GetAsync("lastLoadDataTime");
                DateTime lastLoadDataTime;
                int frequency = updateDelayFrequency * Convert.ToInt32(UpdateFrequency);

                if (DateTime.TryParse(lastLoadDataTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastLoadDataTime))
                {
                    var timeSinceLastLoad = DateTime.Now - lastLoadDataTime;
                    if (timeSinceLastLoad.TotalSeconds >= frequency / 1000.0)
                    {
                        LoadData();
                    }
                }

                if (location != null)
                {
                    Debug.WriteLine("Procesando ubicación...");
                    userLocation = new Location(location.Latitude, location.Longitude);

                    if (Preferences.Get("IsRecenter", false))
                    {
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                        Debug.WriteLine("Se mueve.");
                    }
                    else if (!hasCenteredMapOnce)
                    {
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                        hasCenteredMapOnce = true;
                        Debug.WriteLine("Se movio por primera vez.");
                    }

                    CheckForPromotions();
                }
                else
                {
                    Debug.WriteLine("No se obtuvo localización.");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al obtener la ubicación o cargar datos: {ex.Message}", "OK");
            }

            int waitTime = updateDelayFrequency * Convert.ToInt32(UpdateFrequency);
            await Task.Delay(waitTime);
        }
    }


    private async void LoadData()
    {
        try
        {
            negocios = await negocioService.ObtenerMatricesConPromocionesAsync();
            var currentTime = DateTime.Now.ToString("o");
            await SecureStorage.SetAsync("lastLoadDataTime", currentTime);
            Debug.WriteLine("Cargó Datos.");
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

            if (distance <= OfferDistance)
            {
                if (!map.Pins.Any(pin => pin.Label == negocio.Nombre))
                {
                    Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.1));

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

    private void OnAddPinClicked(object sender, EventArgs e)
    {
        
        if (!isAddingPin)
        {
            turnMode(false);
        }
        else
        {
            turnMode(true);
        }
    }

    private async void turnMode(bool decition)
    {
        if (!decition)
        {
            isAddingPin = true;

            AddPin.Text = "Cancelar";
            AddPin.TextColor = Color.FromArgb("#5f1919");
            AddPin.BackgroundColor = Color.FromArgb("#e7d1d1");

            if (AddPin.ImageSource is FontImageSource icon)
            {
                icon.Color = Color.FromArgb("#5f1919");
                icon.Glyph = IconFont.Cancel;
            }
            await DisplayAlert("Modo Pin", "Toca en el mapa para agregar un pin", "OK");
        }
        else
        {
            isAddingPin = false;

            AddPin.Text = "Agregar Pin";
            AddPin.TextColor = Color.FromArgb("#19535F");
            AddPin.BackgroundColor = Color.FromArgb("#d1e7dd");

            if (AddPin.ImageSource is FontImageSource icon)
            {
                icon.Color = Color.FromArgb("#19535F");
                icon.Glyph = IconFont.Add_location;
            }
            await DisplayAlert("Modo Normal", "Ya puedes mover el mapa", "OK");
        }
    }


    // Evento cuando se toca el mapa (tapOverlay)
    private async void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        if (!isAddingPin) return;

        var location = e.Location;

        var pin = new Microsoft.Maui.Controls.Maps.Pin
        {
            Label = "Ubicación seleccionada",
            Location = location,
            Type = PinType.Place
        };

        map.Pins.Clear(); // Opcional: limpiar otros pins
        map.Pins.Add(pin);

        selectedLat = Math.Round(location.Latitude, 15);
        selectedLng = Math.Round(location.Longitude, 14);

        await DisplayAlert("Coordenadas",
            $"Lat: {selectedLat}, Lng: {selectedLng}",
            "OK");

        isAddingPin = false;
        AddNewFavoriteZone.IsVisible = true;
        await PopupFrame.FadeTo(1, 250, Easing.CubicInOut);
        await PopupFrame.ScaleTo(1, 250, Easing.CubicOut);
    }


    private async void buttonSave_Clicked(object sender, EventArgs e)
    {
        try
        {
            Console.WriteLine($"Nomrbre: {NameEntry.Text} lat: {selectedLat} lng: {selectedLng}, id: {UserId}");
            var jsonRequest = new
            {
                nombre = NameEntry.Text,
                ubicacion = new
                {
                    lat = selectedLat,
                    lng = selectedLng
                },
                cliente_idcliente = UserId
            };


            var response = await apiService.PostAsync("favorito", jsonRequest);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

                await PopupFrame.FadeTo(1, 250, Easing.CubicInOut);
                await PopupFrame.ScaleTo(1, 250, Easing.CubicOut);
                AddNewFavoriteZone.IsVisible = false;
                NameEntry.Text = string.Empty;
                turnMode(true);
            }
            else
                Console.WriteLine("Error");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async void buttonCancel_Clicked(object sender, EventArgs e)
    {
        await PopupFrame.FadeTo(1, 250, Easing.CubicInOut);
        await PopupFrame.ScaleTo(1, 250, Easing.CubicOut);
        AddNewFavoriteZone.IsVisible = false;
        NameEntry.Text = string.Empty;
        turnMode(true);
    }

    private async void LoadUserDataAsync()
    {
        try
        {
            var userDataJson = await SecureStorage.GetAsync("UserData");

            if (!string.IsNullOrEmpty(userDataJson))
            {
                Console.WriteLine($"UserData JSON: {userDataJson}");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var userData = JsonSerializer.Deserialize<UserData>(userDataJson, options);

                if (userData != null)
                {
                    UserId = Convert.ToInt32(userData.IdCliente);
                }
                else
                {
                    Console.WriteLine("Error al descerializar datos");
                }
            }
            else
            {
                Console.WriteLine("Sin informacion del usuario");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

}

public static class MapExtensions
{
    public static Location? ToLocation(this MapSpan region, Point point, double mapWidth, double mapHeight)
    {
        var latDegreesPerPixel = region.LatitudeDegrees / mapHeight;
        var lonDegreesPerPixel = region.LongitudeDegrees / mapWidth;

        var lat = region.Center.Latitude + ((mapHeight / 2 - point.Y) * latDegreesPerPixel);
        var lon = region.Center.Longitude + ((point.X - mapWidth / 2) * lonDegreesPerPixel);

        return new Location(lat, lon);
    }
}