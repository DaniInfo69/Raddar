using Avisen.Views;

namespace Avisen
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Envolver la página principal en un NavigationPage
            //return new Window(new NavigationPage(new Login()));
            return new Window(new AppShell());
        }
    }
}
