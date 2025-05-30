using Parkrun_View.MVVM.Models;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.Services;
using System.Collections.ObjectModel;

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
            var data = DatabaseService.GetDataSync();
            if (data != null)
            {
                string parkrunnerName = string.Empty;
                if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
                {
                    parkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
                }
                var filteredData = data.Where(x => x.Name.ToLower() == parkrunnerName);

                chartViewModel.Data = new ObservableCollection<ParkrunData>(filteredData).ToList();
                chartViewModel.UpdateChartDimensions();
                chartViewModel.UpdateChart();
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