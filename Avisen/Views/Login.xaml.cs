using System.Text.Json;
using Avisen.Services;
using Avisen.Models;

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
                Overlay.IsVisible = true;
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                //LabelLoading.Rotation = 0;
                var existingAccessToken = await tokenService.GetAccessTokenAsync();
                var refreshToken = await tokenService.GetRefreshTokenAsync();
                //await LabelLoading.ScaleTo(1.7, 1000, Easing.BounceIn);
                //await LabelLoading.RotateTo(180, 1200, Easing.CubicInOut);

                if (!string.IsNullOrEmpty(existingAccessToken))
                {
                    await Shell.Current.GoToAsync("//Home");
                    return;
                }

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                    {
                        await DisplayAlert("Sin conexión", "No hay acceso a Internet. Verifica tu conexión.", "OK");
                        await Shell.Current.GoToAsync("//Home");
                    }
                    else
                    {
                        var jsonRequest = new { refreshToken = refreshToken };
                        var response = await apiService.PostAsync("usuario/refresh-token", jsonRequest);

                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                            var newAccessToken = jsonResponse.GetProperty("accessToken").GetString();

                            await tokenService.SetAccessTokenAsync(newAccessToken, TimeSpan.FromMinutes(15));
                            await Shell.Current.GoToAsync("//Home");
                        }
                        else
                        {
                            await DisplayAlert("Error", "Revise su conexion a internet o intente mas tarde.", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                Overlay.IsVisible = false;
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;

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
                Overlay.IsVisible = true;
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;                
                //LabelLoading.Rotation = 0; // Asegúrate de que comience desde 0

                var jsonRequest = new
                {
                    email = "dajosolmag1@gmail.com", // Esto debería venir de entradas de usuario
                    password = "Qq12345."       // Esto también debe provenir de entradas
                };

                var response = await apiService.PostAsync("usuario/login", jsonRequest);

                if (response.IsSuccessStatusCode)
                {
                    //LabelLoading.ScaleTo(1.7, 1200, Easing.BounceIn);
                    //await LabelLoading.RotateTo(180, 1500, Easing.CubicInOut);
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

                    // Guardar tokens
                    var accessToken = jsonResponse.GetProperty("accessToken").GetString();
                    await tokenService.SetAccessTokenAsync(accessToken, TimeSpan.FromMinutes(15));

                    var refreshToken = jsonResponse.GetProperty("refreshToken").GetString();
                    await tokenService.SetRefreshTokenAsync(refreshToken, TimeSpan.FromDays(7));

                    // Guardar datos del usuario
                    var user = jsonResponse.GetProperty("user");
                    var userData = new UserData
                    {
                        IdUsuario = user.GetProperty("idusuario").GetInt32(),
                        Email = user.GetProperty("email").GetString(),
                        NombreCliente = user.GetProperty("nombrecliente").GetString(),
                        RolIdRol = user.GetProperty("rol_idrol").GetInt32(),
                        Rol = user.GetProperty("rol").GetString()
                    };
                    await SecureStorage.SetAsync("UserData", JsonSerializer.Serialize(userData));

                    // Navegar a Home
                    await Shell.Current.GoToAsync("//Home");
                }
                else
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    bool success = jsonResponse.GetProperty("success").GetBoolean();
                    bool emailExists = jsonResponse.GetProperty("emailExists").GetBoolean();
                    bool pending = jsonResponse.GetProperty("pending").GetBoolean();
                    if (!success && emailExists)
                    {
                        await DisplayAlert("Mal", "La contraseña es incorrecta", "OK");
                    }
                    else if (!success && emailExists && pending)
                    {
                        await DisplayAlert("Pendiente", "Su cuenta está esta pendiente de activación, revise su correo, de no encontrarlo revise el spam", "OK");
                    }
                    else if (!success && !emailExists && !pending)
                    {
                        await DisplayAlert("Pendiente", "Su cuenta está pendiente de aprobación", "OK");
                    }
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error en el login.\nResponse: {responseContent}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                Overlay.IsVisible = false;
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
    }
}