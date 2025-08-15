namespace Avisen.Controls;

public partial class LoadingOverlay : ContentView
{
    public LoadingOverlay()
    {
        InitializeComponent();
        this.Opacity = 0;
    }

    public async Task ShowAsync()
    {
        if (!IsVisible)
        {
            IsVisible = true;
            await this.FadeTo(1, 200, Easing.CubicIn);
        }
    }

    public async Task HideAsync()
    {
        if (IsVisible)
        {
            await this.FadeTo(0, 200, Easing.CubicOut);
            IsVisible = false;
        }
    }
}