using System.Text.Json;
using Avisen.Services;
namespace Avisen.Views;

public partial class ChangePassword : ContentPage
{
    private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API
    private string _code;

    bool isPasswordSafe = false;
    bool areBothPasswordsTheSame = false;
    public ChangePassword(string code)
	{
		InitializeComponent();
        _code = code;
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
        buttonSendNewPasswordEnable();
    }

    private void Password2_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsEqualPassword();
        buttonSendNewPasswordEnable();
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

    private void buttonSendNewPasswordEnable()
    {
        if (isPasswordSafe && areBothPasswordsTheSame)
        {
            SendNewPassword.IsEnabled = true;
        }
        else
        {
            SendNewPassword.IsEnabled = false;
        }
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Login");
    }

    private async void SendNewPassword_Clicked(object sender, EventArgs e)
    {
        try
        {
            activateLoading();

            var jsonRequest = new
            {
                token = _code,
                newPassword = Password.Text
            };
            Console.WriteLine("Empieza");
            var response = await apiService.PostAsync("reset-password-code", jsonRequest);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Success");
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

                bool success = jsonResponse.GetProperty("success").GetBoolean();
                Console.WriteLine("Cambia a Login");
                await Shell.Current.GoToAsync("//Login");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            desactivateLoading();
        }
    }

    private void activateLoading()
    {
        Overlay.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
    }

    private void desactivateLoading()
    {
        Overlay.IsVisible = false;
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
    }
}