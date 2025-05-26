using Avisen.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Sharpnado.MaterialFrame;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Avisen
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSharpnadoMaterialFrame(loggerEnable: false)
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("telephas.ttf", "Telephas");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcon");
                    fonts.AddFont("FluentUI.ttf", "FluentUI");

                })
                .UseMauiMaps();

            builder.Services.AddSingleton<NegocioService, ApiService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
