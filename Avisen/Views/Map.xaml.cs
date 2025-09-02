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
    private List<Tesoro> tesoros = new();

    private List<MapPin> _pins;
    public List<MapPin> Pins
    {
        get { return _pins; }
        set { _pins = value; OnPropertyChanged(); }
    }
    private HashSet<string> negociosAlertados = new();
    public static List<Negocio> OfertasVistas { get; private set; } = new List<Negocio>();
    public static List<Negocio> OfertasActuales = new List<Negocio>();

    private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API



    public Map(NegocioService negocioService)
    {
        InitializeComponent();
        Debug.WriteLine("[Map Page] Constructor: InitializeComponent completado.");

        this.negocioService = negocioService;
        LoadDataAsync();
        StartAndUpdateLocation();

        BindingContext = this;
        Debug.WriteLine("[Map Page] BindingContext asignado.");


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
                Icon = "pin_offer"
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
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best))
                               ?? await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
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

                gpsAlertShown = false;
                userLocation = new Location(location.Latitude, location.Longitude);

                // Control de frecuencia de recarga de datos
                var lastLoadDataTimeString = await SecureStorage.GetAsync("lastLoadDataTime");
                DateTime lastLoadDataTime;
                int frequency = Math.Max(updateDelayFrequency * Convert.ToInt32(UpdateFrequency), 5000); // mínimo 5s

                if (DateTime.TryParse(lastLoadDataTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastLoadDataTime))
                {
                    var timeSinceLastLoad = DateTime.Now - lastLoadDataTime;
                    if (timeSinceLastLoad.TotalMilliseconds >= frequency)
                    {
                        await LoadDataAsync();
                    }
                }
                else
                {
                    await LoadDataAsync();
                }

                // Centrado del mapa
                if (Preferences.Get("IsRecenter", false))
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                }
                else if (!hasCenteredMapOnce)
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                    hasCenteredMapOnce = true;
                }

                // Verificar promociones y tesoros
                if (negocios != null && negocios.Any())
                    CheckForPromotions();

                if (tesoros != null && tesoros.Any())
                    CheckForTreasures();
            }
            catch (FeatureNotEnabledException)
            {
                if (!gpsAlertShown)
                {
                    gpsAlertShown = true;
                    await DisplayAlert("GPS apagado", "Activa el GPS para usar el mapa.", "OK");
                }
            }
            catch (PermissionException)
            {
                if (!gpsAlertShown)
                {
                    gpsAlertShown = true;
                    await DisplayAlert("Permisos denegados", "La app no tiene permisos para la ubicación.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en StartAndUpdateLocation: {ex.Message}");
            }

            int frequency2 = Math.Max(updateDelayFrequency * Convert.ToInt32(UpdateFrequency), 5000); // mínimo 5s
            await Task.Delay(frequency2);
        }
    }



    private async Task LoadDataAsync()
    {
        try
        {
            negocios = await negocioService.ObtenerNegociosConPromocionesAsync();
            tesoros = await negocioService.ObtenerTesorosAsync();

            await SecureStorage.SetAsync("lastLoadDataTime", DateTime.Now.ToString("o"));
            Debug.WriteLine("Datos cargados correctamente.");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
            Debug.WriteLine($"Error al cargar datos: {ex.Message}");
        }
    }

    private void CheckForPromotions()
    {
        if (userLocation == null) return;

        var negociosEnRango = new List<Negocio>();

        foreach (var negocio in negocios)
        {
            if (negocio?.Ubicacion == null) continue;

            var distance = userLocation.CalculateDistance(negocio.Location, DistanceUnits.Kilometers);

            if (distance <= OfferDistance)
            {
                // Solo vibrar si no se ha vibrado antes por este negocio
                if (!negociosAlertados.Contains(negocio.Nombre))
                {
                    Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.1));
                    ShowPromotionAlert(negocio);
                    negociosAlertados.Add(negocio.Nombre);
                }

                if (!OfertasActuales.Contains(negocio))
                    OfertasActuales.Add(negocio);

                negociosEnRango.Add(negocio);
            }
            else
            {
                var pinToRemove = map.Pins.FirstOrDefault(pin => pin.Label == negocio.Nombre);
                if (pinToRemove != null)
                    map.Pins.Remove(pinToRemove);

                // Si sale del rango, permitir que vuelva a vibrar cuando entre de nuevo
                negociosAlertados.Remove(negocio.Nombre);
            }
        }

        // Remover ofertas que ya no están en rango
        var ofertasFueraDeRango = OfertasActuales.Except(negociosEnRango).ToList();
        foreach (var oferta in ofertasFueraDeRango)
            OfertasActuales.Remove(oferta);
    }

    private void ShowPromotionAlert(Negocio negocio)
    {
        if (!OfertasVistas.Any(o => o.Nombre == negocio.Nombre))
        {
            OfertasVistas.Add(negocio);
        }

        var promotionPin = new MapPin(p =>
        {
            Debug.WriteLine($"[Map Page] Click en promoción: {negocio.Nombre}");
            DisplayPromotionDetails(negocio);
        })
        {
            Id = Guid.NewGuid().ToString(),
            Position = negocio.Location,
            Icon = "pin_offer",
            Width = 200,  
            Height = 200
        };


        // Asegurar que Pins no es null
        if (Pins == null)
            Pins = new List<MapPin>();

        // Agregar el pin a la lista y forzar OnPropertyChanged
        Pins.Add(promotionPin);
        Pins = new List<MapPin>(Pins); // << Clave para refrescar el binding

        Debug.WriteLine($"[Map Page] Pin de oferta añadido: {negocio.Nombre} en {negocio.Location.Latitude},{negocio.Location.Longitude}");

        // Opcional: centrar el mapa en la ubicación de la oferta
        map.MoveToRegion(MapSpan.FromCenterAndRadius(negocio.Location, Distance.FromMeters(300)));
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


    private void CheckForTreasures()
    {
        if (userLocation == null)
        {
            Debug.WriteLine("GPS no disponible para verificar tesoros.");
            return;
        }

        foreach (var tesoro in tesoros)
        {
            if (string.IsNullOrEmpty(tesoro.ubicacion))
            {
                Debug.WriteLine($"Tesoro sin ubicación: {tesoro.nombre}");
                continue;
            }

            try
            {
                // Parsear coordenadas de la ubicación estilo "POINT(-103.4621 19.7045)"
                var coords = tesoro.ubicacion
                    .Replace("POINT(", "")
                    .Replace(")", "")
                    .Split(" ");

                double lng = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                double lat = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);

                var tesoroLocation = new Location(lat, lng);
                var distance = userLocation.CalculateDistance(tesoroLocation, DistanceUnits.Kilometers);

                if (distance <= OfferDistance) // mismo rango que promociones
                {
                    if (!map.Pins.Any(pin => pin.Label == tesoro.nombre))
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.2));
                        ShowTreasureAlert(tesoro, tesoroLocation);
                    }
                }
                else
                {
                    var pinToRemove = map.Pins.FirstOrDefault(pin => pin.Label == tesoro.nombre);
                    if (pinToRemove != null)
                    {
                        map.Pins.Remove(pinToRemove);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error procesando tesoro {tesoro.nombre}: {ex.Message}");
            }
        }
    }

    private void ShowTreasureAlert(Tesoro tesoro, Location tesoroLocation)
    {
        var treasurePin = new MapPin(p =>
        {
            Debug.WriteLine($"[Map Page] Click en Tesoro: {tesoro.nombre}");
            DisplayTreasureDetails(tesoro);
        })
        {
            Id = Guid.NewGuid().ToString(),
            Position = tesoroLocation,
            Icon = "pin_tesoro",
            Width = 100,
            Height = 100
        };


        if (Pins == null)
            Pins = new List<MapPin>();

        Pins.Add(treasurePin);
        Pins = new List<MapPin>(Pins); // refresca binding

        Debug.WriteLine($"[Map Page] Pin de tesoro añadido: {tesoro.nombre} en {tesoroLocation.Latitude},{tesoroLocation.Longitude}");
    }


    private async void DisplayTreasureDetails(Tesoro tesoro)
    {
        var action = await DisplayActionSheet(
            tesoro.nombre,
            "Cerrar",
            null,
            "Ver descripción",
            "Ver QR disponibles");

        if (action == "Ver descripción")
        {
            await DisplayAlert(tesoro.nombre, tesoro.descripcion, "OK");
        }
        else if (action == "Ver QR disponibles" && tesoro.qr.Any())
        {
            var qrInfo = string.Join("\n", tesoro.qr.Select(q => $"- Token: {q.token} (Expira: {q.fechaexpiracion})"));
            await DisplayAlert("QRs del Tesoro", qrInfo, "OK");
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