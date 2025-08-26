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
    private List<Negocio> negocios;
    private readonly NegocioService negocioService;
    private bool isUpdatingLocation;
    private int updateDelayFrequency = 1000;
    private bool isAddingPin = false;
    private int UserId;
    private double? selectedLat = null;
    private double? selectedLng = null;

    private List<MapPin> _pins;
    public List<MapPin> Pins
    {
        get { return _pins; }
        set { _pins = value; OnPropertyChanged(); }
    }

    public static List<Negocio> OfertasVistas { get; private set; } = new List<Negocio>();
    public static List<Negocio> OfertasActuales = new List<Negocio>();

    private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API



    public Map(NegocioService negocioService)
    {
        InitializeComponent();
        Debug.WriteLine("[Map Page] Constructor: InitializeComponent completado.");

        this.negocioService = negocioService;
        LoadData();
        StartAndUpdateLocation();

        BindingContext = this;
        Debug.WriteLine("[Map Page] BindingContext asignado.");

        Pins = new List<MapPin>()
    {
        new MapPin(MapPinClicked)
        {
            Id = Guid.NewGuid().ToString(),
            Position = new Location(19.879956945376524, -103.59449397593787),
            Icon = "pin2"
        }
    };
        Debug.WriteLine($"[Map Page] Pins inicializada con {Pins.Count} elemento(s).");
    }


    private void MapPinClicked(MapPin pin)
    {
        Debug.WriteLine($"Pin clicked: {pin.Id}");
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
        Debug.WriteLine("[Map Page] OnAppearing llamado.");

        LoadUserDataAsync();
        IsRecenter = Preferences.Get("IsRecenter", false);
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
        OfferDistance = Preferences.Get("OfferDistance", 0.0);

        Debug.WriteLine($"[Map Page] Estado inicial: Pins == null? {Pins == null}");

        if (NavigationService.LocationToGo is Location loc)
        {
            Debug.WriteLine($"[Map Page] NavigationService.LocationToGo encontrada en {loc.Latitude},{loc.Longitude}");

            // Evitar manipular map.CustomPins directamente (no disparará la actualización en el handler).
            // En su lugar actualizamos la propiedad 'Pins' y la reasignamos para forzar el cambio de binding.
            var newPin = new MapPin(p => { /* acción al clicar */ })
            {
                Id = Guid.NewGuid().ToString(),
                Position = new Location(loc),
                Icon = "pin2"
            };

            if (Pins == null)
            {
                Debug.WriteLine("[Map Page] Pins estaba null. Creando nueva lista con el pin.");
                Pins = new List<MapPin>() { newPin };
            }
            else
            {
                Debug.WriteLine($"[Map Page] Pins tenía {Pins.Count} items. Añadiendo y reasignando lista para disparar OnPropertyChanged.");
                Pins.Add(newPin);
                // Reasignar para disparar OnPropertyChanged y que el binding trigue el mapper
                Pins = new List<MapPin>(Pins);
            }

            // También para asegurar que el control en XAML se actualice, puedes asignar explícitamente:
            try
            {
                Debug.WriteLine($"[Map Page] Asignando map.CustomPins = Pins; map control: {(map == null ? "null" : "ok")}");
                map.CustomPins = Pins;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Map Page] Error al asignar map.CustomPins: " + ex);
            }

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
        bool gpsAlertShown = false;

        while (isUpdatingLocation)
        {
            try
            {
                Debug.WriteLine("Empieza ciclo.");

                // Intentar obtener ubicación actual
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best))
                    ?? await Geolocation.GetLastKnownLocationAsync();

                // Si no hay ubicación
                if (location == null)
                {
                    Debug.WriteLine("Ubicación no disponible.");
                    if (!gpsAlertShown)
                    {
                        gpsAlertShown = true;
                        await DisplayAlert("GPS no disponible",
                            "No se pudo obtener tu ubicación. Verifica que el GPS esté encendido y que la app tenga permisos.",
                            "OK");
                    }
                    await Task.Delay(2000);
                    continue;
                }

                gpsAlertShown = false; // Se resetea si ya obtuvimos ubicación
                userLocation = new Location(location.Latitude, location.Longitude);

                // Cargar datos si ya pasó el tiempo definido
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
                else
                {
                    LoadData();
                }

                // Centrado del mapa
                if (Preferences.Get("IsRecenter", false))
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                    Debug.WriteLine("Mapa centrado por IsRecenter.");
                }
                else if (!hasCenteredMapOnce)
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                    hasCenteredMapOnce = true;
                    Debug.WriteLine("Mapa centrado la primera vez.");
                }

                // Verificar promociones solo si hay datos
                if (negocios != null && negocios.Any())
                {
                    CheckForPromotions();
                }
                else
                {
                    Debug.WriteLine("No hay negocios cargados para verificar promociones.");
                }
            }
            catch (FeatureNotEnabledException)
            {
                if (!gpsAlertShown)
                {
                    gpsAlertShown = true;
                    await DisplayAlert("GPS apagado",
                        "Por favor, activa el GPS para usar el mapa.",
                        "OK");
                }
            }
            catch (PermissionException)
            {
                if (!gpsAlertShown)
                {
                    gpsAlertShown = true;
                    await DisplayAlert("Permisos denegados",
                        "La aplicación no tiene permisos para acceder a tu ubicación. Ve a ajustes y actívalos.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inesperado en StartAndUpdateLocation: {ex.Message}");
            }

            int waitTime = updateDelayFrequency * Convert.ToInt32(UpdateFrequency);
            await Task.Delay(waitTime > 0 ? waitTime : 1000); // Valor mínimo para evitar bucle rápido
        }
    }



    private async void LoadData()
    {
        try
        {
            negocios = await negocioService.ObtenerNegociosConPromocionesAsync();
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

        var negociosEnRango = new List<Negocio>();

        foreach (var negocio in negocios)
        {
            if (negocio?.Ubicacion == null)
            {
                Debug.WriteLine($"Negocio sin ubicación: {negocio?.Nombre}");
                Console.WriteLine($"Ubicación {negocio?.Ubicacion} en:{negocio?.Nombre} ");
                continue;
            }

            var distance = userLocation.CalculateDistance(negocio.Location, DistanceUnits.Kilometers);
            Console.WriteLine($"Ubicación {negocio?.Location} en:{negocio?.Nombre} ");


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
            Location = negocio.Location
        };

        promotionPin.MarkerClicked += (s, e) => DisplayPromotionDetails(negocio);
        map.Pins.Add(promotionPin);
    }


    private async void DisplayPromotionDetails(Negocio negocio)
    {
        if (negocio?.Promociones?.Any() == true)
        {
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
            await DisplayAlert("Sin promociones", "Esta sucursal no tiene promociones disponibles", "OK");
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