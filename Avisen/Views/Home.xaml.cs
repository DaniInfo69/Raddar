using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;

namespace Avisen.Views
{
    public partial class Home : ContentPage
    {
        public ObservableCollection<Negocio> OfertasReales { get; set; }
        public ObservableCollection<Negocio> OfertasActuales { get; set; }

        public List<string> Filters { get; } = new List<string> { "Ofertas Vistas", "Ofertas Cercanas" };


        private string _selectedFilter;
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter != value)
                {
                    _selectedFilter = value;
                    OnPropertyChanged();
                    UpdateCollectionView();
                }
            }
        }

        private double _updateFrequency;
        public double UpdateFrequency
        {
            get => _updateFrequency;
            set
            {
                if (_updateFrequency != value)
                {
                    _updateFrequency = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _seeHour;
        public string SeeHour
        {
            get => _seeHour;
            set
            {
                if (_seeHour != value)
                {
                    _seeHour = value;
                    OnPropertyChanged();
                }
            }
        }

        public Home()
        {
            InitializeComponent();
            UpdateFrequency = Preferences.Get("UpdateFrequency", 10.0);
            SeeHour = string.Empty;
            LoadSeeHour();

            OfertasReales = new ObservableCollection<Negocio>(Map.OfertasVistas);
            OfertasActuales = new ObservableCollection<Negocio>(Map.OfertasActuales);

            // Filtro por defecto
            SelectedFilter = "Ofertas Vistas";

            BindingContext = this;
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateFrequency = Preferences.Get("UpdateFrequency", 10.0);
            LoadSeeHour();

            OfertasReales.Clear();
            OfertasActuales.Clear();

            foreach (var oferta in Map.OfertasVistas)
                OfertasReales.Add(oferta);

            foreach (var oferta in Map.OfertasActuales)
                OfertasActuales.Add(oferta);

            // Asegurar que se actualice la vista con el filtro correcto
            UpdateCollectionView();
        }


        private void UpdateCollectionView()
        {
            if (SelectedFilter == "Ofertas Vistas")
            {
                OfertasList.ItemsSource = OfertasReales;
            }
            else if (SelectedFilter == "Ofertas Cercanas")
            {
                OfertasList.ItemsSource = OfertasActuales;
            }
        }

        private async void OnVerOfertaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Negocio negocio)
            {
                await Navigation.PushModalAsync(new PromocionDetallesPage(negocio));
            }
        }

        private async void LoadSeeHour()
        {
            SeeHour = await SecureStorage.GetAsync("lastLoadDataTime") ?? "No se ha ejecutado LoadData";
        }

        private void FiltroPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FiltroPicker.SelectedIndex == 0)
            {
                SelectedFilter = "Ofertas Vistas";
            }
            else if (FiltroPicker.SelectedIndex == 1)
            {
                SelectedFilter = "Ofertas Cercanas";
            }
        }
    }
}
