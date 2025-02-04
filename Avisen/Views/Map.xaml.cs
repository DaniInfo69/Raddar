using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Avisen.Views;

public partial class Map : ContentPage
{
    public Map()
    {
        InitializeComponent();
        MoveToUserLocation();
        map.PropertyChanged += OnMapPropertyChanged;
    }

    private async void MoveToUserLocation()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                var position = new Location(location.Latitude, location.Longitude);
                map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMiles(0.5))); // Zoom más cercano
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
}