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
}