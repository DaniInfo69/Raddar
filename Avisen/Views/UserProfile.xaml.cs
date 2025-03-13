using System.Text.Json;

namespace Avisen.Views;

public partial class UserProfile : ContentPage
{
    public UserProfile()
    {
        InitializeComponent();
        BindingContext = this;
        LoadUserNameAsync();
    }

    private async void LoadUserNameAsync()
    {
        try
        {
            var userDataJson = await SecureStorage.GetAsync("UserData");

            if (!string.IsNullOrEmpty(userDataJson))
            {
                await DisplayAlert("UserData JSON", userDataJson, "OK");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var userData = JsonSerializer.Deserialize<UserData>(userDataJson, options);

                if (userData != null)
                {
                    lblUserName.Text = userData.NombreCliente ?? "Nombre no disponible";
                }
                else
                {
                    lblUserName.Text = "Error al deserializar datos.";
                }
            }
            else
            {
                lblUserName.Text = "No se encontró información del usuario.";
            }
        }
        catch (Exception ex)
        {
            lblUserName.Text = "Error al cargar el nombre";
            Console.WriteLine($"Error: {ex.Message}");
            await DisplayAlert("Error", $"Detalles: {ex.Message}", "OK");
        }
    }
}

// Modelo para deserializar los datos del usuario
public class UserData
{
    public int IdUsuario { get; set; }
    public string Email { get; set; }
    public string NombreCliente { get; set; }
    public int RolIdRol { get; set; }
    public string Rol { get; set; }
}
