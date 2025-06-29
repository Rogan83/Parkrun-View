using Parkrun_View.MVVM.Models;
using Parkrun_View.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Parkrun_View.MVVM.Helpers
{
    internal static class NavigationHelper
    {
        public static string LastPageRoute { get; set; } = String.Empty; // Speichert die Route der letzten Seite, um später dorthin zurückzukehren

        public static ObservableCollection<ParkrunData> Data { get; set; } = new(); // Speichert ALLE Daten von der Datenbank, welche von den anderen ViewModel Klassen verwendet werden können
        public static ObservableCollection<ParkrunData> DataPeriod { get; set; } = new(); // Speichert nur die Daten von der Datenbank von einem bestimmten Zeitraum, welche von den anderen ViewModel Klassen verwendet werden können



        public static void LoadFilteredParkrunDataSync()
        {
            var data = DatabaseService.GetDataSync();
            if (data != null)
            {
                // Hol die gespeicherten Track-Namen aus den Einstellungen
                var selectedTracks = Preferences.Get("SelectedTracks", string.Empty)
                                                .Split(',')
                                                .Select(t => t.Trim())
                                                .ToList();

                // Suche nach dem gespeicherten Parkrunner-Namen
                string parkrunnerName = string.Empty;
                if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
                {
                    parkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
                }

                // Filtere die Daten nach Parkrunner-Name UND nach den ausgewählten Tracks
                var filteredData = data.Where(x => x.Name.ToLower() == parkrunnerName
                                                 && selectedTracks.Contains(x.TrackName))
                                       .OrderBy(x => x.Date);

                Data = new System.Collections.ObjectModel.ObservableCollection<ParkrunData>(filteredData); // Speichert die gefilterten Daten in der Data-Collection, die von anderen ViewModels verwendet werden kann.
            }
        }

        public static async Task LoadFilteredParkrunDataAsync()
        {
            var data = await DatabaseService.GetDataAsync(); // Async-Version vorausgesetzt
            if (data != null)
            {
                var selectedTracks = Preferences
                    .Get("SelectedTracks", string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

                var parkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);

                var filteredData = data
                    .Where(x => x.Name.Equals(parkrunnerName, StringComparison.OrdinalIgnoreCase)
                             && selectedTracks.Contains(x.TrackName))
                    .OrderBy(x => x.Date);

                Data = new ObservableCollection<ParkrunData>(filteredData);
            }
        }



        public static ICommand GoToSettingsCommand { get; } = new Command(async () =>
        {
            NavigationHelper.LastPageRoute = Shell.Current.CurrentState.Location.ToString();
            await Shell.Current.GoToAsync("//SettingsPage");
        });
    }
}
