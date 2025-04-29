using Avisen.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Avisen.Views
{
    public partial class EditUserInfo : ContentPage, INotifyPropertyChanged
    {
        private string _nombre;
        public string Nombre
        {
            get => _nombre;
            set
            {
                _nombre = value;
                OnPropertyChanged();
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        private string _rol;
        public string Rol
        {
            get => _rol;
            set
            {
                _rol = value;
                OnPropertyChanged();
            }
        }

        public EditUserInfo()
        {
            InitializeComponent();
            BindingContext = this; 
            LoadUserInfo();
        }

        private async void CerrarModal(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void LoadUserInfo()
        {
            var userDataJson = await SecureStorage.GetAsync("UserData");

            if (!string.IsNullOrEmpty(userDataJson))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var userData = JsonSerializer.Deserialize<UserData>(userDataJson, options);
                Nombre = userData?.NombreCliente ?? "";
                Email = userData?.Email ?? "";
                Rol = userData?.Rol ?? "";
            }
            else
            {
                await DisplayAlert("Advertencia", "No se encontró información del usuario en SecureStorage.", "OK");
            }
        }

        // Método necesario para que el XAML detecte los cambios
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
