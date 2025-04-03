namespace Avisen.Views;

public partial class SignUp : ContentPage
{
    public SignUp()
    {
        InitializeComponent();
    }

    private string IsSafePassword(string password)
    {
        if (password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        if (!password.Any(char.IsUpper))
            return "La contraseña debe contener al menos una letra mayúscula.";
        if (!password.Any(char.IsLower))
            return "La contraseña debe contener al menos una letra minúscula.";
        if (!password.Any(char.IsDigit))
            return "La contraseña debe contener al menos un número.";
        if (!password.Any(ch => "@$!%*?&.".Contains(ch)))
            return "La contraseña debe contener al menos un carácter especial (@$!%*?&.).";

        return string.Empty;


    }

    private void Password_TextChanged(object sender, TextChangedEventArgs e)
    {
        var passwordEntry = sender as Entry;
        string password = e.NewTextValue;

        string mensajeError = IsSafePassword(password);

        if (!string.IsNullOrEmpty(mensajeError))
        {
            Message.TextColor = Color.FromArgb("#dc5a4b");
            Message.Text = mensajeError;
            Message.FontSize = 20;
        }
        else
        {
            Message.TextColor = Color.FromArgb("#A3A3A4");
            Message.Text = "Crea tu cuenta!";
            Message.FontSize = 22;
        }

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