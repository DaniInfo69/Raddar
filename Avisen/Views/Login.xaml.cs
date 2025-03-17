using System.Text;
using System.Text.Json;

namespace Avisen.Views
{
    public partial class Login : ContentPage
    {
        public Login()
        {
            InitializeComponent();
        }

        protected async  override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Verificar si el AccessToken está almacenado
                var existingAccessToken = await SecureStorage.GetAsync("AccessToken");
                var refreshToken = await SecureStorage.GetAsync("RefreshToken");

                if (!string.IsNullOrEmpty(existingAccessToken) && !string.IsNullOrEmpty(refreshToken))
                {
                    // Si hay un token almacenado, solicitar un nuevo AccessToken usando la API
                    var url = "https://napi-production.up.railway.app/api/usuario/refresh-token";

                    using var httpClient = new HttpClient();
                    var jsonRequest = new
                    {
                        refreshToken = refreshToken
                    };

                    var content = new StringContent(JsonSerializer.Serialize(jsonRequest), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        // Guardar el nuevo AccessToken
                        var newAccessToken = jsonResponse.GetProperty("accessToken").GetString();
                        await SecureStorage.SetAsync("AccessToken", newAccessToken);

                        // Navegar a Home
                        await Shell.Current.GoToAsync("//Home");
                    }
                    else
                    {
                        // Manejar errores en la solicitud
                        await DisplayAlert("Error", "No se pudo refrescar el token. Por favor, inicie sesión nuevamente.", "OK");
                    }
                }
                // Si no hay tokens almacenados, no ocurre nada.
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void CreateAccount_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SignUp());
        }

        private async void LoginHome_Clicked(object sender, EventArgs e)
        {
            try
            {
                var url = "https://napi-production.up.railway.app/api/usuario/login";

                using var httpClient = new HttpClient();

                var jsonRequest = new
                {
                    email = "usuario@ejemplo.com",
                    contraseña = "contra123"
                };

                var content = new StringContent(JsonSerializer.Serialize(jsonRequest), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var accessToken = jsonResponse.GetProperty("accessToken").GetString();
                    await SecureStorage.SetAsync("AccessToken", accessToken);

                    var refreshToken = jsonResponse.GetProperty("refreshToken").GetString();
                    await SecureStorage.SetAsync("RefreshToken", refreshToken);
                    await DisplayAlert("AccessToken", accessToken, "OK");


                    var user = jsonResponse.GetProperty("user");
                    var userData = new
                    {
                        idUsuario = user.GetProperty("idusuario").GetInt32(),
                        email = user.GetProperty("email").GetString(),
                        nombreCliente = user.GetProperty("nombrecliente").GetString(),
                        rolIdRol = user.GetProperty("rol_idrol").GetInt32(),
                        rol = user.GetProperty("rol").GetString()
                    };
                    await DisplayAlert("Nombre", userData.nombreCliente, "OK");

                    await SecureStorage.SetAsync("UserData", JsonSerializer.Serialize(userData));
                    await DisplayAlert("UserData JSON", JsonSerializer.Serialize(userData), "OK");

                    await Shell.Current.GoToAsync("//Home");
                }
                else
                {
                    await DisplayAlert("Error", $"Error en el login.\nResponse: {responseContent}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
