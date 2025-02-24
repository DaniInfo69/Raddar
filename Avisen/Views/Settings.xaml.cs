namespace Avisen.Views
{
    public partial class Settings : ContentPage
    {
        public Settings()
        {
            InitializeComponent();
            saveButton.IsEnabled = false;
            BindingContext = this;
            UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            IsDarkMode = Preferences.Get("IsDarkMode", false);
            OfferDistance = Preferences.Get("OfferDistance", 0.0);
            SliderFrequencyValue = Preferences.Get("SliderFrequencyValue", 15.0);
            SliderOfferDistanceValue = Preferences.Get("SliderOfferDistanceValue", 1.0);
            
        }

        //Cambiar el switch
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

        //Cambiar distancia de deteccion de ofertas
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

        //Cambiar la frecuencia de actualizacion
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

        //Incrementar el valor de la frecuencia de actualizacion con el slider
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
            saveButton.IsEnabled = true;
        }

        //Incrementar el valor de la distancia de rastreo con el slider

        private double _sliderOfferDistanceValue;
        public double SliderOfferDistanceValue
        {
            get => _sliderOfferDistanceValue;
            set
            {
                _sliderOfferDistanceValue = Math.Round(value);
                OnPropertyChanged();
            }
        }
        private void OnSliderOfferDistanceValueChanged(object sender, ValueChangedEventArgs e)
        {
            SliderOfferDistanceValue = Math.Round(e.NewValue);
            saveButton.IsEnabled = true;
        }

        //Guardar los datos en SecurityStorage
        private void OnSavePreferences(object sender, EventArgs e)
        {
            saveButton.IsEnabled = false;
            try
            {
                Preferences.Set("OfferDistance", SliderOfferDistanceValue);
                Preferences.Set("SliderOfferDistanceValue", SliderOfferDistanceValue);
                Preferences.Set("UpdateFrequency", SliderFrequencyValue);
                Preferences.Set("SliderFrequencyValue", SliderFrequencyValue);
                DisplayAlert("Guardado", "Se ha guardado correctamente", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", "" + ex + "", "OK");
            }
        }



    }
}
