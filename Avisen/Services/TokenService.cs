using Microsoft.Maui.Storage;

namespace Avisen.Services
{
    public class TokenService
    {
        private readonly BindableObject _bindableObject;

        public TokenService(BindableObject bindableObject)
        {
            _bindableObject = bindableObject; // Necesitamos un objeto que tenga Dispatcher
        }

        public async Task SetAccessTokenAsync(string accessToken, TimeSpan expiration)
        {
            await SecureStorage.SetAsync("AccessToken", accessToken);

            // Programar eliminación en 15 minutos usando Dispatcher
            _bindableObject.Dispatcher.StartTimer(expiration, () =>
            {
                SecureStorage.Remove("AccessToken");
                return false; // El temporizador no se reinicia
            });
        }

        public async Task SetRefreshTokenAsync(string refreshToken, TimeSpan expiration)
        {
            await SecureStorage.SetAsync("RefreshToken", refreshToken);

            // Programar eliminación en 7 días usando Dispatcher
            _bindableObject.Dispatcher.StartTimer(expiration, () =>
            {
                SecureStorage.Remove("RefreshToken");
                return false; // El temporizador no se reinicia
            });
        }

        public async Task<string> GetAccessTokenAsync()
        {
            return await SecureStorage.GetAsync("AccessToken");
        }

        public async Task<string> GetRefreshTokenAsync()
        {
            return await SecureStorage.GetAsync("RefreshToken");
        }
    }
}
