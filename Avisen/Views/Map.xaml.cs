using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Collections.Generic;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    private Location userLocation;
    private List<Negocio> negocios;
    private NegocioService negocioService;

    public Map()
    {
        InitializeComponent();
        negocioService = new NegocioService(); // Inicializa el servicio
        CargarNegocios(); // Carga los datos asíncronamente
        MoveToUserLocation();
        map.PropertyChanged += OnMapPropertyChanged;
    }

    private async void CargarNegocios()
    {
        negocios = await negocioService.ObtenerNegociosAsync(); // Obtiene los datos asíncronamente
        CheckForPromotions(); // Verifica promociones después de cargar los datos
    }

    private async void MoveToUserLocation()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                userLocation = new Location(location.Latitude, location.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(userLocation, Distance.FromMiles(0.5))); // Zoom más cercano
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
        foreach (var negocio in negocios)
        {
            var distance = userLocation.CalculateDistance(negocio.Ubicacion, DistanceUnits.Kilometers);
            if (distance <= 0.1) // 500 metros de radio
            {
                // Verificar si el Pin ya existe para evitar duplicados
                if (!map.Pins.Any(pin => pin.Label == negocio.Nombre))
                {
                    ShowPromotionAlert(negocio); // Agregar el Pin al mapa
                }
            }
            else
            {
                // Si el negocio no está dentro del radio, eliminar su Pin (opcional)
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
        var promotionPin = new Pin
        {
            Label = negocio.Nombre,
            Address = "¡Oferta!",
            Type = PinType.Place,
            Location = negocio.Ubicacion
        };

        // Asignar el evento de clic al Pin
        promotionPin.MarkerClicked += (s, e) =>
        {
            DisplayPromotionDetails(negocio);
        };

        map.Pins.Add(promotionPin); // Agregar el Pin al mapa
    }

    private async void DisplayPromotionDetails(Negocio negocio)
    {
        var detallesPage = new PromocionDetallesPage(negocio); // Crear la página
        await Navigation.PushModalAsync(detallesPage); // Mostrar la página modal
    }

}