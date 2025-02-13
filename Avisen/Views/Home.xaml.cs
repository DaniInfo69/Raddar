using System.Collections.ObjectModel;

namespace Avisen.Views;

public partial class Home : ContentPage
{
    public ObservableCollection<Negocio> OfertasReales { get; set; }

    public Home()
    {
        InitializeComponent();
        OfertasReales = new ObservableCollection<Negocio>(Map.OfertasVistas);
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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

}