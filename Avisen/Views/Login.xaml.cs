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

        private async void CreateAccount_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SignUp());
        }

        private async void LoginHome_Clicked(object sender, EventArgs e)
        {
            try
            {
                // URL de la API
                var url = "https://napi-production.up.railway.app/api/usuario/login";

                // Crear el cliente HTTP
                using var httpClient = new HttpClient();

                // Crear el cuerpo de la solicitud (JSON)
                var jsonRequest = new
                {
                    email = "usuario@ejemplo.com",
                    contraseña = "contra123"
                };

                // Serializar el cuerpo a JSON
                var content = new StringContent(JsonSerializer.Serialize(jsonRequest), Encoding.UTF8, "application/json");

                // Enviar la solicitud POST
                var response = await httpClient.PostAsync(url, content);

                // Leer la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Validar y procesar la respuesta
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Mostrar el mensaje y el AccessToken
                    var message = jsonResponse.GetProperty("message").GetString();
                    var accessToken = jsonResponse.GetProperty("accessToken").GetString();

                    await DisplayAlert("Éxito", $"{message}\nAccessToken: {accessToken}", "OK");

                    // Navegar a la página principal
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
