using Parkrun_View.MVVM.Models;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.Services;

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

        // Daten neu laden
        LoadDataSync();
    }

    private void LoadDataSync()
    {
        if (BindingContext is ParkrunViewModel parkrunViewModel)
        {
            parkrunViewModel.FontSize = Preferences.Get("selectedFontSize", 16.0); // Schriftgröße aus den Einstellungen laden
            var data = DatabaseService.GetDataSync();
            if (data != null)
            {
                // Hol die gespeicherten Track-Namen aus den Einstellungen
                var selectedTracks = Preferences.Get("SelectedTracks", string.Empty)
                                                .Split(',')
                                                .Select(t => t.Trim())
                                                .ToList();

                // Filtere die Daten nach Parkrunner-Name UND nach den ausgewählten Tracks
                var filteredData = data.Where(x => x.Name.ToLower() == parkrunViewModel.ParkrunnerName
                                                 && selectedTracks.Contains(x.TrackName))
                                       .OrderBy(x => x.Date);

                parkrunViewModel.Data = new System.Collections.ObjectModel.ObservableCollection<ParkrunData>(filteredData);

                parkrunViewModel.SetContentVisibility();
            }
        }
    }
}