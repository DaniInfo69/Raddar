namespace Avisen.Views;

public partial class SignUp : ContentPage
{
	public SignUp()
	{
		InitializeComponent();
	}

    private async void Back_Clicked(object sender, EventArgs e)
    {
        // Obtener la p�gina actual
        var currentPage = Navigation.NavigationStack.LastOrDefault();

        // Navegar hacia atr�s
        await Navigation.PopAsync();

        // Eliminar la p�gina actual de la pila de navegaci�n
        if (currentPage != null)
        {
            Navigation.RemovePage(currentPage);
        }
    }

}