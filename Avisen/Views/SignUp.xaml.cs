using System.Text.Json;
using Avisen.Services;

namespace Avisen.Views;

public partial class SignUp : ContentPage
{
    private readonly ApiService apiService = new ApiService();
    public SignUp()
    {
        InitializeComponent();
    }

    bool isPasswordSafe = false;
    bool areBothPasswordsTheSame = false;
    bool isEmailValid = false;
    bool isUserNameValid = false;

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

    private void IsEqualPassword()
    {
        if (string.IsNullOrEmpty(Password.Text) || string.IsNullOrEmpty(Password2.Text))
        {
            areBothPasswordsTheSame = false;
            return;
        }

        if (Password2.Text == Password.Text)
        {
            Message.TextColor = Color.FromArgb("#A3A3A4");
            Message.Text = "Crea tu cuenta!";
            Message.FontSize = 20;
            areBothPasswordsTheSame = true;
        }
        else
        {
            Message.TextColor = Color.FromArgb("#dc5a4b");
            Message.Text = "Las contraseñas no coinciden";
            Message.FontSize = 20;
            areBothPasswordsTheSame = false;
        }


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
            isPasswordSafe = false;
        }
        else
        {
            Message.TextColor = Color.FromArgb("#A3A3A4");
            Message.Text = "Crea tu cuenta!";
            Message.FontSize = 22;
            isPasswordSafe = true;
            IsEqualPassword();
        }
        buttonCreateAccountEnable();
    }

    private void Password2_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsEqualPassword();
        buttonCreateAccountEnable();
    }

    private void UserName_TextChanged(object sender, TextChangedEventArgs e)
    {
        isUserNameValid = !string.IsNullOrEmpty(UserName.Text);
        buttonCreateAccountEnable();
    }

    private void Email_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var emailEntry = sender as Entry;
            string email = e.NewTextValue;

            // Validar si el correo electrónico es válido usando DataAnnotations
            var atributoEmail = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!atributoEmail.IsValid(email))
            {
                Message.TextColor = Color.FromArgb("#dc5a4b");
                Message.Text = "El correo electrónico no es válido.";
                Message.FontSize = 20;
                isEmailValid = false;
            }
            else
            {
                Message.TextColor = Color.FromArgb("#A3A3A4");
                Message.Text = "Crea tu cuenta!";
                Message.FontSize = 22;
                isEmailValid = true;
            }
        }
        catch (Exception ex)
        {
            Message.TextColor = Color.FromArgb("#dc5a4b");
            Message.Text = "Error:" + ex;
        }
        finally
        {
            buttonCreateAccountEnable();
        }


    }

    private void buttonCreateAccountEnable()
    {
        if (isPasswordSafe && areBothPasswordsTheSame && isEmailValid && isUserNameValid)
        {
            CreateAccount.IsEnabled = true;
        }
        else
        {
            CreateAccount.IsEnabled = false;
        }
    }

    private async void CreateAccount_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Construir el cuerpo de la solicitud
            var jsonRequest = new
            {
                rol_idrol = 2, // Rol por defecto 2 (Cliente)
                email = Email.Text,
                contraseña = Password.Text,
                nombre = UserName.Text
            };

            // Consumir API
            var apiService = new ApiService();
            var response = await apiService.PostAsync("usuario", jsonRequest);

            // Manejo de respuesta
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                var message = jsonResponse.GetProperty("message").GetString();
                await DisplayAlert("Éxito", message, "OK");
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo registrar el usuario. Respuesta: {responseContent}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un error inesperado: {ex.Message}", "OK");
        }
        finally
        {
        }

    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        try
        {
            var currentPage = Navigation.NavigationStack.LastOrDefault();
            if (currentPage == null)
                throw new InvalidOperationException("No se pudo obtener la página actual.");

            await Navigation.PopAsync();
            Navigation.RemovePage(currentPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo navegar hacia atrás: {ex.Message}", "OK");
        }
    }
}