
namespace Avisen.Services
{
    public static class NavigationService
    {
        public static Location? LocationToGo { get; set; }

        public static async Task AbrirNavegacion(Location destino)
        {
            var options = new MapLaunchOptions
            {
                NavigationMode = NavigationMode.Driving,
                Name = "Destino de la promoción"
            };

            await Map.OpenAsync(destino, options);
        }
    }


}