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
    private double _lastKnownOfferDistance = 0;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("[Map Page] OnAppearing llamado.");

        //  LIMPIAR TODOS LOS PINS ANTES DE CARGAR NUEVOS
        CleanAllPins();

        LoadUserDataAsync();
        IsRecenter = Preferences.Get("IsRecenter", false);
        UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
        OfferDistance = Preferences.Get("OfferDistance", 0.0);

        //  CARGAR DATOS NUEVOS DESPUÉS DE LIMPIAR
        await LoadDataAsync();

        isUpdatingLocation = true;
        StartAndUpdateLocation(); // Se reactiva cuando se ve
        Debug.WriteLine($"[Map Page] Estado inicial: Pins == null? {Pins == null}");

        if (NavigationService.LocationToGo is Location loc)
        {
            Debug.WriteLine($"[Map Page] NavigationService.LocationToGo encontrada en {loc.Latitude},{loc.Longitude}");

            var newPin = new MapPin(p => { /* acción al clicar */ })
            {
                Id = Guid.NewGuid().ToString(),
                Position = new Location(loc),
                Icon = "pin_offer"
            };

            if (Pins == null)
            {
                Pins = new List<MapPin>() { newPin };
            }
            else
            {
                Pins.Add(newPin);
                Pins = new List<MapPin>(Pins);
            }

            try
            {
                map.CustomPins = Pins;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Map Page] Error al asignar map.CustomPins: " + ex);
            }

            map.MoveToRegion(MapSpan.FromCenterAndRadius(loc, Distance.FromMeters(200)));
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
                Debug.WriteLine($"[Map Page] Ubicación actual: {userLocation.Latitude}, {userLocation.Longitude}");

                // Verificar promociones y tesoros SOLO si tenemos datos
                if (negocios != null && negocios.Any())
                {
                    Debug.WriteLine("[Map Page]  Verificando promociones...");
                    CheckForPromotions();
                }

                if (tesoros != null && tesoros.Any())
                {
                    Debug.WriteLine("[Map Page]  Verificando tesoros...");
                    CheckForTreasures();
                }

                // Centrado del mapa (solo si tenemos ubicación)
                if (userLocation != null)
                {
                    if (Preferences.Get("IsRecenter", false))
                    {
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                    }
                    else if (!hasCenteredMapOnce)
                    {
                        map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromKilometers(OfferDistance)));
                        hasCenteredMapOnce = true;
                    }
                }
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
                Debug.WriteLine($"[Map Page]  Error en StartAndUpdateLocation: {ex.Message}");
            }

            int frequency2 = Math.Max(updateDelayFrequency * Convert.ToInt32(UpdateFrequency), 5000);
            await Task.Delay(frequency2);
        }
    }



    public async Task LoadDataAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            Debug.WriteLine("[Map Page]  Cargando datos de negocios y tesoros...");

            negocios = await negocioService.ObtenerNegociosConPromocionesAsync();
            tesoros = await negocioService.ObtenerTesorosAsync();

            await SecureStorage.SetAsync("lastLoadDataTime", DateTime.Now.ToString("o"));
            Debug.WriteLine($"[Map Page]  Datos cargados: {negocios?.Count ?? 0} negocios, {tesoros?.Count ?? 0} tesoros");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
            Debug.WriteLine($"[Map Page]  Error al cargar datos: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Debug.WriteLine("[Map Page] OnDisappearing llamado.");
        isUpdatingLocation = false; // Se pausa cuando se va

        // Guardar la última distancia conocida
        _lastKnownOfferDistance = OfferDistance;
    }

    private void CheckForPromotions()
    {
        if (userLocation == null)
        {
            Debug.WriteLine("[Map Page] userLocation es null, no se pueden verificar promociones");
            return;
        }

        if (negocios == null || !negocios.Any())
        {
            Debug.WriteLine("[Map Page] No hay negocios para verificar");
            return;
        }

        var negociosEnRango = new List<Negocio>();

        foreach (var negocio in negocios)
        {
            if (negocio?.Location == null)
            {
                Debug.WriteLine("[Map Page] Negocio o Location es null");
                continue;
            }

            try
            {
                var distance = userLocation.CalculateDistance(negocio.Location, DistanceUnits.Kilometers);
                Debug.WriteLine($"[Map Page] Distancia a {negocio.Nombre}: {distance} km (Límite: {OfferDistance} km)");

                if (distance <= OfferDistance)
                {
                    if (!negociosAlertados.Contains(negocio.Nombre))
                    {
                        Debug.WriteLine($"[Map Page]  Mostrando alerta para: {negocio.Nombre}");
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
                    // Solo remover de alertados, los pins se limpian en OnAppearing
                    negociosAlertados.Remove(negocio.Nombre);
                    Debug.WriteLine($"[Map Page]  {negocio.Nombre} fuera de rango");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Map Page] Error calculando distancia para {negocio.Nombre}: {ex.Message}");
            }
        }

        // Limpiar ofertas que ya no están en rango
        var ofertasFueraDeRango = OfertasActuales.Except(negociosEnRango).ToList();
        foreach (var oferta in ofertasFueraDeRango)
        {
            OfertasActuales.Remove(oferta);
            Debug.WriteLine($"[Map Page] Removida oferta fuera de rango: {oferta.Nombre}");
        }
    }


    private void ShowPromotionAlert(Negocio negocio)
    {
        try
        {
            if (negocio?.Location == null)
            {
                Debug.WriteLine("[Map Page]  Negocio o Location es null");
                return;
            }

            // Verificar si ya existe un pin para este negocio
            if (Pins?.Any(p => p.Position.Latitude == negocio.Location.Latitude &&
                              p.Position.Longitude == negocio.Location.Longitude) == true)
            {
                Debug.WriteLine($"[Map Page]  Pin ya existe para: {negocio.Nombre}");
                return;
            }

            Debug.WriteLine($"[Map Page]  Agregando pin para: {negocio.Nombre}");

            var promotionPin = new MapPin(p =>
            {
                Debug.WriteLine($"[Map Page]  Click en promoción: {negocio.Nombre}");
                DisplayPromotionDetails(negocio);
            })
            {
                Id = negocio.Nombre,
                Position = negocio.Location,
                Icon = "pin_offer",
                Width = 200,
                Height = 200
            };

            if (Pins == null)
                Pins = new List<MapPin>();

            Pins.Add(promotionPin);
            Pins = new List<MapPin>(Pins);

            //  Asegurar que el mapa se actualice
            map.CustomPins = Pins;

            Debug.WriteLine($"[Map Page]  Pin agregado: {negocio.Nombre} en {negocio.Location.Latitude},{negocio.Location.Longitude}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Map Page]  Error en ShowPromotionAlert: {ex.Message}");
        }
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
                    // Pasar la ubicación REAL del negocio, no default
                    var detallesPage = new PromocionDetallesPage(promocionSeleccionada, negocio.Location);
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
                var coords = tesoro.ubicacion
                    .Replace("POINT(", "")
                    .Replace(")", "")
                    .Split(" ");

                double lng = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                double lat = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);

                var tesoroLocation = new Location(lat, lng);
                var distance = userLocation.CalculateDistance(tesoroLocation, DistanceUnits.Kilometers);

                if (distance <= OfferDistance)
                {
                    if (Pins?.All(p => p.Position != tesoroLocation) ?? true)
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.2));
                        ShowTreasureAlert(tesoro, tesoroLocation);
                    }
                }
                else
                {
                    // Buscar y eliminar pin de este tesoro
                    var pinToRemove = Pins?.FirstOrDefault(p => p.Position == tesoroLocation);
                    if (pinToRemove != null)
                    {
                        Pins.Remove(pinToRemove);
                        Pins = new List<MapPin>(Pins); // refresca binding
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

    private void CleanAllPins()
    {
        try
        {
            Debug.WriteLine("[Map Page]  Iniciando limpieza completa de pins...");

            // 1. Limpiar lista de Pins (binding property)
            if (Pins != null)
            {
                Debug.WriteLine($"[Map Page] Limpiando {Pins.Count} pins de la lista");
                Pins.Clear();
                Pins = new List<MapPin>(); // Forzar actualización del binding
            }
            else
            {
                Pins = new List<MapPin>();
            }

            // 2. Limpiar pins nativos del mapa
            map.Pins.Clear();
            Debug.WriteLine("[Map Page] Pins nativos del mapa limpiados");

            // 3. Limpiar todas las colecciones internas
            negociosAlertados.Clear();
            Debug.WriteLine($"[Map Page] negociosAlertados limpiado ({negociosAlertados.Count} items)");

            OfertasActuales.Clear();
            Debug.WriteLine($"[Map Page] OfertasActuales limpiado ({OfertasActuales.Count} items)");

            // 4. Forzar actualización del mapa
            map.CustomPins = Pins;

            Debug.WriteLine("[Map Page]  Limpieza completa terminada");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Map Page]  Error en CleanAllPins: {ex.Message}");
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