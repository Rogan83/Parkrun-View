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
            // Liste einiger verfügbaren Parkrun-Strecken in Deutschland. Es gibt 64 Standorte in Deutschland, die Parkrun-Veranstaltungen anbieten. Diese Liste ist nicht vollständig und kann erweitert werden.
            new TrackModel { TrackName = "Prießnitzgrund", FirstParkrunDate = new DateTime(2022, 7, 9), TrackNameURL = "priessnitzgrund" },
            new TrackModel { TrackName = "Oberwald", FirstParkrunDate = new DateTime(2020, 10, 25)},
            new TrackModel { TrackName = "Lahnwiesen", FirstParkrunDate = new DateTime(2020, 3, 7), TrackNameURL = "lahnwiesen" },
            new TrackModel { TrackName = "Küchenholz", FirstParkrunDate = new DateTime(2017, 12, 2), TrackNameURL = "kuechenholz" },
            new TrackModel { TrackName = "Kräherwald", FirstParkrunDate = new DateTime(2018, 5, 5), TrackNameURL = "kraeherwald" },
            new TrackModel { TrackName = "Westpark", FirstParkrunDate = new DateTime(2019, 1, 19), TrackNameURL = "westpark" },
            new TrackModel { TrackName = "Georgengarten", FirstParkrunDate = new DateTime(2017, 12, 2), TrackNameURL = "georgengarten" },
            new TrackModel { TrackName = "Hasenheide", FirstParkrunDate = new DateTime(2018, 1, 6), TrackNameURL = "hasenheide" },
            new TrackModel { TrackName = "Kiessee", FirstParkrunDate = new DateTime(2019, 5, 18), TrackNameURL = "kiessee" },
            new TrackModel { TrackName = "Aachener Weiher", FirstParkrunDate = new DateTime(2019, 3, 23), TrackNameURL = "aachenerweiher" },
            new TrackModel { TrackName = "Dietenbach", FirstParkrunDate = new DateTime(2021, 10, 23), TrackNameURL = "dietenbach" },
            new TrackModel { TrackName = "Ehrenbreitstein", FirstParkrunDate = new DateTime(2024, 1, 13), TrackNameURL = "ehrenbreitstein" },
            new TrackModel { TrackName = "Friedrichsau", FirstParkrunDate = new DateTime(2022, 3, 26), TrackNameURL = "friedrichsau" },
            new TrackModel { TrackName = "Krupunder See", FirstParkrunDate = new DateTime(2025, 1, 18), TrackNameURL = "krupundersee" },
            new TrackModel { TrackName = "Leinpfad", FirstParkrunDate = new DateTime(2019, 1, 5), TrackNameURL = "leinpfad" },
            new TrackModel { TrackName = "Lousberg", FirstParkrunDate = new DateTime(2024, 1, 20), TrackNameURL = "lousberg" },
            new TrackModel { TrackName = "Maaraue", FirstParkrunDate = new DateTime(2021, 10, 30), TrackNameURL = "maaraue" }

            // weitere Strecken können hier hinzugefügt werden
        };
    }
    //Alle Standorte
    //Aachener Weiher, Allerpark, Alstervorland, Bahnstadt Promenade, Bugasee, Dietenbach, Dreiländergarten, Ebenberg, Ehrenbreitstein, Emmerwiesen, Friedrichsau, Fuldaaue, Georgengarten, Globe, Grüner Weg, Hasenheide, Havelkanal, Hockgraben, Kastanienallee, Kemnader See, Kiessee, Kräherwald, Krupunder See, Küchenholz, Kurt-Schumacher-Promenade, Lahnwiesen, Landesgartenschau Park, Leinpfad, Lousberg, Luitpold, Maaraue, Mattheiser Weiher, Oberwald, Prießnitzgrund, Westpark, Aasee, Buga-Park, Bürgerpark, Eichenpark, Elbauenpark, Fasanerie, Flaucher, Gruga-Park, Hainpark, Havelpark, Heidepark, Herrenkrug, Hirschgarten, Isarauen, Johannapark, Killesberg, Klosterpark, Kurpark, Luitpoldhain, Mainufer, Maschpark, Neckarwiese, Nordpark, Olympiapark, Rheinpark, Schlosspark, Stadtpark, Tiergarten, Volkspark.

    public class TrackModel
    {
        public string TrackName { get; set; } = string.Empty; // der Name der Strecke, z.B. "Prießnitzgrund"
        public string TrackNameURL { get; set; } = string.Empty;     // der Name der Strecke, der in der URL verwendet wird, z.B. "priessnitzgrund" für "Prießnitzgrund", da in der URL keine Sonderzeichen vorkommen.
        public bool IsSelected { get; set; } // Bindung an CheckBox

        public DateTime FirstParkrunDate { get; set; }
    }
}
