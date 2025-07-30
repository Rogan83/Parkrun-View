using HtmlAgilityPack;
using Microsoft.Maui.Devices.Sensors;
using Parkrun_View.MVVM.Helpers;
using Parkrun_View.MVVM.Interfaces;
using Parkrun_View.MVVM.Models;
using Parkrun_View.Services;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Xml;

namespace Parkrun_View.MVVM.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class ParkrunViewModel : ILoadableViewModel
    {
        #region Properties and Fields

        public ObservableCollection<ParkrunData> Data { get; set; } = new();

        public ObservableCollection<string> SuggestionList { get; set; } = new();
        

        List<ParkrunData> allData = new List<ParkrunData>();


        public double FontSize { get; set; } = 16;
        public DateTime SelectedDate { get; set; } = DateTime.Now.Date;

        public List<int> Hours { get; } = Enumerable.Range(0, 24).ToList();
        public List<int> Minutes { get; } = Enumerable.Range(0, 60).ToList();
        public List<int> Seconds { get; } = Enumerable.Range(0, 60).ToList();

        public int SelectedHour { get; set; }
        public int SelectedMinute { get; set; }
        public int SelectedSecond { get; set; }


        public int Delay { get; set; } = 5;     // Zeitverzögerung in ms für das Laden der Daten. Wird in der xaml-Datei verwendet, um die Zeitverzögerung für das Asynchrone Laden der Daten zu setzen, damit die UI sie in der Collection View Schrittweise aktualisieren kann. Standardmäßig auf 5 Sekunden gesetzt.

        string parkrunnerName = string.Empty;

        public string ParkrunnerName
        {
            get => parkrunnerName;
            set
            {
                if (parkrunnerName != value)
                {
                    parkrunnerName = value;
                }
            }
        }
        bool isSelectedEntryTriggeredByConstructor = false;           // Das erste mal wird das Entry durch den Konstruktor getriggert. Da soll aber die Methode LoadDataAsync nicht hier ausgeführt werden, da es noch in der OnAppearing von der XAML ebenfalls ausgeführt wird. Das führt dazu, dass diese Methode 2 mal ausgeführt wird => Die Daten werden doppelt in der Data Liste hinzugefügt. Deswegen soll nach dem Starten des Programms diese Methode anfangs nicht ausgeführt werden.
        private string selectedEntry;
        public string SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (selectedEntry != value)
                {
                    selectedEntry = value;

                    if (!isSelectedEntryTriggeredByConstructor)     // Soll nur ausgeführt werden, wenn es durch die Auswahl des Namens getriggert wurde, nicht durch den Konstuktor. Da der Konstuktor nur einmal pro Programmstart ausgeführt wird, kann diese Methode beim zweiten mal ausführen immer ausgeführt werden.
                    {
                        _ = LoadDataAsync();
                    }
                    isSelectedEntryTriggeredByConstructor = false;

                    // Automatisches Speichern in Preferences
                    Preferences.Set("ParkrunnerName", parkrunnerName.ToLower().Trim());
                }
            }
        }


        public string UpdateDatabaseButtonText { get; set; }

        string updateDatabaseText = "Aktualisiere Datenbank"; // Text für den Button, der die Datenbank aktualisiert. Wird beim Toggeln "UpdateDatabaseText" zugewiesen.
        string cancelUpdateText = "Abbrechen"; // Text für den Button, der das Aktualisieren abbricht. Wird beim Toggeln "UpdateDatabaseText" zugewiesen.

        public string ParkrunInfo { get; set; } = string.Empty; // Info für den User, von welcher Seite von der Webseite geladen werden.


        // Wenn keine Daten vorhanden sind, dann sollen die Flags dementsprechend zugewiesen werden und der passende Text dazu in der dazugehörigen xaml ausgegeben werden.
        public bool isDataEmpty { get; set; }
        public bool isDataAvailable { get; set; }

        public bool IsScrapping { get; set; } = false;          // Wenn true, dann wird von der Webseite Daten extrahiert.
        public bool isFetchDataFromDatabase { get; set; } = false;            // Wenn true, dann wird der Lade-Spinner angezeigt, der signalisiert, dass die Daten von der Datenbank geladen werden. 
        public bool isUpdateDataFromWebsite { get; set; } = false;            // Wenn true, dann wird der Lade-Spinner angezeigt, der signalisiert, dass die Daten von der Datenbank geladen werden. 

        public bool IsLoading { get; set; } // Wenn true, dann wird der Ladespinner angezeigt, wenn die Seite verlassen wird



        bool toogleUpdateButton = true; // Wenn true, dann wird beim Button der Text "Aktualisiere Datenbank" angezeigt. Anderfalls "Abbrechen".

        public double Progress { get; set; }


        public TimeSpan SelectedTime => new TimeSpan(SelectedHour, SelectedMinute, SelectedSecond);

        public ICommand AddDataCommand { get; }
        public ICommand RemoveDataCommand { get; }
        public ICommand ToogleUpdateButtonCommand { get; }
        public ICommand LoadDataCommand { get; }

        public ICommand DeleteTableCommand { get; }
        public ICommand CancelUpdateDataCommand { get; }


        public Command OpenChartPageCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("///ChartPage");
        });

        public ICommand GoToSettingsCommand { get; } = NavigationHelper.GoToSettingsCommand;

        public ICommand SuggestionUpdateCommand { get; }



        //private bool isDataLoaded = false;
        #endregion

        public async Task LoadDataAsync()
        {
            //Data = NavigationHelper.Data; // Verweis auf die Daten, die von der Datenbank geladen wurden. Wird in der NavigationHelper-Klasse gespeichert, um von anderen ViewModels darauf zuzugreifen.

            // aktiviert den Lade-Spinner, der signalisiert, dass die Daten von der Datenbank geladen werden.
            isDataAvailable = false;  // aktiviert den Lade-Spinner, der signalisiert, dass die Daten von der Datenbank geladen werden.
            isFetchDataFromDatabase = true;

            //isDataLoaded = false;
            Data.Clear();


            // Hol die gespeicherten Track-Namen aus den Einstellungen
            var selectedTracks = Preferences.Get("SelectedTracks", string.Empty)
                                            .Split(',')
                                            .Select(t => t.Trim())
                                            .ToList();


            allData = (await DatabaseService.GetDataAsync())
                .Where(x => selectedTracks.Contains(x.TrackName))   // filtere die Daten nach den ausgewählten Tracks
                .OrderBy(x => x.Date)                               // und sortiere die Daten nach Datum
                .ToList();

            foreach (var d in allData)
            {
                if (ParkrunnerName.ToLower().Trim() == d.Name.ToLower())
                {
                    Data.Add(d);
                    await Task.Delay(Delay); //kleine Pause gibt der UI Zeit zum Rendern
                }
            }

            //isDataLoaded = true;

            SetContentVisibility();

            // Wenn die Daten geladen wurden, dann wird der Lade-Spinner deaktiviert
            isDataAvailable = true;
            isFetchDataFromDatabase = false;

            // Speichern in Preferences
            Preferences.Set("ParkrunnerName", parkrunnerName.ToLower().Trim());
        }

        /// <summary>
        /// Zeigt das Label "Keine Daten vorhanden" an, indem die IsVisible-Eigenschaft auf true gesetzt wird, falls keine Daten vorliegen.
        /// </summary>
        public void SetContentVisibility()
        {
            if (Data != null && Data.Count() > 0)
            {
                isDataAvailable = true;
                isDataEmpty = false;
            }
            else
            {
                isDataAvailable = false;
                isDataEmpty = true;
            }
        }

        public ParkrunViewModel()
        {
            //NavigationHelper.LoadFilteredParkrunDataSync(); // Lädt die gefilterten Daten von der Datenbank, die von anderen ViewModels verwendet werden können.
            //Data = NavigationHelper.Data; // Verweis auf die Daten, die von der Datenbank geladen wurden. Wird in der NavigationHelper-Klasse gespeichert, um von anderen ViewModels darauf zuzugreifen. 

            // Initialisere den Namen, wenn schon einer gespeichert wurde.
            if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
            {
                ParkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
            }
            else
            {
                Preferences.Set("ParkrunnerName", "");
                ParkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
            }

            isSelectedEntryTriggeredByConstructor = true;       // Setzt den Status, dass das Entry durch den Konstruktor getriggert wurde. Dadurch wird verhindert, dass die Methode LoadDataAsync() beim Setzen des Namens im Konstruktor ausgeführt wird. Diese Methode wird dann in der OnAppearing-Methode der XAML-Datei ausgeführt, um die Daten zu laden, welche auch immer beim Seitenwechsel ausgeführt wird.
            SelectedEntry = ParkrunnerName;

            ToogleUpdateButtonCommand = new Command(async () =>
            {
                if (toogleUpdateButton)
                {
                    toogleUpdateButton = false;
                    UpdateDatabaseButtonText = cancelUpdateText;  // Wenn der Button gedrückt wird, dann wird der Text auf "Abbrechen" geändert.

                    bool isNewDataAvailable = await ScrapeParkrunDataAsync(ParkrunnerName);
                    if (isNewDataAvailable) 
                    {
                        await LoadDataAsync();
                    }
                }
                else
                {
                    toogleUpdateButton = true; // Setze den Status des Buttons auf "Aktualisieren" zurück
                    UpdateDatabaseButtonText = updateDatabaseText;  // Wenn der Button gedrückt wird, dann wird der Text auf "Aktualisiere Datenbank" geändert.

                    // Abbrechen des Scrappens, falls der User es wünscht.
                    if (cts != null && !cts.IsCancellationRequested)
                    {
                        isUpdateDataFromWebsite = false; // Deaktiviere die Anzeige für den Status der Fortschrittsanzeige 
                        cts.Cancel();
                    }
                }
            });


            LoadDataCommand = new Command(async () =>
            {
                await LoadDataAsync();
            });

            DeleteTableCommand = new Command(() =>
            {
                DeleteTable();
            });

            CancelUpdateDataCommand = new Command(() =>
            {
                if (cts != null && !cts.IsCancellationRequested)
                {
                    isUpdateDataFromWebsite = false; // Deaktiviere die Anzeige für den Status der Fortschrittsanzeige 
                    cts.Cancel();
                }
            });

            SuggestionUpdateCommand = new Command(() =>
            {
                SuggestionUpdate();
            });

            void SuggestionUpdate()
            {
                if (String.IsNullOrEmpty(ParkrunnerName))
                {
                    SuggestionList = new ObservableCollection<string>();
                    return;
                }

                SuggestionList = new ObservableCollection<string>(allData
               .Where(d => d.Name.StartsWith(ParkrunnerName, StringComparison.OrdinalIgnoreCase))
               .Select(d => d.Name)
               .OrderBy(name => name)
               .Distinct()
               .Take(2));
            }

            if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
            {
                ParkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
            }

            if (toogleUpdateButton)
                UpdateDatabaseButtonText = updateDatabaseText; // Wenn der Button gedrückt wird, dann wird der Text auf "Aktualisiere Datenbank" geändert.
            else
                UpdateDatabaseButtonText = cancelUpdateText; // Wenn der Button gedrückt wird, dann wird der Text auf "Aktualisiere Datenbank" geändert.

            SuggestionList = new ObservableCollection<string>();
        }

        int datacount = 0; // Variable, um die Anzahl der Datensätze zu zählen, die von der Webseite extrahiert wurden. Wird für den Fortschritt benötigt.
        bool isURLValid = true; // Variable, um zu überprüfen, ob die URL gültig ist. Wird für den Fortschritt benötigt.
        private CancellationTokenSource cts; // Variable, um den Task des Scrappens abzubrechen, falls der User es wünscht.

        public async Task<bool> ScrapeParkrunDataAsync(string searchName)
        {
            cts = new CancellationTokenSource();        // Wird verwendet, um den Task des Scrappens abzubrechen, falls der User es wünscht.

            //suche aus der Datenbank den höchsten Parkrun Nr. und setze den Startwert für die Schleife
            var data = await DatabaseService.GetDataAsync();

            IsScrapping = true; // Setze den Status auf "Daten werden von der Webseite extrahiert"
            bool isScrappingSuccess = false; // Variable, um den Erfolg des Scrappens zu verfolgen
            isUpdateDataFromWebsite = true;

            List<string> parkrunLocations = GetParkrunLocations();

            // Wandelt den String, wo alle Namen von den Parkrun-Standorten gespeichert sind, in eine Liste um.
            List<string> GetParkrunLocations()
            {
                // Gespeicherte Namen aus Preferences laden
                var savedTracks = Preferences.Get("SelectedTracks", "").Split(',');

                // Neue Liste mit aktualisierten Namen
                List<string> parkrunLocations = new List<string>();

                if (savedTracks[0] == string.Empty)
                    return parkrunLocations;

                foreach (var savedTrack in savedTracks)
                {
                    parkrunLocations.Add(savedTrack);
                }

                return parkrunLocations;
            }

            if (parkrunLocations.Count > 0)
                ParkrunInfo = "Starte mit dem Extrahieren der Daten von der Webseite...";
            else
                ParkrunInfo = "Keine Parkrun-Standorte ausgewählt. Bitte gehe zu den Einstellungen und wähle mindestens einen Parkrun-Standort aus.";

            foreach (var location in parkrunLocations)
            {
                int nextParkrunNr = data.Any(x => x.TrackName == location) ? data
                    .Where(x => x.TrackName == location)
                    .Max(x => x.ParkrunNr + 1) : 1; //Holt sich die Nr. vom letzten Run von der Datenbank und addiere 1 dazu, so dass man mit der nächsten Seite, welche man scrappen will, fortsetzen kann. Falls die Datenbank keine Einträge hat, setze 1

                int currentParkrunNr = nextParkrunNr - 1;  //Die aktuell höchste Parkrun Nr in der Datenbank von einem bestimmten Ort

                int totalRuns = CalculateTotalRuns(location);
                //totalRuns = 11; //test
                for (int run = nextParkrunNr; run <= totalRuns; run++)
                {
                    // Prüfe, ob der Task abgebrochen wurde
                    if (cts != null && cts.IsCancellationRequested)
                    {
                        return true; // Beende die Schleife, wenn der Task abgebrochen wurde
                    }

                    string url = $"https://www.parkrun.com.de/{location}/results/{run}/";
                    var htmlContent = await ScrapeSingleParkrunSiteAsync(url);

                    htmlContent = HttpUtility.HtmlDecode(htmlContent); // Wandelt HTML-Entity in lesbaren Text um
                    if (!string.IsNullOrEmpty(htmlContent))
                    {
                        ParkrunInfo = $"Extrahiere Seite {run.ToString()} vom parkrun {location.ToUpper()}";  //Dient als Info für den User, von welcher Seite von der Webseite geladen werden.

                        // Parse den HTML-Inhalt mit HtmlAgilityPack
                        var doc = new HtmlDocument();
                        doc.LoadHtml(htmlContent);

                        var DateString = doc.DocumentNode.SelectSingleNode("//span[@class='format-date']")?.InnerHtml;
                        if (!DateTime.TryParse(DateString ?? "", out DateTime date)) { date = new DateTime(); }

                        var resultNode = doc.DocumentNode.SelectSingleNode("//tbody[@class='js-ResultsTbody']");

                        if (resultNode != null)
                        {
                            var dataFromWebsite = resultNode.SelectNodes(".//tr");
                            if (dataFromWebsite == null) { return false; }

                            foreach (var trNode in dataFromWebsite) // Punkt vor `//tr` bedeutet "relativ zum aktuellen Node"
                            {
                                var parkrunNr = run;
                                var name = trNode.GetAttributeValue("data-name", "Kein Name vorhanden"); // Wandelt HTML-Entity um
                                                                                                         //if (name.ToLower() != searchName.ToLower()) { continue; } // Hier wird der Name des Läufers gefiltert
                                var ageGroup = trNode.GetAttributeValue("data-agegroup", "Keine Altersgruppe vorhanden");

                                string genderText = trNode.InnerText.Trim();

                                var gender = trNode.GetAttributeValue("data-gender", "kein Geschlecht angegeben.");

                                if (!int.TryParse(trNode.GetAttributeValue("data-runs", "0"), out int runs)) { runs = 0; }

                                string ageGradeString = trNode.GetAttributeValue("data-agegrade", "0");
                                if (!float.TryParse(ageGradeString, CultureInfo.InvariantCulture, out float ageGrade)) { ageGrade = 0f; }

                                var timeNode = trNode.SelectSingleNode(".//*[contains(@class, 'time')]");
                                TimeSpan currentTime = FetchCurrentTime(timeNode);

                                TimeSpan bestTime = FetchBestTime(timeNode);

                                var parkrunData = new ParkrunData
                                {
                                    TrackName = location, // Name der Laufstrecke
                                    ParkrunNr = parkrunNr,
                                    Date = date,
                                    Name = name,
                                    AgeGroup = ageGroup,
                                    Gender = gender,
                                    Runs = runs,
                                    AgeGrade = ageGrade,
                                    Time = currentTime,
                                    PersonalBest = bestTime,
                                    DistanceKm = 5
                                };

                                //if (name.ToLower() != searchName.ToLower())
                                //    Data.Add(parkrunData);

                                isScrappingSuccess = true;

                                await DatabaseService.SaveDataAsync(parkrunData);
                            }
                        }

                        // Aktualisiere die Fortschrittsanzeige
                        var bar = (double)(run - nextParkrunNr + 1) / (totalRuns - nextParkrunNr + 1); // Berechnet den Fortschritt in Prozent
                        Progress = bar;

                        int waitTime = new Random().Next(14, 20); // Zufällige Wartezeit 

                        if (run < totalRuns)
                            await WaitBeforeNextScrape(waitTime, ParkrunInfo + ". Die Daten von der nächste Seite werden extrahiert in ", "Aktualisierung abgebrochen.");
                    }
                    else
                    {
                        break;
                    }
                }

                datacount += (totalRuns - currentParkrunNr);
            } // Ende der foreach-Schleife für die Parkrun-Standorte
            isUpdateDataFromWebsite = false; // Deaktiviere die Anzeige für den Status der Fortschrittsanzeige 

            bool isNewDataAvailable = true;
            if (!isURLValid) // Wenn das Scrappen nicht erfolgreich war
            {
                //ParkrunInfo = "Es konnte keine Verbindung zur Webseite aufgebaut werden.";
                if (Application.Current?.Windows[0].Page != null)
                { 
                    await Application.Current?.Windows[0].Page.DisplayAlert("Fehler", "Es konnte keine Verbindung zur Webseite aufgebaut werden.", "OK");
                    isNewDataAvailable = false; // Setze den Status auf "Keine neuen Daten verfügbar"
                }

            }
            else if (!isScrappingSuccess)
            {
                //ParkrunInfo = "Es sind keine neuen Daten vorhanden.";
                if (Application.Current?.Windows[0].Page != null)
                {
                    await Application.Current?.Windows[0].Page.DisplayAlert("Hinweis", "Es sind keine neuen Daten vorhanden.", "OK");
                    isNewDataAvailable = false; // Setze den Status auf "Keine neuen Daten verfügbar"
                }

            }
            else if (isScrappingSuccess) // Wenn das Scrappen erfolgreich war
            {
                //ParkrunInfo = "Die Datenbank wurde erfolgreich aktualisiert. Es sind " + (totalRuns - currentParkrunNr) + " neue Daten vorhanden.";
                if (Application.Current?.Windows[0].Page != null)
                {
                    string message = datacount == 1
                        ? $"Die Datenbank wurde erfolgreich aktualisiert. Es ist {datacount} neuer Datensatz vorhanden."
                        : $"Die Datenbank wurde erfolgreich aktualisiert. Es sind {datacount} neue Datensätze vorhanden.";

                    await Application.Current?.Windows[0].Page.DisplayAlert("Hinweis", message, "OK");
                }
            }

            IsScrapping = false; // Setze den Status zurück, wenn die Datenextraktion abgeschlossen ist

            toogleUpdateButton = true; // Setze den Status des Buttons auf "Aktualisieren" zurück
            UpdateDatabaseButtonText = updateDatabaseText;  // Wenn der Button gedrückt wird, dann wird der Text auf "Aktualisiere Datenbank" geändert.

            // Berechnet die Gesamtanzahl der Parkruns basierend auf dem Datum des ersten Parkruns in Deutschland
            int CalculateTotalRuns(string location)
            {
                int extraRuns = 1; // Falls extra Läufe stattfanden. Normalerweise finden alle 7 Tage ein Parkrun statt, aber ich vermute, dass min. 1 extra Lauf stattfand. 

                Dictionary<string, DateTime> firstParkrunDates = ParkrunTracks.AvailableTracks
                .ToDictionary(track => string.IsNullOrEmpty(track.TrackNameURL) ? track.TrackName.ToLower() : track.TrackNameURL,
                              track => track.FirstParkrunDate);

                //var firstParkrunDate = new DateTime(2022, 7, 9); // Das Datum des ersten Parkruns in Deutschland beim Prießnitzgrund in der Heide in Dresden
                DateTime firstParkrunDate = DateTime.MinValue;
                if (!firstParkrunDates.ContainsKey(location))
                {
                    Console.WriteLine("Der Standort '" + location + "' ist nicht in der Liste der Parkrun-Standorte enthalten.");
                    return 0; // Wenn der Standort nicht gefunden wird, gebe 0 zurück
                }
                else
                {
                    firstParkrunDate = firstParkrunDates[location]; // Hole das Datum des ersten Parkruns für den angegebenen Standort
                }
                    


                var currentDate = DateTime.Now.Date;
                var daysSinceFirstParkrun = (currentDate - firstParkrunDate).TotalDays;
                var totalRuns = (int)(daysSinceFirstParkrun / 7) + 1 + extraRuns; // Anzahl der Parkruns seit dem ersten Parkrun in Deutschland

                return totalRuns;
            }

            TimeSpan FetchCurrentTime(HtmlNode? timeNode)
            {
                var timeOnly = timeNode?.SelectSingleNode(".//div[@class='compact']")?.InnerHtml ?? TimeSpan.Zero.ToString();
                var match = Regex.Match(timeOnly, @"\d{1,2}:\d{2}");
                if (match.Success)
                {
                    timeOnly = "00:" + timeOnly; // fügt "00:" am Anfang hinzu, um es in "hh:mm:ss" zu konvertieren
                }

                if (!TimeSpan.TryParse(timeOnly ?? TimeSpan.Zero.ToString(), out TimeSpan currentTime)) { currentTime = TimeSpan.Zero; }

                return currentTime;
            }
            TimeSpan FetchBestTime(HtmlNode? timeNode)
            {
                var rawText = timeNode?.SelectSingleNode(".//div[@class='detailed']")?.InnerText.Trim() ?? TimeSpan.Zero.ToString();
                var match = Regex.Match(rawText, @"\d{1,2}:\d{2}(:\d{2})?"); // Es wird sowohl das Format "mm:ss" als auch "hh:mm:ss" extrahiert
                var timeOnly = match.Success ? match.Value : TimeSpan.Zero.ToString();

                // hier wird kontrolliert, ob das Format "mm:ss" entspricht. Wenn das der Fall ist, soll es in "hh:mm:ss" umgewandelt werden.
                var match2 = Regex.Match(timeOnly, @"\d{1,2}:\d{2}");
                if (match2.Success)
                {
                    timeOnly = "00:" + timeOnly; // fügt "00:" am Anfang hinzu, um es in "hh:mm:ss" zu konvertieren
                }

                if (!TimeSpan.TryParse(timeOnly ?? TimeSpan.Zero.ToString(), out TimeSpan bestTime)) { bestTime = TimeSpan.Zero; }

                return bestTime;
            }

            return isNewDataAvailable; // Gibt true zurück, wenn neue Daten verfügbar sind, andernfalls false
        }

        private async Task WaitBeforeNextScrape(int waitSeconds, string infoText, string taskCanceledText = "Vorgang abgebrochen.")
        {
            try
            {
                for (int i = waitSeconds; i >= 0; i--)
                {
                    ParkrunInfo = infoText + $"{i} Sekunden.";
                    await Task.Delay(1000, cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                if (Application.Current?.Windows[0].Page != null)
                    await Application.Current.Windows[0].Page.DisplayAlert("Hinweis", taskCanceledText, "OK");
            }
        }
        

        public async void DeleteTable()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                    "Bestätigung",
                    "Möchtest du wirklich alle Daten löschen?",
                    "Ja",
                    "Nein"
                );

            if (confirm)
            {
                await DatabaseService.DeleteAllDataAsync();
                Data = new ObservableCollection<ParkrunData>();
                //pendingEntries.Clear(); // Leere die Warteschlange
            }
        }

        public async Task<string> ScrapeSingleParkrunSiteAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var userAgents = new List<string>
                {
                     // 🖥️ Windows-Browser
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:110.0) Gecko/20100101 Firefox/110.0",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Edge/120.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Version/15.0 Safari/537.36",

                    // 📱 Mobile Browser
                    "Mozilla/5.0 (Linux; Android 12; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Mobile Safari/537.36",
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/605.1.15",

                    // 🖥️ MacOS Browser
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:110.0) Gecko/20100101 Firefox/110.0",

                    // 🔍 Googlebot (Falls die Seite Bots erlaubt)
                    "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)"
                };

                int tryCount = 0;

                while (tryCount < 5)
                {
                    try
                    {
                        // Wähle zufälligen User-Agent bei jedem Versuch
                        string randomUserAgent = userAgents[new Random().Next(userAgents.Count)];
                        client.DefaultRequestHeaders.Remove("User-Agent"); // Entferne alten User-Agent
                        client.DefaultRequestHeaders.Add("User-Agent", randomUserAgent);

                        HttpResponseMessage response = await client.GetAsync(url);

                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                        {
                            int waitTime = 0;
                            if (tryCount <= 3)
                            {
                                waitTime = new Random().Next(15, 30); // Zufällige Wartezeit
                            }
                            else
                            {
                                waitTime = new Random().Next(60, 80); // Zufällige Wartezeit 
                            }

                            await WaitBeforeNextScrape(waitTime, "Die Verbindung konnte nicht hergestellt werden. Es wird ein neuer Versuch gestartet in ");

                            tryCount++;
                            continue;
                        }

                        response.EnsureSuccessStatusCode(); // Falls kein 403-Fehler (Forbidden), prüfe auf andere Fehler
                        return await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fehler beim Abrufen der Webseite: {ex.Message}");
                        isURLValid = false; // Variable, um den Erfolg des Scrappens zu verfolgen. Wird für den Fortschritt benötigt.
                        break;
                    }
                }
            }

            return string.Empty;
        }
    }
}