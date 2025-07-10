using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Avisen.Models;
using Avisen.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace Avisen.Views
{
    public partial class Home : ContentPage
    {
        public ObservableCollection<Promocion> OfertasReales { get; set; }
        public ObservableCollection<Categoria> Categorias { get; set; }
        private List<Promocion> todasLasPromosCache = new();
        private bool timerIniciado = false;
        private Categoria _categoriaSeleccionada;

        public ObservableCollection<Promocion> OfertasDestacadas { get; set; } = new();



        public Categoria CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                if (_categoriaSeleccionada != value)
                {
                    // Deseleccionar la categoría anterior
                    if (_categoriaSeleccionada != null)
                        _categoriaSeleccionada.IsSelected = false;

                    _categoriaSeleccionada = value;

                    // Seleccionar la nueva categoría
                    if (_categoriaSeleccionada != null)
                    {
                        _categoriaSeleccionada.IsSelected = true;
                        FiltrarOfertasPorCategoria(_categoriaSeleccionada);
                    }

                    OnPropertyChanged();
                }
            }
        }

        private readonly NegocioService negocioService;

        public ICommand TapCommand { get; }

        public Home(NegocioService negocioService)
        {
            InitializeComponent();
            this.negocioService = negocioService;

            OfertasReales = new ObservableCollection<Promocion>();
            Categorias = new ObservableCollection<Categoria>();
            OfertasDestacadas = new ObservableCollection<Promocion>();

            TapCommand = new Command<Promocion>(async (promo) => await NavigateToDetalle(promo));

            BindingContext = this;

            LoadUserNameAsync();
            _ = CargarCategoriasAsync();
            LoadPromotions(); // Llenará también las ofertas destacadas correctamente
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            double updateFrequency = Preferences.Get("UpdateFrequency", 0.0);
            double offerDistance = Preferences.Get("OfferDistance", 0.0);

            if (updateFrequency == 0)
            {
                Preferences.Set("UpdateFrequency", 20.0);
                Preferences.Set("OfferDistance", 0.5);
            }

            OfertasReales.Clear();
            RefreshPromotions();

            if (!timerIniciado)
            {
                timerIniciado = true;
                this.Dispatcher.StartTimer(TimeSpan.FromSeconds(20), () =>
                {
                    LoadPromotions();
                    return true;
                });
            }

            var carouselTimer = Application.Current.Dispatcher.CreateTimer();
            carouselTimer.Interval = TimeSpan.FromSeconds(8);
            carouselTimer.Tick += (_, _) =>
            {
                if (OfertasDestacadas.Count > 1)
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        int next = (HotOffersCarousel.Position + 1) % OfertasDestacadas.Count;
                        HotOffersCarousel.ScrollTo(next, animate: true);
                    });
            };
            carouselTimer.Start();

        }

        private void RefreshPromotions()
        {
            OfertasReales.Clear();
            foreach (var promo in todasLasPromosCache)
                OfertasReales.Add(promo);
        }

        private async Task NavigateToDetalle(Promocion promocion)
        {
            if (promocion == null) return;

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
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo navegar al detalle: {ex.Message}", "OK");
            }
        }

        private async Task CargarCategoriasAsync()
        {
            try
            {
                var apiService = new ApiService();
                var categoriasObtenidas = await apiService.ObtenerCategoriaAsync();

                categoriasObtenidas.Insert(0, new Categoria
                {
                    idcategoria = -1,
                    Nombre = "Todas las categorías"
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Categorias.Clear();
                    foreach (var categoria in categoriasObtenidas)
                        Categorias.Add(categoria);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando categorías: {ex.Message}");
            }
        }

        private async void LoadPromotions()
        {
            try
            {
                var negocios = await negocioService.ObtenerMatricesConPromocionesAsync();
                var nuevasPromos = negocios
                    .Where(m => m.Promociones != null)
                    .SelectMany(m => m.Promociones)
                    .ToList();

                var promocionesJson = Preferences.Get("PromosGuardadas", null);
                List<Promocion> promosGuardadas = new();

                if (!string.IsNullOrEmpty(promocionesJson))
                {
                    try
                    {
                        promosGuardadas = JsonSerializer.Deserialize<List<Promocion>>(promocionesJson);
                    }
                    catch { }
                }

                var idsAntiguos = promosGuardadas.Select(p => p.idpromocion).ToHashSet();
                var idsNuevos = nuevasPromos.Select(p => p.idpromocion).ToHashSet();

                if (!idsAntiguos.SetEquals(idsNuevos))
                {
                    var isGranted = await LocalNotificationCenter.Current.AreNotificationsEnabled();
                    if (!isGranted)
                        await LocalNotificationCenter.Current.RequestNotificationPermission();

                    var notification = new NotificationRequest
                    {
                        NotificationId = 1001,
                        Title = "¡Nuevas ofertas disponibles!",
                        Description = "Hay promociones nuevas que podrían interesarte.",
                        Schedule = new NotificationRequestSchedule
                        {
                            NotifyTime = DateTime.Now.AddSeconds(1)
                        }
                    };

                    await LocalNotificationCenter.Current.Show(notification);
                }

                var nuevasPromosJson = JsonSerializer.Serialize(nuevasPromos);
                Preferences.Set("PromosGuardadas", nuevasPromosJson);

                todasLasPromosCache = nuevasPromos;

                OfertasDestacadas.Clear();
                foreach (var promo in nuevasPromos.Take(3))
                    OfertasDestacadas.Add(promo);


                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (CategoriaSeleccionada != null)
                        FiltrarOfertasPorCategoria(CategoriaSeleccionada);
                    else
                    {
                        OfertasReales.Clear();
                        foreach (var promo in nuevasPromos)
                            OfertasReales.Add(promo);
                    }
                });


                Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.1));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar promociones: {ex.Message}", "OK");
            }
        }

        private async void LoadUserNameAsync()
        {
            try
            {
                var userDataJson = await SecureStorage.GetAsync("UserData");

                if (!string.IsNullOrEmpty(userDataJson))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var userData = JsonSerializer.Deserialize<UserData>(userDataJson, options);

                    lblUserName.Text = userData != null
                        ? $"¡Hola, {userData.NombreCliente}!"
                        : "Nombre no disponible";
                }
                else
                {
                    lblUserName.Text = "No se encontró información del usuario.";
                }
            }
            catch (Exception ex)
            {
                lblUserName.Text = "Error al cargar el nombre";
                await DisplayAlert("Error", $"Detalles: {ex.Message}", "OK");
            }
        }

        private void FiltrarOfertasPorCategoria(Categoria categoria)
        {
            var filtradas = categoria.idcategoria == -1
                ? todasLasPromosCache
                : todasLasPromosCache
                    .Where(p => p.categoria_idcategoria == categoria.idcategoria)
                    .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OfertasReales.Clear();
                foreach (var promo in filtradas)
                    OfertasReales.Add(promo);
            });
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