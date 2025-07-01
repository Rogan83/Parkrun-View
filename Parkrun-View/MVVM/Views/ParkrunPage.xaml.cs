using Parkrun_View.MVVM.Helpers;
using Parkrun_View.MVVM.Models;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.Services;
using System.Collections.ObjectModel;

namespace Parkrun_View.MVVM.Views;

public partial class ParkrunPage : ContentPage
{
	public ParkrunPage()
	{
		InitializeComponent();
        BindingContext = new ParkrunViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ = LoadDataAsync(); // bewusst ignoriert, aber async sauber
    }

    private async Task LoadDataAsync()
    {
        if (BindingContext is ParkrunViewModel parkrunViewModel)
        {
            parkrunViewModel.FontSize = Preferences.Get("selectedFontSize", 16.0); // Schriftgröße aus den Einstellungen laden
            parkrunViewModel.Data = new ObservableCollection<ParkrunData>();

            await parkrunViewModel.LoadDataAsync();
            parkrunViewModel.SetContentVisibility();
        }
    }
}