using System.Text.Json;
using Avisen.Services;
namespace Avisen.Views;
public partial class EmailCode : ContentPage
{
    private readonly ApiService apiService = new ApiService(); // Servicio para manejo de API
    private string _email;
    public EmailCode(string Email)
    {
        InitializeComponent();
        _email = Email;
    }
    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        var currentEntry = sender as Entry;

        if (currentEntry != null)
        {
            // Si el usuario ha ingresado un número, mueve el foco al siguiente Entry
            if (!string.IsNullOrEmpty(currentEntry.Text) && currentEntry.Text.Length == 1)
            {
                MoverFocoAdelante(currentEntry);
            }
        }
    }

    private void OnEntryCompleted(object sender, EventArgs e)
    {
        var currentEntry = sender as Entry;
        if (currentEntry != null)
        {
            MoverFocoAdelante(currentEntry);
        }
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        var currentEntry = sender as Entry;
        if (currentEntry != null && string.IsNullOrEmpty(currentEntry.Text))
        {
            MoverFocoAtras(currentEntry);
        }
    }

    private void MoverFocoAdelante(Entry currentEntry)
    {
        if (currentEntry == Entry1) Entry2.Focus();
        else if (currentEntry == Entry2) Entry3.Focus();
        else if (currentEntry == Entry3) Entry4.Focus();
        else if (currentEntry == Entry4) Entry5.Focus();
        else if (currentEntry == Entry5) Entry6.Focus();
    }

    private void MoverFocoAtras(Entry currentEntry)
    {
        if (currentEntry == Entry6) Entry5.Focus();
        else if (currentEntry == Entry5) Entry4.Focus();
        else if (currentEntry == Entry4) Entry3.Focus();
        else if (currentEntry == Entry3) Entry2.Focus();
        else if (currentEntry == Entry2) Entry1.Focus();
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

    private async void NewPassword_Clicked(object sender, EventArgs e)
    {
        try
        {
            activateLoading();
            string code = Entry1.Text + Entry2.Text + Entry3.Text + Entry4.Text + Entry5.Text + Entry6.Text;
            Console.WriteLine($"El codigo recibido es: {code}");
            var jsonRequest = new
            {
                code
            };

            var response = await apiService.PostAsync("verificarcode", jsonRequest);
            if (response.IsSuccessStatusCode)
            {

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

                bool success = jsonResponse.GetProperty("success").GetBoolean();

                if (success)
                {
                    await Navigation.PushAsync(new ChangePassword(code));
                }
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

    private async void GoBack_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ResendEmail_Clicked(object sender, EventArgs e)
    {
        Console.WriteLine($"El email recibido es: {_email}");
    }
}
