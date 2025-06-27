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

        private string selectedFontSize = "Mittel";
        public string SelectedFontSize
        {
            get => selectedFontSize;
            set
            {
                selectedFontSize = value;
                FontSize = selectedFontSize switch
                {
                    "Klein" => 12.0,
                    "Mittel" => 16.0,
                    "Groß" => 20.0,
                    _ => 16.0 // Standardwert
                };
                
                // Speichern der ausgewählten Schriftgröße in den Einstellungen

                Preferences.Set("selectedFontSize", FontSize);
            }
        }

        public double FontSize { get; set; } = 16; 
        public List<string> FontSizes { get; set; }

        public bool isSaveSettings { get; set; } = false;

        public ICommand GoBack { get; } 

        public SettingsViewModel()
        {
            var temp = Preferences.Get("selectedFontSize", "Mittel");
            SelectedFontSize = temp switch
            {
                "12" => "Klein",
                "16" => "Mittel",
                "20" => "Groß",
                _ => "Mittel" // Standardwert
            }; // Laden der gespeicherten Schriftgröße oder Standardwert "Mittel"

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

            FontSizes = new List<string>
            {
                "Klein",
                "Mittel",
                "Groß"
            };

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
