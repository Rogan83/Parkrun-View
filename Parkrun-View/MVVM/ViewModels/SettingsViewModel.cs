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
        public ObservableCollection<TrackModel> AvailableTracks { get; } = new()
        {
            new TrackModel { Name = "Prießnitzgrund" },
            new TrackModel { Name = "Oberwald" },
        };

        public ICommand GoBack { get; } 

        public SettingsViewModel()
        {
            // Initialisierung der Befehle
            GoBack = new Command(async() =>
            {
                SaveSettings(); // Speichern der Einstellungen
                await Shell.Current.GoToAsync("//ParkrunPage");
            
            });
            
            LoadSettings();     // Laden der Einstellungen
        }

        // Speichern der Einstellungen
        void SaveSettings()
        {
            var selectedTracks = AvailableTracks.Where(t => t.IsSelected).Select(t => t.Name).ToList();
            Preferences.Set("SelectedTracks", string.Join(",", selectedTracks));
        }

        // Laden der Einstellungen
        void LoadSettings()
        {
            var savedTracks = Preferences.Get("SelectedTracks", "").Split(',');

            foreach (var track in AvailableTracks)
            {
                track.IsSelected = savedTracks.Contains(track.Name);
            }
        }
    }

    public class TrackModel
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; } // Bindung an CheckBox
    }
}
