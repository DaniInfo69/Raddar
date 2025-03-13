using System.Text;
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

            var url = "https://napi-production.up.railway.app/api/usuario/logout";

            using var httpClient = new HttpClient();

            var jsonRequest = new
            {
                refreshToken = refreshToken
            };

            var content = new StringContent(
                JsonSerializer.Serialize(jsonRequest),
                Encoding.UTF8,
                "application/json");

            // Realizar la solicitud POST a la API
            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Éxito", "Sesión cerrada exitosamente.", "OK");

                // Eliminar todos los datos almacenados en SecureStorage
                SecureStorage.Remove("UserData");
                SecureStorage.Remove("AccessToken");
                SecureStorage.Remove("RefreshToken");

                // Redirigir al usuario a la página de inicio de sesión
                await Shell.Current.GoToAsync("//Login");
            }
            else
            {
                // Si la API devuelve un error, muestra el mensaje
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
