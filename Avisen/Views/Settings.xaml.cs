namespace Avisen.Views;

public partial class Settings : ContentPage
{
    public Settings()
    {
        InitializeComponent();
        BindingContext = this;
        IsDarkMode = Preferences.Get("IsDarkMode", false);
        OfferDistance = Preferences.Get("OfferDistance", 0.0);
    }

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            OnPropertyChanged();
            Preferences.Set("IsDarkMode", value);
        }
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

    private void OnSaveOfferDistanceClicked(object sender, EventArgs e)
    {
        Preferences.Set("OfferDistance", OfferDistance);
        DisplayAlert("Guardado", "La distancia entre ofertas ha sido guardada.", "OK");
    }
}
