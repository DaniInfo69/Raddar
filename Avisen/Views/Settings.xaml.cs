using System.Diagnostics;
using Avisen.Models;
using Avisen.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;

namespace Avisen.Views
{
    public partial class Settings : ContentPage
    {
        public ObservableCollection<Favorito> Favoritos { get; set; } = new ObservableCollection<Favorito>();
        public ICommand EliminarFavoritoCommand { get; }
        private int UserId;
        public Settings()
        {
            BindingContext = this;
            InitializeComponent();
            saveButton.IsEnabled = false;
            EliminarFavoritoCommand = new Command<int>(OnEliminarFavorito);
        }

        private async Task LoadUserDataAsync()
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
                        UserId = Convert.ToInt32(userData.IdUsuario);
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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserDataAsync();
            IsRecenter = Preferences.Get("IsRecenter", false);
            OfferDistance = Preferences.Get("OfferDistance", 0.0);
            UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            SliderOfferDistanceValue = OfferDistance;
            SliderFrequencyValue = UpdateFrequency;
            await ObtenerFavoritos();

            
        }

        private async Task ObtenerFavoritos()
        {
            try
            {
                var apiService = new ApiService();
                var favoritos = await apiService.ObtenerFavoritosPorUsuarioAsync(UserId);
                Console.WriteLine(UserId);
                Favoritos.Clear();
                foreach (var favorito in favoritos)
                {
                    Favoritos.Add(favorito);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }

        //Cambiar el switch
        private bool _isRecenter;
        public bool IsRecenter
        {
            get => _isRecenter;
            set
            {
                _isRecenter = value;
                OnPropertyChanged();
                Preferences.Set("IsRecenter", value);
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
            IsEqualToOriginalValue();
        }

        //Incrementar el valor de la distancia de rastreo con el slider

        private double _sliderOfferDistanceValue;
        public double SliderOfferDistanceValue
        {
            get => _sliderOfferDistanceValue;
            set
            {
                _sliderOfferDistanceValue = value;
                OnPropertyChanged();
            }
        }
        private void OnSliderOfferDistanceValueChanged(object sender, ValueChangedEventArgs e)
        {
            SliderOfferDistanceValue = Math.Round(e.NewValue * 2) / 2.0;
            IsEqualToOriginalValue();
        }
        private void IsEqualToOriginalValue()
        {
            if ((SliderOfferDistanceValue == Preferences.Get("OfferDistance", 0.0)) && (SliderFrequencyValue == Preferences.Get("UpdateFrequency", 0.0)))
            {
                saveButton.IsEnabled = false;
                saveButton.IsVisible = false;
            }
            else
            {
                saveButton.IsEnabled = true;
                saveButton.IsVisible = true;

            }
        }

        //Guardar los datos en Preferences
        private void OnSavePreferences(object sender, EventArgs e)
        {
            saveButton.IsEnabled = false;
            saveButton.IsVisible = false;
            try
            {
                Preferences.Set("OfferDistance", SliderOfferDistanceValue);
                Preferences.Set("UpdateFrequency", SliderFrequencyValue);
                DisplayAlert("Guardado", "Se ha guardado correctamente", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", "" + ex + "", "OK");
            }
        }

        private void SwtichRecenter_Toggled(object sender, ToggledEventArgs e)
        {
            // Cambia el color del Switch dependiendo del estado IsToggled
            if (e.Value)
            {
                SwitchRecenter.OnColor = Color.FromArgb("#0aa59b"); // Color verde cuando activado
                SwitchRecenter.ThumbColor = Color.FromArgb("#f0ebdc"); // Color verde cuando activado
            }
            else
            {
                SwitchRecenter.OnColor = Colors.Gray; // Color rojo cuando desactivado
                SwitchRecenter.ThumbColor = Color.FromArgb("#f0ebdc"); // Color verde cuando activado
            }

        }

        private void OnEliminarFavorito(int idfavorito)
        {
            Debug.WriteLine($"ID del usuario: {UserId}");
            Debug.WriteLine($"Eliminar favorito con ID: {idfavorito}");
        }

    }
}
