using System.Text;
using System.Text.Json;
using Avisen.Services;
using Microsoft.Maui.Storage;

namespace Avisen.Views
{
    public partial class Login : ContentPage
    {
        private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API
        private readonly TokenService tokenService; // Servicio para manejo de tokens

        public Login()
        {
            InitializeComponent();
            tokenService = new TokenService(this); // Pasar el contexto actual para usar Dispatcher
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Recuperar tokens almacenados
                var existingAccessToken = await tokenService.GetAccessTokenAsync();
                var refreshToken = await tokenService.GetRefreshTokenAsync();

                if (!string.IsNullOrEmpty(existingAccessToken) && !string.IsNullOrEmpty(refreshToken))
                {
                    // Refrescar el token si es necesario
                    var jsonRequest = new { refreshToken = refreshToken };
                    var response = await apiService.PostAsync("refresh-token", jsonRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        var newAccessToken = jsonResponse.GetProperty("accessToken").GetString();

                        // Guardar el nuevo AccessToken
                        await tokenService.SetAccessTokenAsync(newAccessToken, TimeSpan.FromMinutes(15));

                        // Navegar a Home
                        await Shell.Current.GoToAsync("//Home");
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo refrescar el token. Por favor, inicie sesión nuevamente.", "OK");
                    }
                }
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
                var jsonRequest = new
                {
                    email = "usuario@ejemplo.com", // Esto debería venir de entradas de usuario
                    contraseña = "contra123"       // Esto también debe provenir de entradas
                };

                var response = await apiService.PostAsync("login", jsonRequest);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

                    // Guardar tokens
                    var accessToken = jsonResponse.GetProperty("accessToken").GetString();
                    await tokenService.SetAccessTokenAsync(accessToken, TimeSpan.FromMinutes(15));

                    var refreshToken = jsonResponse.GetProperty("refreshToken").GetString();
                    await tokenService.SetRefreshTokenAsync(refreshToken, TimeSpan.FromDays(7));

                    // Guardar datos del usuario
                    var user = jsonResponse.GetProperty("user");
                    var userData = new
                    {
                        idUsuario = user.GetProperty("idusuario").GetInt32(),
                        email = user.GetProperty("email").GetString(),
                        nombreCliente = user.GetProperty("nombrecliente").GetString(),
                        rolIdRol = user.GetProperty("rol_idrol").GetInt32(),
                        rol = user.GetProperty("rol").GetString()
                    };
                    await SecureStorage.SetAsync("UserData", JsonSerializer.Serialize(userData));

                    // Navegar a Home
                    await Shell.Current.GoToAsync("//Home");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
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
