using Avisen.Models;
using Avisen.Services;
using Microsoft.Maui;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Maui.Devices.Sensors;    // Geolocation, Location
using Microsoft.Maui.ApplicationModel;   // Permissions
using System.Threading;                  // CancellationTokenSource


namespace Avisen.Views
{
    public partial class Home : ContentPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int currentPage = 1;
        private const int pageSize = 5;

        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage != value)
                {
                    currentPage = value;
                    OnPropertyChanged();
                    LoadPage();
                }
            }
        }

        public int TotalPages =>
    (_promosFiltradas?.Count ?? 0) == 0
        ? 1
        : (int)Math.Ceiling((double)_promosFiltradas.Count / pageSize);


        private readonly Stopwatch _loadStopwatch = new();
        private bool _isInitialized;
        private Categoria _categoriaSeleccionada;
        private IDispatcherTimer _carouselTimer;
        private IDispatcherTimer _refreshTimer; // <-- Timer guardado para el auto-refresh
        private CancellationTokenSource _cts;
        private readonly NegocioService _negocioService;
        private readonly ApiService _apiService = new ApiService();
        private List<Promocion> _todasLasPromosCache = new();
        private List<Promocion> _promosFiltradas = new();



        private bool _isNavigating;
        private bool _isLoadingDetalle;
        public bool IsLoadingDetalle
        {
            get => _isLoadingDetalle;
            set
            {
                _isLoadingDetalle = value;
                OnPropertyChanged();
            }
        }


        private bool _isRefreshing;
        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Colecciones optimizadas
        public OptimizedObservableCollection<Promocion> OfertasReales { get; } = new();
        public OptimizedObservableCollection<Categoria> Categorias { get; } = new();
        public OptimizedObservableCollection<Promocion> OfertasDestacadas { get; } = new();


        public ICommand TapCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ChangePageCommand { get; }

        public Categoria CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                if (_categoriaSeleccionada != value)
                {
                    _categoriaSeleccionada = value;
                    OnPropertyChanged();

                    if (_categoriaSeleccionada != null)
                    {
                        FiltrarOfertasPorCategoria(_categoriaSeleccionada);
                    }
                }
            }
        }


        private List<Promocion> _promosCercanasCache = new();

        // Contador y binding para el Hero
        private int _offersNearbyCount;
        public int OffersNearbyCount
        {
            get => _offersNearbyCount;
            set
            {
                if (_offersNearbyCount == value) return;
                _offersNearbyCount = value;
                OnPropertyChanged(nameof(OffersNearbyCount));
                OnPropertyChanged(nameof(OffersNearbyText));
            }
        }
        public string OffersNearbyText => $"{OffersNearbyCount} nuevas ofertas cerca de ti";

        // Command para el botón del Hero
        public ICommand ShowNearbyOffersCommand { get; }

        public Home(NegocioService negocioService)
        {
            InitializeComponent();
            _negocioService = negocioService;
            _loadStopwatch.Start();

            // Comandos
            TapCommand = new Command<Promocion>(async (p) => await NavigateToDetalle(p));
            RefreshCommand = new Command(async () => await RefreshDataAsync(force: true));
            ChangePageCommand = new Command<int>((page) =>
            {
                if (page >= 1 && page <= TotalPages)
                    CurrentPage = page;
            });

            ShowNearbyOffersCommand = new Command(async () => await ShowNearbyOffersAsync());

            BindingContext = this;
            SetDefaultPreferences();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isInitialized)
            {
                await RefreshDataAsync();
                StartTimers();
                return;
            }

            await InitializeAsync();
            _isInitialized = true;
            StartTimers();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            CleanupResources();
        }

        #region Métodos Principales Optimizados

        private async Task InitializeAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            try
            {
                _cts = new CancellationTokenSource();
                var loadTasks = new[]
                {
                    LoadUserNameAsync(_cts.Token),
                    LoadCategoriesAsync(_cts.Token),
                    LoadPromotionsAsync(_cts.Token),
                    LoadPromotionsDestacadasAsync(_cts.Token),
                    UpdateNearbyCountAsync()
                };

                await Task.WhenAll(loadTasks);
                Debug.WriteLine($"InitializeAsync completed in {_loadStopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException) { /* Ignorar cancelación */ }
            catch (Exception ex)
            {
                await SafeDisplayAlert("Error", $"Initialize error: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
                _loadStopwatch.Reset();
            }
        }

        private async Task RefreshDataAsync(bool force = false)
        {
            if (_isRefreshing && !force) return;
            _isRefreshing = true;

            try
            {
                _cts = new CancellationTokenSource();
                await Task.WhenAll(
                    LoadPromotionsAsync(_cts.Token),
                    LoadPromotionsDestacadasAsync(_cts.Token), // <-- nuevo
                    UpdateNearbyCountAsync()
                );
                Debug.WriteLine($"Refresh completed in {_loadStopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await SafeDisplayAlert("Error", $"Refresh error: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
                _loadStopwatch.Reset();
            }
        }


        #endregion

        #region Métodos de Carga Optimizados

        private async Task LoadPromotionsAsync(CancellationToken ct)
        {
            var position = await GetCurrentCarouselPosition();
            var negocios = await _negocioService.ObtenerNegociosConPromocionesAsync();

            ct.ThrowIfCancellationRequested();

            var nuevasPromos = negocios
                .Where(m => m.Promociones != null)
                .SelectMany(m => m.Promociones)
                .ToList();

            await CheckForNewPromotions(nuevasPromos);
            _todasLasPromosCache = nuevasPromos;

            await UpdateUI(nuevasPromos, position);
        }

        private async Task LoadPromotionsDestacadasAsync(CancellationToken ct)
        {

            var destacadas = await _negocioService.ObtenerPromocionesPremiumAsync();

            ct.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                OfertasDestacadas.ReplaceRange(destacadas ?? new List<Promocion>());

                if (OfertasDestacadas.Count > 0)
                {
                    HotOffersCarousel.ScrollTo(0, animate: false);
                }
            });
        }


        private async Task UpdateUI(List<Promocion> promociones, int carouselPosition)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Actualizamos la lista filtrada pero intentamos mantener la página actual
                _promosFiltradas = promociones ?? new List<Promocion>();

                // Recalcular total de páginas
                OnPropertyChanged(nameof(TotalPages));

                // Si la página actual está fuera de rango (p.e. la lista se hizo más corta)
                var totalPages = TotalPages;
                if (CurrentPage > totalPages)
                {
                    // Ajustar a la última página disponible — esto disparará LoadPage() desde el setter
                    CurrentPage = totalPages;
                }
                else
                {
                    // Mantener la página y recargar su contenido
                    LoadPage();
                }

                // No vibramos en cada refresh (lo dejamos donde detectas novedades)
            });
        }



        void LoadPage()
        {
            if (_promosFiltradas == null || _promosFiltradas.Count == 0)
                return;

            IsLoading = true;

            try
            {
                var promos = _promosFiltradas
                    .Skip((CurrentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                foreach (var promo in promos)
                {
                    if (promo.VigenciaFin.HasValue)
                    {
                        var fecha = promo.VigenciaFin.Value.ToLocalTime().Date;
                        var diasRestantes = (fecha - DateTime.Now.Date).Days;

                        if (diasRestantes < 0)
                            promo.DiasRestantesTexto = "Expirada";
                        else if (diasRestantes == 0)
                            promo.DiasRestantesTexto = "Hoy";
                        else if (diasRestantes == 1)
                            promo.DiasRestantesTexto = "1 día restante";
                        else
                            promo.DiasRestantesTexto = $"{diasRestantes} días restantes";
                    }
                    else
                    {
                        promo.DiasRestantesTexto = "Sin fecha de fin";
                    }
                }


                OfertasReales.ReplaceRange(promos);
                OnPropertyChanged(nameof(TotalPages));
                RenderPagination();
            }
            finally
            {
                IsLoading = false;
            }
        }



        private async Task LoadCategoriesAsync(CancellationToken ct)
        {
            var apiService = new ApiService();
            var categorias = await apiService.ObtenerCategoriaAsync();

            ct.ThrowIfCancellationRequested();

            categorias.Insert(0, new Categoria
            {
                idcategoria = -1,
                Nombre = "Todas las categorías"
            });

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Categorias.ReplaceRange(categorias);
                CategoriaSeleccionada = Categorias.FirstOrDefault();
            });
        }

        private async Task LoadUserNameAsync(CancellationToken ct)
        {
            var userDataJson = await SecureStorage.GetAsync("UserData");
            ct.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (string.IsNullOrEmpty(userDataJson))
                {
                    lblUserName.Text = "No se encontró información del usuario.";
                    return;
                }

                try
                {
                    var userData = JsonSerializer.Deserialize<UserData>(
                        userDataJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    lblUserName.Text = userData != null
                        ? $"¡Hola, {userData.NombreCliente}!"
                        : "Nombre no disponible";
                }
                catch
                {
                    lblUserName.Text = "Error formato usuario";
                }
            });
        }

        #endregion

        #region Métodos de Soporte Optimizados

        private async void FiltrarOfertasPorCategoria(Categoria categoria)
        {
            if (categoria.idcategoria == -1) // Todas las categorías
            {
                _promosFiltradas = _todasLasPromosCache;
            }
            else
            {
                // Consultamos al backend el nuevo endpoint
                _promosFiltradas = await _apiService.ObtenerPromocionesPorCategoriaAsync(categoria.idcategoria);
            }

            CurrentPage = 1;
            LoadPage();
        }



        private async Task CheckForNewPromotions(List<Promocion> nuevasPromos)
        {
            try
            {
                var promocionesJson = Preferences.Get("PromosGuardadas", null);
                if (string.IsNullOrEmpty(promocionesJson)) return;

                var promosGuardadas = JsonSerializer.Deserialize<List<Promocion>>(promocionesJson) ?? new List<Promocion>();

                var nuevasIds = new HashSet<int>(nuevasPromos.Select(p => p.idpromocion));
                var guardadasIds = new HashSet<int>(promosGuardadas.Select(p => p.idpromocion));

                if (nuevasIds.SetEquals(guardadasIds))
                {
                    return;
                }

                // -> Hay diferencias: mostrar notificación y vibrar solo aquí
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() ||
                    await LocalNotificationCenter.Current.RequestNotificationPermission())
                {
                    await LocalNotificationCenter.Current.Show(new NotificationRequest
                    {
                        NotificationId = 1001,
                        Title = "¡Nuevas ofertas disponibles!",
                        Description = "Hay promociones nuevas que podrían interesarte.",
                        Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now.AddSeconds(1) },
                        Android = new AndroidOptions { ChannelId = "ofertas_channel" }
                    });
                }

                // Vibración solamente si hay novedades
                SafeVibrate();

                Preferences.Set("PromosGuardadas", JsonSerializer.Serialize(nuevasPromos));
            }
            catch { /* Silenciar errores de notificación */ }
        }

        private void StartTimers()
        {
            // Parar timer previo si existe
            _refreshTimer?.Stop();
            _refreshTimer = Application.Current.Dispatcher.CreateTimer();

            // Obtener preferencia y clamar un mínimo (ej. 20s)
            var freqSeconds = Preferences.Get("UpdateFrequency", 60.0);
            freqSeconds = Math.Max(freqSeconds, 20.0);

            _refreshTimer.Interval = TimeSpan.FromSeconds(freqSeconds);
            _refreshTimer.Tick += (_, _) =>
            {
                if (!_isRefreshing)
                    _ = RefreshDataAsync();
            };
            _refreshTimer.Start();

            // Carousel: solo si no está creado y si hay más de 1 oferta destacada
            if (_carouselTimer == null && OfertasDestacadas.Count > 1)
            {
                _carouselTimer = Application.Current.Dispatcher.CreateTimer();
                _carouselTimer.Interval = TimeSpan.FromSeconds(6);
                _carouselTimer.Tick += (_, _) =>
                {
                    if (OfertasDestacadas.Count > 1)
                    {
                        var next = (HotOffersCarousel.Position + 1) % OfertasDestacadas.Count;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            HotOffersCarousel.ScrollTo(next, animate: true);
                        });
                    }
                };
                _carouselTimer.Start();
            }
        }

        private void CleanupResources()
        {
            _carouselTimer?.Stop();
            _carouselTimer = null;

            _refreshTimer?.Stop();
            _refreshTimer = null;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task<int> GetCurrentCarouselPosition()
        {
            return await MainThread.InvokeOnMainThreadAsync(() =>
                HotOffersCarousel.Position);
        }

        private async Task SafeDisplayAlert(string title, string message)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert(title, message, "OK"));
        }

        private void SafeVibrate()
        {
            try { Vibration.Default.Vibrate(TimeSpan.FromSeconds(0.1)); }
            catch { }
        }

        private static void SetDefaultPreferences()
        {
            if (Preferences.Get("UpdateFrequency", 0.0) == 0)
            {
                Preferences.Set("UpdateFrequency", 60.0); // ahora 60s por defecto
                Preferences.Set("OfferDistance", 0.5);
            }
        }

        #endregion

        private async Task NavigateToDetalle(Promocion promocion)
        {
            if (promocion == null || _isNavigating) return;

            _isNavigating = true;
            await LoadingView.ShowAsync(); // Muestra con animación

            try
            {
                var negocios = await _negocioService.ObtenerNegociosConPromocionesAsync();
                var negocio = negocios.FirstOrDefault(m =>
                    m.Promociones.Any(p => p.idpromocion == promocion.idpromocion));

                if (negocio == null)
                {
                    await SafeDisplayAlert("Error", "No se encontró la ubicación de la promoción.");
                    return;
                }

                await Navigation.PushAsync(new PromocionDetallesPage(promocion, negocio.Location));
            }
            catch (Exception ex)
            {
                await SafeDisplayAlert("Error", $"No se pudo navegar: {ex.Message}");
            }
            finally
            {
                await LoadingView.HideAsync(); // Oculta con animación
                _isNavigating = false;
            }
        }


        void RenderPagination()
        {
            PaginationLayout.Children.Clear();

            if (TotalPages <= 1)
                return;

            // Botón anterior - Siempre visible
            var prevButton = new Button
            {
                Text = "<", // Usamos un carácter Unicode más estilizado
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent,
                TextColor = CurrentPage > 1 ? Color.FromArgb("#0aa59b") : Colors.LightGray,
                Command = new Command(() =>
                {
                    if (CurrentPage > 1) CurrentPage--;
                })
                // Quitamos IsEnabled para que siempre sea clickeable (aunque no haga nada en la primera página)
            };
            PaginationLayout.Children.Add(prevButton);

            int maxButtons = 2;
            int half = maxButtons / 2;
            int startPage = Math.Max(1, CurrentPage - half);
            int endPage = Math.Min(TotalPages, startPage + maxButtons - 1);

            // Ajustar startPage si no hay suficientes páginas al final
            if (endPage - startPage + 1 < maxButtons)
            {
                startPage = Math.Max(1, endPage - maxButtons + 1);
            }


            // Primera página con elipsis si es necesario
            if (startPage > 1)
            {
                AddPageButton(1);
                if (startPage > 2)
                {
                    PaginationLayout.Children.Add(new Label
                    {
                        Text = "…", // Carácter Unicode para puntos suspensivos
                        VerticalOptions = LayoutOptions.Center,
                        Style = (Style)Resources["PaginationLabel"]
                    });
                }
            }

            // Páginas centrales
            for (int i = startPage; i <= endPage; i++)
            {
                AddPageButton(i);
            }

            // Última página con elipsis si es necesario
            

            // Botón siguiente - Siempre visible
            var nextButton = new Button
            {
                Text = ">", // Usamos un carácter Unicode más estilizado
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent,
                TextColor = CurrentPage < TotalPages ? Color.FromArgb("#0aa59b") : Colors.LightGray,
                Command = new Command(() =>
                {
                    if (CurrentPage < TotalPages) CurrentPage++;
                })
                // Quitamos IsEnabled para que siempre sea clickeable (aunque no haga nada en la última página)
            };
            PaginationLayout.Children.Add(nextButton);
        }

        void AddPageButton(int pageNumber)
        {
            var text = pageNumber.ToString();

            var button = new Button
            {
                Text = text,
                BackgroundColor = pageNumber == CurrentPage ? Color.FromArgb("#0aa59b") : Colors.Transparent,
                TextColor = pageNumber == CurrentPage ? Colors.White : Color.FromArgb("#0aa59b"),
                CornerRadius = 20,
                WidthRequest = Math.Max(50, text.Length * 18), // <--- ancho dinámico
                HeightRequest = 40,
                Command = new Command(() => CurrentPage = pageNumber)
            };

            PaginationLayout.Children.Add(button);
        }


        public class OptimizedObservableCollection<T> : ObservableCollection<T>
        {
            private bool _isBatching;

            public void BeginBatchUpdate()
            {
                _isBatching = true;
            }

            public void EndBatchUpdate()
            {
                _isBatching = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
            }

            public void ReplaceRange(IEnumerable<T> items)
            {
                BeginBatchUpdate();

                try
                {
                    Clear();
                    foreach (var item in items)
                    {
                        Add(item);
                    }
                }
                finally
                {
                    EndBatchUpdate();
                }
            }

            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (!_isBatching)
                    base.OnCollectionChanged(e);
            }
        }

        private async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                        return null; // usuario negó permisos
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                var location = await Geolocation.Default.GetLocationAsync(request, cts.Token);
                return location;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetCurrentLocationAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateNearbyCountAsync()
        {
            try
            {
                var loc = await GetCurrentLocationAsync();
                if (loc == null)
                {
                    OffersNearbyCount = 0;
                    _promosCercanasCache = new List<Promocion>();
                    return;
                }

                
                var promos = await _apiService.ObtenerPromocionesPorRangoAsync(loc.Latitude, loc.Longitude);
                _promosCercanasCache = promos ?? new List<Promocion>();
                OffersNearbyCount = _promosCercanasCache.Count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateNearbyCountAsync error: {ex.Message}");
                OffersNearbyCount = 0;
            }
        }

        private async Task ShowNearbyOffersAsync()
        {
            try
            {
                // Si la cache está vacía, forzamos una recarga rápida
                if (_promosCercanasCache == null || !_promosCercanasCache.Any())
                    await UpdateNearbyCountAsync();

                if (_promosCercanasCache == null || !_promosCercanasCache.Any())
                {
                    await SafeDisplayAlert("Aviso", "No hay promociones cercanas por mostrar.");
                    return;
                }

                // --- 2) Intentar Shell (si usas Shell) ---
                try
                {
                    // Si registraste una ruta para la MapPage (recomendado): Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
                    // Intentamos navegar por ruta (no falla si la ruta no existe, capturamos excepción)
                    var routeName = "Map"; // ajusta si tu ruta es otra, por ejemplo nameof(MapPage)
                    await Shell.Current?.GoToAsync($"//{routeName}");
                    return;
                }
                catch
                {
                    Debug.WriteLine("No se encontró la ruta");
                }


                // --- Fallback: mostrar alert con conteo (útil si no se pudo navegar) ---
                await SafeDisplayAlert("Promociones cercanas", $"Se encontraron {_promosCercanasCache.Count} promociones cercanas, pero no se pudo navegar automáticamente al mapa. Revisa la configuración de Tabs/Shell.");
            }
            catch (Exception ex)
            {
                await SafeDisplayAlert("Error", $"No se pudo mostrar promociones: {ex.Message}");
            }
        }

        }
}
