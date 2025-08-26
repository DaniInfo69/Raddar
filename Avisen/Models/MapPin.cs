using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Avisen.Models
{
    public class MapPin
    {
        public string Id { get; set; }
        public Location Position { get; set; }
        public string Icon { get; set; }
        public ICommand ClickedCommand { get; set; }

        public MapPin(Action<MapPin> clicked)
        {
            ClickedCommand = new Command(() => clicked(this));
        }
    }
}
