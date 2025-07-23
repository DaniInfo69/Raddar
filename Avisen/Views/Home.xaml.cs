using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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
        private readonly Stopwatch _loadStopwatch = new();
        private bool _isInitialized;
        private Categoria _categoriaSeleccionada;
        private IDispatcherTimer _carouselTimer;
        private CancellationTokenSource _cts;
        private readonly NegocioService _negocioService;
        private List<Promocion> _todasLasPromosCache = new();
        private bool _isRefreshing;

        // Colecciones optimizadas
        public OptimizedObservableCollection<Promocion> OfertasReales { get; } = new();
        public OptimizedObservableCollection<Categoria> Categorias { get; } = new();
        public OptimizedObservableCollection<Promocion> OfertasDestacadas { get; } = new();

        public ICommand TapCommand { get; }
        public ICommand RefreshCommand { get; }

        public Categoria CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                if (_categoriaSeleccionada == value) return;

                // Actualización sin notificar cambios hasta terminar
                OfertasReales.BeginBatchUpdate();

                try
                {
                    if (_categoriaSeleccionada != null)
                        _categoriaSeleccionada.IsSelected = false;

                    _categoriaSeleccionada = value;

                    if (_categoriaSeleccionada != null)
                    {
                        _categoriaSeleccionada.IsSelected = true;
                        FiltrarOfertasPorCategoria(_categoriaSeleccionada);
                    }

                    OnPropertyChanged();
                }
                finally
                {
                    OfertasReales.EndBatchUpdate();
                }
            }
        }

        public Home(NegocioService negocioService)
        {
            InitializeComponent();
            _negocioService = negocioService;
            _loadStopwatch.Start();

            // Comandos
            TapCommand = new Command<Promocion>(async (p) => await NavigateToDetalle(p));
            RefreshCommand = new Command(async () => await RefreshDataAsync(force: true));

            BindingContext = this;

            // Configuración inicial ligera
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

                // Carga paralela optimizada
                var loadTasks = new[]
                {
                    LoadUserNameAsync(_cts.Token),
                    LoadCategoriesAsync(_cts.Token),
                    LoadPromotionsAsync(_cts.Token)
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
                await LoadPromotionsAsync(_cts.Token);
                Debug.WriteLine($"Refresh completed in {_loadStopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException) { /* Ignorar cancelación */ }
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
            var negocios = await _negocioService.ObtenerMatricesConPromocionesAsync();

            ct.ThrowIfCancellationRequested();

            var nuevasPromos = negocios
                .Where(m => m.Promociones != null)
                .SelectMany(m => m.Promociones)
                .ToList();

            await CheckForNewPromotions(nuevasPromos);
            _todasLasPromosCache = nuevasPromos;

            await UpdateUI(nuevasPromos, position);
        }

        private async Task UpdateUI(List<Promocion> promociones, int carouselPosition)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Actualización por lotes para OfertasDestacadas
                OfertasDestacadas.ReplaceRange(promociones.Take(3));

                // Actualizar carrusel sin animación si es refresco
                if (OfertasDestacadas.Count > 0)
                {
                    var newPosition = Math.Min(carouselPosition, OfertasDestacadas.Count - 1);
                    HotOffersCarousel.ScrollTo(newPosition, animate: false);
                }

                // Aplicar filtro actual
                if (CategoriaSeleccionada != null)
                {
                    FiltrarOfertasPorCategoria(CategoriaSeleccionada);
                }

                // Feedback táctil optimizado
                SafeVibrate();
            });
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

        private void FiltrarOfertasPorCategoria(Categoria categoria)
        {
            var filtradas = categoria.idcategoria == -1
                ? _todasLasPromosCache
                : _todasLasPromosCache.Where(p => p.categoria_idcategoria == categoria.idcategoria).ToList();

            OfertasReales.ReplaceRange(filtradas);
        }

        private async Task CheckForNewPromotions(List<Promocion> nuevasPromos)
        {
            try
            {
                var promocionesJson = Preferences.Get("PromosGuardadas", null);
                if (string.IsNullOrEmpty(promocionesJson)) return;

                var promosGuardadas = JsonSerializer.Deserialize<List<Promocion>>(promocionesJson);
                if (new HashSet<int>(nuevasPromos.Select(p => p.idpromocion))
                    .SetEquals(promosGuardadas.Select(p => p.idpromocion)))
                {
                    return;
                }

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

                Preferences.Set("PromosGuardadas", JsonSerializer.Serialize(nuevasPromos));
            }
            catch { /* Silenciar errores de notificación */ }
        }

        private void StartTimers()
        {
            // Timer de actualización con intervalo configurable
            this.Dispatcher.StartTimer(TimeSpan.FromSeconds(
                Preferences.Get("UpdateFrequency", 20.0)), () =>
                {
                    if (!_isRefreshing)
                        _ = RefreshDataAsync();
                    return true;
                });

            // Timer de carrusel solo si hay elementos
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
            _cts?.Cancel();
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
                Preferences.Set("UpdateFrequency", 20.0);
                Preferences.Set("OfferDistance", 0.5);
            }
        }

        #endregion

        private async Task NavigateToDetalle(Promocion promocion)
        {
            if (promocion == null) return;

            try
            {
                var negocios = await _negocioService.ObtenerMatricesConPromocionesAsync();
                var matriz = negocios.FirstOrDefault(m =>
                    m.Promociones.Any(p => p.idpromocion == promocion.idpromocion));

                if (matriz == null)
                {
                    await SafeDisplayAlert("Error", "No se encontró la ubicación de la promoción.");
                    return;
                }

                await Navigation.PushAsync(new PromocionDetallesPage(promocion, matriz.Location));
            }
            catch (Exception ex)
            {
                await SafeDisplayAlert("Error", $"No se pudo navegar: {ex.Message}");
            }
        }
    }

    #region Clases de Soporte para Optimización

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

    #endregion
}