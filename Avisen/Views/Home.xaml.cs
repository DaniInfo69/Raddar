using System.Collections.ObjectModel;

namespace Avisen.Views
{
    public partial class Home : ContentPage
    {
        public ObservableCollection<Negocio> OfertasReales { get; set; }

        public Home()
        {
            InitializeComponent();
            UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            if (UpdateFrequency == 0.0)
                Preferences.Set("UpdateFrequency", 15.0);

            SeeHour = string.Empty; // Inicializar con una cadena vacía
            LoadSeeHour();
            OfertasReales = new ObservableCollection<Negocio>(Map.OfertasVistas);
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Actualizar UpdateFrequency cuando se accede a la página
            UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            LoadSeeHour();

            OfertasReales.Clear();
            foreach (var oferta in Map.OfertasVistas)
            {
                OfertasReales.Add(oferta);
            }
        }

        private async void OnVerOfertaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Negocio negocio)
            {
                await Navigation.PushModalAsync(new PromocionDetallesPage(negocio));
            }
        }

        private double _UpdateFrequency;

        public double UpdateFrequency
        {
            get => _UpdateFrequency;
            set
            {
                if (_UpdateFrequency != value)
                {
                    _UpdateFrequency = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _SeeHour;
        public string SeeHour
        {
            get => _SeeHour;
            set
            {
                if (_SeeHour != value)
                {
                    _SeeHour = value;
                    OnPropertyChanged();
                }
            }
        }

        private async void LoadSeeHour()
        {
            // Obtener la hora guardada desde SecureStorage
            SeeHour = await SecureStorage.GetAsync("lastLoadDataTime") ?? "No se ha ejecutado LoadData";
        }


    }
}
