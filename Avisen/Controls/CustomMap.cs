using Avisen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avisen.Controls
{
    public class CustomMap : Microsoft.Maui.Controls.Maps.Map
    {
        public List<MapPin> CustomPins
        {
            get => (List<MapPin>)GetValue(CustomPinsProperty);
            set => SetValue(CustomPinsProperty, value);
        }
        public static readonly BindableProperty CustomPinsProperty =
    BindableProperty.Create(nameof(CustomPins), typeof(List<MapPin>), typeof(CustomMap), new List<MapPin>());

    }

}
