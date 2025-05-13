using System.Text.Json;
using Avisen.Services;
using Avisen.Models;

namespace Avisen.Views;

public partial class SendEmail : ContentPage
{

    private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API

    public SendEmail()
    {
        InitializeComponent();
    }

    private async void SendUserEmail_Clicked(object sender, EventArgs e)
    {
        activateLoading();
        var jsonRequest = new
        {
            email = EmailEntry.Text
        };

        var response = await apiService.PostAsync("resetpassword-request-code", jsonRequest);

        if (response.IsSuccessStatusCode)
        {

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

            bool success = jsonResponse.GetProperty("success").GetBoolean();

            if (success)
            {
                await DisplayAlert("Bien", "siguiente pagina", "OK");
                await Navigation.PushAsync(new EmailCode(EmailEntry.Text));
            }
        }
        desactivateLoading();
    }

    private async void ReturnLogin_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
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