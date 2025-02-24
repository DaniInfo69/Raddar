using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
namespace Avisen.Views
{
    public partial class Settings : ContentPage
    {
        public Settings()
        {
            InitializeComponent();
            BindingContext = this;
            UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            IsDarkMode = Preferences.Get("IsDarkMode", false);
            OfferDistance = Preferences.Get("OfferDistance", 0.0);
            SliderFrequencyValue = Preferences.Get("SliderFrequencyValue", 15.0);
        }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
                Preferences.Set("IsDarkMode", value);
            }
        }

        private double _offerDistance;
        public double OfferDistance
        {
            get => _offerDistance;
            set
            {
                _offerDistance = value;
                OnPropertyChanged();
            }
        }

        private double _UpdateFrequency;

        public double UpdateFrequency
        {
            get => _UpdateFrequency;
            set
            {
                _UpdateFrequency = value;
                OnPropertyChanged();
            }
        }

        private void OnSavePreferences(object sender, EventArgs e)
        {
            try
            {
                Preferences.Set("OfferDistance", OfferDistance);
                Preferences.Set("UpdateFrequency", SliderFrequencyValue);
                Preferences.Set("SliderFrequencyValue", SliderFrequencyValue);
                DisplayAlert("Guardado", "Se ha guardado correctamente", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", "" + ex + "", "OK");
            }
        }

        private double _sliderFrequencyValue;
        public double SliderFrequencyValue
        {
            get => _sliderFrequencyValue;
            set
            {
                _sliderFrequencyValue = Math.Round(value);
                OnPropertyChanged();
            }
        }

        private void OnSliderFrequencyValueChanged(object sender, ValueChangedEventArgs e)
        {
            SliderFrequencyValue = Math.Round(e.NewValue);
        }

    }
}
