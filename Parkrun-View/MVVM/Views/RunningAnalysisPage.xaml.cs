using Parkrun_View.MVVM.Models;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.Services;

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
            var data = DatabaseService.GetDataSync();
            if (data != null)
            {
                string parkrunnerName = string.Empty;
                if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
                {
                    parkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
                }
                var filteredData = data.Where(x => x.Name.ToLower() == parkrunnerName);

                runningAnalysisViewModel.Data = filteredData.ToList();

                runningAnalysisViewModel.SelectedRun = runningAnalysisViewModel.Data.FirstOrDefault() ?? new ParkrunData();
            }

            runningAnalysisViewModel.CalculateStatistics();

            runningAnalysisViewModel.SetContentVisibility();
        }
    }
}