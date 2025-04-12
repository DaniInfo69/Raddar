namespace Avisen.Views;

public partial class EditUserInfo : ContentPage
{

	public EditUserInfo()
	{
		InitializeComponent();
	}

    private async void CerrarModal(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

}