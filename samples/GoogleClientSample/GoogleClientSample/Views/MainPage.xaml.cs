using GoogleClientSample.ViewModels;

namespace GoogleClientSample.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = new MainPageViewModel();
	}
}

