using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkrun_View.MVVM.Helpers
{
    public static class ParkrunTracks
    {
        public static ObservableCollection<TrackModel> AvailableTracks { get; } = new()
        {
            new TrackModel { TrackName = "Prießnitzgrund", FirstParkrunDate = new DateTime(2022, 7, 9), TrackNameURL = "priessnitzgrund" },
            new TrackModel { TrackName = "Oberwald", FirstParkrunDate = new DateTime(2020, 10, 25)},
            // weitere Strecken können hier hinzugefügt werden
        };
    }

    public class TrackModel
    {
        public string TrackName { get; set; } = string.Empty; // der Name der Strecke, z.B. "Prießnitzgrund"
        public string TrackNameURL { get; set; } = string.Empty;     // der Name der Strecke, der in der URL verwendet wird, z.B. "priessnitzgrund" für "Prießnitzgrund", da in der URL keine Sonderzeichen vorkommen.
        public bool IsSelected { get; set; } // Bindung an CheckBox

        public DateTime FirstParkrunDate { get; set; }
    }
}
