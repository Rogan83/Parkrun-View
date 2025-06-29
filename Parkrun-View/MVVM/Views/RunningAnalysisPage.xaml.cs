using Parkrun_View.MVVM.Helpers;
using Parkrun_View.MVVM.Models;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.Services;
using System.Collections.ObjectModel;

namespace Parkrun_View.MVVM.Views;

public partial class RunningAnalysisPage : ContentPage
{
	public RunningAnalysisPage()
	{
		InitializeComponent();
        BindingContext = new RunningAnalysisViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDataSync();
    }

    private void LoadDataSync()
    {
        if (BindingContext is RunningAnalysisViewModel runningAnalysisViewModel)
        {
            runningAnalysisViewModel.FontSize = Preferences.Get("selectedFontSize", 16.0); // Schriftgröße aus den Einstellungen laden
            
            runningAnalysisViewModel.Data = NavigationHelper.Data.ToList(); // Verweis auf die Daten, die von der Datenbank geladen wurden. Wird in der NavigationHelper-Klasse gespeichert, um von anderen ViewModels darauf zuzugreifen.
            runningAnalysisViewModel.SelectedRun = runningAnalysisViewModel.Data.FirstOrDefault() ?? new ParkrunData();

            runningAnalysisViewModel.CalculateStatistics();
            runningAnalysisViewModel.SetContentVisibility();
        }
    }
}