namespace Avisen.Views;

public partial class SignUp : ContentPage
{
	public SignUp()
	{
		InitializeComponent();
	}

    private async void Back_Clicked(object sender, EventArgs e)
    {
        // Obtener la página actual
        var currentPage = Navigation.NavigationStack.LastOrDefault();

        // Navegar hacia atrás
        await Navigation.PopAsync();

        // Eliminar la página actual de la pila de navegación
        if (currentPage != null)
        {
            Navigation.RemovePage(currentPage);
        }
    }

}