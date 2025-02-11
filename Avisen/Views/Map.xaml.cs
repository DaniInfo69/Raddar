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
        CargarNegocios(); // Carga los datos as�ncronamente
        MoveToUserLocation();
        map.PropertyChanged += OnMapPropertyChanged;
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
        var promotionPin = new Pin
        {
            Label = negocio.Nombre,
            Address = "�Oferta!",
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
        // Crear un mensaje con los detalles de todas las promociones del negocio
        string mensaje = $"Promociones en {negocio.Nombre}:\n\n";
        foreach (var promocion in negocio.Promociones)
        {
            mensaje += $"**{promocion.Nombre}**\n";
            mensaje += $"{promocion.Descripcion}\n";
            mensaje += $"Precio: {(promocion.Precio == 0 ? "Oferta especial" : $"${promocion.Precio}")}\n";
            mensaje += $"Vigencia: {promocion.Vigencia.ToShortDateString()}\n\n";
        }

        // Mostrar el mensaje en una alerta
        await DisplayAlert("Detalles de la Promoci�n", mensaje, "Ver promoci�n", "Cerrar");
    }
}