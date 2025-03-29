using System.Collections.ObjectModel;
using System.Windows.Input;
using Avisen.Models;
using Microsoft.Maui.Storage;

namespace Avisen.Views
{
    public partial class Home : ContentPage
    {
        public ObservableCollection<Promocion> OfertasReales { get; set; }
        public ObservableCollection<Promocion> OfertasActuales { get; set; }



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

            // Inicializamos con listas de promociones
            OfertasReales = new ObservableCollection<Promocion>(GetPromocionesFromMatrices(Map.OfertasVistas));
            OfertasActuales = new ObservableCollection<Promocion>(GetPromocionesFromMatrices(Map.OfertasActuales));

            // Modificamos el comando para recibir Promocion
            TapCommand = new Command<Promocion>(async (promo) => await NavigateToDetalle(promo));

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
            UpdateCollectionView();
        }

        private void UpdateCollectionView()
        {
            OfertasReales.Clear();

            var promociones = SelectedFilter switch
            {
                "Ofertas Vistas" => GetPromocionesFromMatrices(Map.OfertasVistas),
                "Ofertas Cercanas" => GetPromocionesFromMatrices(Map.OfertasActuales),
                "Todas las Ofertas" => GetPromocionesFromMatrices(Map.TodasLasOfertas),
                _ => new List<Promocion>()
            };

            foreach (var promo in promociones)
                OfertasReales.Add(promo);
        }


        // Nueva navegación directa desde el tap de la tarjeta
        private async Task NavigateToDetalle(Promocion promocion)
        {
            if (promocion != null)
            {
                await Navigation.PushModalAsync(new PromocionDetallesPage(promocion));
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

        private List<Promocion> GetPromocionesFromMatrices(List<Matriz> matrices)
        {
            return matrices
                .Where(m => m.Promociones.Any()) // Solo matrices con promociones
                .SelectMany(m => m.Promociones)   // Aplanamos todas las promociones
                .ToList();
        }

    }
}
