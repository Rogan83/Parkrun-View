using Parkrun_View.MVVM.Models;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace Parkrun_View.MVVM.Views;

public partial class ChartPage : ContentPage
{
	public ChartPage()
	{
		InitializeComponent();
        BindingContext = new ChartViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDataSync();
    }

    private void LoadDataSync()
    {
        if (BindingContext is ChartViewModel chartViewModel)
        {
            chartViewModel.FontSize = Preferences.Get("selectedFontSize", 16.0); // Schriftgröße aus den Einstellungen laden

            var data = DatabaseService.GetDataSync();
            if (data != null)
            {
                string parkrunnerName = string.Empty;
                if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
                {
                    parkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
                }

                // Filter nun die Daten von allen Standorten, die in den Einstellungen ausgewählt wurden
                string[] tracks = Preferences.Get("SelectedTracks", string.Empty).Split(",");
                List<ParkrunData> filteredDataByTrack = new List<ParkrunData>();
                foreach (var track in tracks)
                {
                    var temp = data.Where(x => x.TrackName.ToLower() == track.ToLower());
                    filteredDataByTrack.AddRange(temp);
                }

                // Filter nur die Daten vom Namen des Parkrunners heraus
                var filteredData = filteredDataByTrack.Where(x => x.Name.ToLower() == parkrunnerName);

                chartViewModel.DataPeriod = chartViewModel.Data = new ObservableCollection<ParkrunData>(filteredData).ToList();
                //chartViewModel.UpdateChartDimensions();
                chartViewModel.UpdateChart();
                chartViewModel.InitPeriod();
                chartViewModel.SetContentVisibility();
            }
        }
    }

    //void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    //{
    //    if (e.Status == GestureStatus.Running)
    //    {
    //        var view = (View)sender;
    //        double newScale = Math.Max(1, Math.Min(3, view.Scale * e.Scale)); // Zoom zwischen 1x und 3x begrenzen
    //        view.Scale = newScale;
    //    }
    //}
}