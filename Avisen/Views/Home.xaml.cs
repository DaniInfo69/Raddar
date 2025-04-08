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

        public List<string> Filters { get; } = new List<string> { "Ofertas Vistas", "Ofertas Cercanas", "Todas las Ofertas" };

        // Propiedades para selecciones temporales (no aplican filtros automáticamente)
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

        private string _seeHour;
        public string SeeHour
        {
            get => _seeHour;
            set => SetProperty(ref _seeHour, value);
        }

        // Comandos
        public ICommand TapCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public Home()
        {
            InitializeComponent();
            UpdateFrequency = Preferences.Get("UpdateFrequency", 10.0);
            SeeHour = string.Empty;
            LoadSeeHour();

            // Inicializamos con listas de promociones
            OfertasReales = new ObservableCollection<Promocion>();
            OfertasActuales = new ObservableCollection<Promocion>();

            // Inicializamos la colección de categorías
            Categorias = new ObservableCollection<Categoria>();

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
            UpdateFrequency = Preferences.Get("UpdateFrequency", 10.0);
            LoadSeeHour();

            OfertasReales.Clear();
            OfertasActuales.Clear();
            RefreshPromotions();
        }

        private void RefreshPromotions()
        {
            // Método para cargar promociones sin filtros
            var promociones = GetPromocionesFromMatrices(Map.TodasLasOfertas);
            OfertasReales.Clear();
            foreach (var promo in promociones)
                OfertasReales.Add(promo);
        }

        private async Task NavigateToDetalle(Promocion promocion)
        {
            if (promocion == null)
                return;

            // 1) Encuentra la Matriz que contiene esta promoción
            var matriz = Map.TodasLasOfertas
                           .FirstOrDefault(m => m.Promociones.Contains(promocion));

            if (matriz == null)
            {
                await DisplayAlert("Error", "No se encontró la ubicación de la promoción.", "OK");
                return;
            }

            // 2) Pasa tanto la Promoción como la Location al detalle
            await Navigation.PushModalAsync(
                new PromocionDetallesPage(promocion, matriz.Location)
            );
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
            var promociones = TempSelectedFilter switch
            {
                "Ofertas Vistas" => GetPromocionesFromMatrices(Map.OfertasVistas),
                "Ofertas Cercanas" => GetPromocionesFromMatrices(Map.OfertasActuales),
                "Todas las Ofertas" => GetPromocionesFromMatrices(Map.TodasLasOfertas),
                _ => GetPromocionesFromMatrices(Map.TodasLasOfertas)
            };

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
    }
}