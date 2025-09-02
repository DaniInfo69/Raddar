using System.Windows.Input;

namespace Avisen.Models
{
    public class MapPin
    {
        public string Id { get; set; }
        public Location Position { get; set; }
        public string Icon { get; set; }
        public ICommand ClickedCommand { get; set; }

        // Tamaño personalizado
        public int Width { get; set; } = 180;  // Tamaño por defecto
        public int Height { get; set; } = 180;

        public MapPin(Action<MapPin> clicked)
        {
            ClickedCommand = new Command(() => clicked(this));
        }
    }
}
