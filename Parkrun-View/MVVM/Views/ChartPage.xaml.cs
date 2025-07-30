using Parkrun_View.MVVM.Helpers;
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
            chartViewModel.DataPeriod = chartViewModel.Data = NavigationHelper.Data.ToList(); // Verweis auf die Daten, die von der Datenbank geladen wurden. Wird in der NavigationHelper-Klasse gespeichert, um von anderen ViewModels darauf zuzugreifen.

            chartViewModel.InitPeriod();
            chartViewModel.UpdateChart();
            chartViewModel.SetContentVisibility();
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