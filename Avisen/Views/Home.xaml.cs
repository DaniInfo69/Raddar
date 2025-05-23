using System.Collections.ObjectModel;
using System.Windows.Input;
using Avisen.Models;
using Avisen.Services;
using System.Runtime.CompilerServices;

namespace Avisen.Views
{
    public partial class Home : ContentPage
    {
        public ObservableCollection<Promocion> OfertasReales { get; set; }
        public ObservableCollection<Promocion> OfertasActuales { get; set; }
        public ObservableCollection<Categoria> Categorias { get; set; }
        private List<Promocion> todasLasPromosCache = new(); // para filtros


        public List<string> Filters { get; } = new List<string> { "Ofertas Vistas", "Ofertas Cercanas", "Todas las Ofertas" };

        private string _tempSelectedFilter;
        public string TempSelectedFilter
        {
            get => _tempSelectedFilter;
            set => SetProperty(ref _tempSelectedFilter, value);
        }

        private Categoria _tempSelectedCategory;
        public Categoria TempSelectedCategory
        {
            get => _tempSelectedCategory;
            set => SetProperty(ref _tempSelectedCategory, value);
        }

        private double _updateFrequency;
        public double UpdateFrequency
        {
            get => _updateFrequency;
            set => SetProperty(ref _updateFrequency, value);
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

        // Comandos
        public ICommand TapCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        private readonly NegocioService negocioService;
        public Home(NegocioService negocioService)
        {
            InitializeComponent();
            this.negocioService = negocioService;

            OfertasReales = new ObservableCollection<Promocion>();
            OfertasActuales = new ObservableCollection<Promocion>();
            Categorias = new ObservableCollection<Categoria>();

            LoadPromotions();

            // Inicializar comandos
            TapCommand = new Command<Promocion>(async (promo) => await NavigateToDetalle(promo));
            ApplyFiltersCommand = new Command(ApplyFilters);
            ClearFiltersCommand = new Command(ClearFilters);

            // Cargamos las categorías
            _ = CargarCategoriasAsync();

            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            OfferDistance = Preferences.Get("OfferDistance", 0.0);

            if (UpdateFrequency == 0)
            {
                try
                {
                    Preferences.Set("UpdateFrequency", 20.0);
                    Preferences.Set("OfferDistance", 0.5);
                }
                catch (Exception ex)
                {
                    DisplayAlert("Error", "" + ex + "", "OK");
                }
                finally
                {
                    UpdateFrequency = Preferences.Get("UpdateFrequency", 0.0);
                    OfferDistance = Preferences.Get("OfferDistance", 0.0);
                }

            }

            OfertasReales.Clear();
            OfertasActuales.Clear();
            RefreshPromotions();
        }

        private void RefreshPromotions()
        {
            if (todasLasPromosCache.Any())
            {
                OfertasReales.Clear();
                foreach (var promo in todasLasPromosCache)
                {
                    OfertasReales.Add(promo);
                }
            }
        }


        private async Task NavigateToDetalle(Promocion promocion)
        {
            if (promocion == null)
                return;

            try
            {
                var negocios = await negocioService.ObtenerMatricesConPromocionesAsync();
                var matriz = negocios.FirstOrDefault(m => m.Promociones.Any(p => p.idpromocion == promocion.idpromocion));

                if (matriz == null)
                {
                    await DisplayAlert("Error", "No se encontró la ubicación de la promoción.", "OK");
                    return;
                }

                await Navigation.PushAsync(new PromocionDetallesPage(promocion, matriz.Location));
                //await Navigation.PushModalAsync(new PromocionDetallesPage(promocion, matriz.Location));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo navegar al detalle: {ex.Message}", "OK");
            }
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
            return matrices?
                .Where(m => m.Promociones?.Any() == true)
                .SelectMany(m => m.Promociones)
                .ToList() ?? new List<Promocion>();
        }

        private async Task CargarCategoriasAsync()
        {
            try
            {
                var apiService = new ApiService();
                var categoriasObtenidas = await apiService.ObtenerCategoriaAsync();

                // Agregar opción "Todas las categorías"
                categoriasObtenidas.Insert(0, new Categoria
                {
                    idcategoria = -1,
                    Nombre = "Todas las categorías"
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Categorias.Clear();
                    foreach (var categoria in categoriasObtenidas)
                    {
                        Categorias.Add(categoria);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando categorías: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var promociones = todasLasPromosCache;

            if (TempSelectedFilter == "Ofertas Vistas")
            {
                promociones = Map.OfertasVistas.SelectMany(m => m.Promociones).ToList();
            }
            else if (TempSelectedFilter == "Ofertas Cercanas")
            {
                promociones = Map.OfertasActuales.SelectMany(m => m.Promociones).ToList();
            }

            if (TempSelectedCategory != null && TempSelectedCategory.idcategoria != -1)
            {
                promociones = promociones.Where(p => p.categoria_idcategoria == TempSelectedCategory.idcategoria).ToList();
            }

            OfertasReales.Clear();
            foreach (var promo in promociones)
                OfertasReales.Add(promo);

            OnCerrarFiltroTapped(null, null);
        }


        private void ClearFilters()
        {
            // Restablecer a valores por defecto
            TempSelectedFilter = "Todas las Ofertas";
            TempSelectedCategory = Categorias.FirstOrDefault(c => c.idcategoria == -1);

            // Notificar cambios en las propiedades
            OnPropertyChanged(nameof(TempSelectedFilter));
            OnPropertyChanged(nameof(TempSelectedCategory));
        }

        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private async void LoadPromotions()
        {
            try
            {
                // Cargar directamente desde el servicio, no desde Map
                var negocios = await negocioService.ObtenerMatricesConPromocionesAsync();

                // Guardar localmente para Home
                var todasLasPromos = negocios
                    .Where(m => m.Promociones != null)
                    .SelectMany(m => m.Promociones)
                    .ToList();

                OfertasReales.Clear();
                foreach (var promo in todasLasPromos)
                {
                    OfertasReales.Add(promo);
                }
                Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.1));
                // Guardar localmente en una lista privada si deseas filtrar más adelante
                todasLasPromosCache = todasLasPromos;

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar promociones: {ex.Message}", "OK");
            }
        }


    }
}