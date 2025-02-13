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
    private bool isUpdatingLocation; // Bandera para controlar la actualizaci�n de la ubicaci�n

    public static List<Negocio> OfertasVistas { get; private set; } = new List<Negocio>();

    public Map()
    {
        InitializeComponent();
        negocioService = new NegocioService(); // Inicializa el servicio
        CargarNegocios(); // Carga los datos as�ncronamente
        MoveToUserLocation();
        map.PropertyChanged += OnMapPropertyChanged;

        // Iniciar la actualizaci�n continua de la ubicaci�n
        StartLocationUpdates();
    }

    private async void StartLocationUpdates()
    {
        isUpdatingLocation = true;

        while (isUpdatingLocation)
        {
            await GetUserLocationAsync(); // Obtener la ubicaci�n actual
            await Task.Delay(5000); // Esperar 5 segundos antes de la pr�xima actualizaci�n
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
                CheckForPromotions(); // Verificar promociones cercanas
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la ubicaci�n: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        isUpdatingLocation = false; // Detener la actualizaci�n de la ubicaci�n
    }

    private async void CargarNegocios()
    {
        negocios = await negocioService.ObtenerNegociosAsync(); // Obtiene los datos as�ncronamente
        CheckForPromotions(); // Verifica promociones despu�s de cargar los datos
    }

    private async void MoveToUserLocation()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                userLocation = new Location(location.Latitude, location.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromMiles(0.5))); // Zoom m�s cercano
                CheckForPromotions();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener la ubicaci�n: {ex.Message}");
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
        foreach (var negocio in negocios)
        {
            var distance = userLocation.CalculateDistance(negocio.Ubicacion, DistanceUnits.Kilometers);
            if (distance <= 0.1) // 100 metros de radio
            {
                // Verificar si el Pin ya existe para evitar duplicados
                if (!map.Pins.Any(pin => pin.Label == negocio.Nombre))
                {
                    ShowPromotionAlert(negocio); // Agregar el Pin al mapa
                }
            }
            else
            {
                // Si el negocio no est� dentro del radio, eliminar su Pin (opcional)
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
            OfertasVistas.Add(negocio); // Guardar la oferta real
        }

        var promotionPin = new Pin
        {
            Label = negocio.Nombre,
            Address = "�Oferta!",
            Type = PinType.Place,
            Location = negocio.Ubicacion
        };

        promotionPin.MarkerClicked += (s, e) =>
        {
            DisplayPromotionDetails(negocio);
        };

        map.Pins.Add(promotionPin);
    }

    private async void DisplayPromotionDetails(Negocio negocio)
    {
        var detallesPage = new PromocionDetallesPage(negocio); // Crear la p�gina
        await Navigation.PushModalAsync(detallesPage); // Mostrar la p�gina modal
    }
}