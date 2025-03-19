using System.Text;
using System.Text.Json;
using Avisen.Services;

namespace Avisen.Views;

public partial class UserProfile : ContentPage
{
    private readonly ApiService apiService = new ApiService();

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

    private async void ExitSession_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Obtén el refreshToken de SecureStorage
            var refreshToken = await SecureStorage.GetAsync("RefreshToken");

            if (string.IsNullOrEmpty(refreshToken))
            {
                await DisplayAlert("Error", "No se encontró el RefreshToken. No se puede cerrar sesión.", "OK");
                return;
            }

            // Crear la solicitud a la API para cerrar sesión
            var jsonRequest = new { refreshToken = refreshToken };
            var response = await apiService.PostAsync("usuario/logout", jsonRequest);

            if (response.IsSuccessStatusCode)
            {
                // Éxito: cerrar sesión y limpiar almacenamiento seguro
                await DisplayAlert("Éxito", "Sesión cerrada exitosamente.", "OK");

                SecureStorage.Remove("UserData");
                SecureStorage.Remove("AccessToken");
                SecureStorage.Remove("RefreshToken");

                // Redirigir al usuario a la pantalla de inicio de sesión
                await Shell.Current.GoToAsync("//Login");
            }
            else
            {
                // Si la API devuelve un error
                var responseContent = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo cerrar sesión.\nRespuesta: {responseContent}", "OK");
            }
        }
        catch (Exception ex)
        {
            // Manejo de errores
            Console.WriteLine($"Error al cerrar sesión: {ex.Message}");
            await DisplayAlert("Error", "Hubo un problema al cerrar sesión. Intenta nuevamente.", "OK");
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