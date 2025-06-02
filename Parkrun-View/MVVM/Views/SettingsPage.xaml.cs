using Parkrun_View.MVVM.ViewModels;

namespace Parkrun_View.MVVM.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		BindingContext = new SettingsViewModel();
    }
}