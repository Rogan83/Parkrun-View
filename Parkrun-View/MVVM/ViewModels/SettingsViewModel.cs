using Parkrun_View.MVVM.Helpers;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Parkrun_View.MVVM.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsViewModel
    {
        public ObservableCollection<TrackModel> AvailableTracks { get; } = new();

        public ICommand GoBack { get; } 

        public SettingsViewModel()
        {
            AvailableTracks = ParkrunTracks.AvailableTracks; // Verweis auf die verfügbaren Strecken in ParkrunTracks
            // Initialisierung der Befehle
            GoBack = new Command(async() =>
            {
                SaveSettings(); // Speichern der Einstellungen

                if (!string.IsNullOrEmpty(NavigationHelper.LastPageRoute))
                {
                    await Shell.Current.GoToAsync(NavigationHelper.LastPageRoute);
                }
                else
                {
                    await Shell.Current.GoToAsync("//ParkrunPage"); // Falls keine vorige Seite bekannt ist
                }
            });
            
            LoadSettings();     // Laden der Einstellungen
        }

        // Speichern der Einstellungen
        void SaveSettings()
        {
            var selectedTracks = AvailableTracks.Where(t => t.IsSelected)
                .Select(t => string.IsNullOrEmpty(t.TrackNameURL) ? t.TrackName.ToLower() : t.TrackNameURL).ToList();
            string selectedTracksString = selectedTracks.Count > 0 ? string.Join(",", selectedTracks) : string.Empty;
            Preferences.Set("SelectedTracks", selectedTracksString);
        }

        // Laden der Einstellungen
        void LoadSettings()
        {
            var savedTracks = Preferences.Get("SelectedTracks", "").Split(',');

            foreach (var track in AvailableTracks)
            {
                track.IsSelected = savedTracks.Contains(track.TrackName.ToLower()) || savedTracks.Contains(track.TrackNameURL);
            }
        }
    }
}
