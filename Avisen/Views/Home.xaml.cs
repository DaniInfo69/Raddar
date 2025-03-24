using System.Collections.ObjectModel;
using System.Windows.Input;
using Avisen.Models;
using Microsoft.Maui.Storage;

namespace Avisen.Views
{
    public partial class Home : ContentPage
    {
        public ObservableCollection<Negocio> OfertasReales { get; set; }
        public ObservableCollection<Negocio> OfertasActuales { get; set; }

        public List<string> Filters { get; } = new List<string> { "Ofertas Vistas", "Ofertas Cercanas", "Todas las Ofertas" };

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

        // Comando que usará la tarjeta para navegar
        public ICommand TapCommand { get; set; }

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

            // Inicializamos el comando para el tap
            TapCommand = new Command<Negocio>(async (negocio) => await NavigateToDetalle(negocio));

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

            UpdateCollectionView();
        }

        private void UpdateCollectionView()
        {
            OfertasReales.Clear();

            if (SelectedFilter == "Ofertas Vistas")
            {
                foreach (var oferta in Map.OfertasVistas)
                    OfertasReales.Add(oferta);
            }
            else if (SelectedFilter == "Ofertas Cercanas")
            {
                foreach (var oferta in Map.OfertasActuales)
                    OfertasReales.Add(oferta);
            }
            else if (SelectedFilter == "Todas las Ofertas")
            {
                foreach (var oferta in Map.TodasLasOfertas)
                    OfertasReales.Add(oferta);
            }
        }

        // Nueva navegación directa desde el tap de la tarjeta
        private async Task NavigateToDetalle(Negocio negocio)
        {
            if (negocio != null)
            {
                await Navigation.PushModalAsync(new PromocionDetallesPage(negocio));
            }
        }

        private async void LoadSeeHour()
        {
            SeeHour = await SecureStorage.GetAsync("lastLoadDataTime") ?? "No se ha ejecutado LoadData";
        }


        private async void OnFiltrarTapped(object sender, EventArgs e)
        {
            FiltroPopup.IsVisible = true;
            await PopupFrame.FadeTo(1, 250, Easing.CubicInOut);
            await PopupFrame.ScaleTo(1, 250, Easing.CubicOut);
        }

        private async void OnCerrarFiltroTapped(object sender, EventArgs e)
        {
            await PopupFrame.ScaleTo(0.8, 200, Easing.CubicIn);
            await PopupFrame.FadeTo(0, 200, Easing.CubicOut);
            FiltroPopup.IsVisible = false;
        }


    }
}
