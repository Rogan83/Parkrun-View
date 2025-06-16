using HtmlAgilityPack;
using Parkrun_View.MVVM.Helpers;
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
    internal class ParkrunViewModel
    {
        #region Properties and Fields
        public ObservableCollection<ParkrunData> Data { get; set; } = new();
        private List<ParkrunData> pendingEntries = new();

        public double FontSize { get; set; } = 16;
        public DateTime SelectedDate { get; set; } = DateTime.Now.Date;

        public List<int> Hours { get; } = Enumerable.Range(0, 24).ToList();
        public List<int> Minutes { get; } = Enumerable.Range(0, 60).ToList();
        public List<int> Seconds { get; } = Enumerable.Range(0, 60).ToList();

        public int SelectedHour { get; set; }
        public int SelectedMinute { get; set; }
        public int SelectedSecond { get; set; }

        string parkrunnerName = string.Empty;

        public string ParkrunnerName
        {
            get => parkrunnerName;
            set
            {
                parkrunnerName = value;

                // Automatisches Speichern in Preferences
                Preferences.Set("ParkrunnerName", parkrunnerName.ToLower().Trim());
            }
        }

        public string ParkrunInfo { get; set; } = string.Empty; // Info für den User, von welcher Seite von der Webseite geladen werden.
        public bool IsScrapping { get; set; } = false;          // Wenn true, dann wird von der Webseite Daten extrahiert.
        public bool IsScrappingProgressEnabled { get; set; } = false;

        public double Progress { get; set; }


        public TimeSpan SelectedTime => new TimeSpan(SelectedHour, SelectedMinute, SelectedSecond);

        public ICommand AddDataCommand { get; }
        public ICommand RemoveDataCommand { get; }
        public ICommand FetchDataFromWebsite { get; }
        public ICommand LoadDataCommand { get; }

        public ICommand DeleteTableCommand { get; }


        public Command OpenChartPageCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("///ChartPage");
        });

        public ICommand GoToSettingsCommand { get; } = NavigationHelper.GoToSettingsCommand;



        private bool isDataLoaded = false;
        #endregion


        public async Task LoadDataAsync()
        {
            if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
            {
                ParkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
            }
            else
            {
                Preferences.Set("ParkrunnerName", "");
                ParkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
            }

            isDataLoaded = false;
            Data.Clear();

            var data = await DatabaseService.GetDataAsync();
            foreach (var d in data)
            {
                if (ParkrunnerName.ToLower() == d.Name.ToLower())
                    Data.Add(d);
            }

            isDataLoaded = true;

            // Jetzt füge die zwischengespeicherten neuen Einträge hinzu
            foreach (var pending in pendingEntries)
            {
                if (ParkrunnerName.ToLower() != pending.Name.ToLower())
                    Data.Add(pending);
            }
            pendingEntries.Clear(); // Warteschlange leeren
        }

        public ParkrunViewModel()
        {
            AddDataCommand = new Command(async () =>
            {
                var parkrunData = new ParkrunData { Date = SelectedDate, Time = SelectedTime, DistanceKm = 5 };

                await DatabaseService.SaveDataAsync(parkrunData);

                if (isDataLoaded)
                {
                    Data.Add(parkrunData);
                }
                else
                {
                    pendingEntries.Add(parkrunData);        // Speichere das neue Element temporär, so dass es nach dem Laden der Daten von der Datenbank in der "LoadDataAsync" hinzugefügt wird.
                }

                Data = new ObservableCollection<ParkrunData>(Data.OrderBy(d => d.Date));
            });

            RemoveDataCommand = new Command<ParkrunData>(async (parkrunData) =>
            {
                if (parkrunData != null)
                {
                    await DatabaseService.DeleteDataAsync(parkrunData);
                    Data.Remove(parkrunData);
                }
            });

            FetchDataFromWebsite = new Command(async () =>
            {
                await ScrapeParkrunDataAsync(ParkrunnerName);
                await LoadDataAsync();
            });

            LoadDataCommand = new Command(async () =>
            {
                await LoadDataAsync();
            });

            DeleteTableCommand = new Command(() =>
            {
                DeleteTable();
            });


            if (Preferences.Get("ParkrunnerName", string.Empty) != string.Empty)
            {
                ParkrunnerName = Preferences.Get("ParkrunnerName", string.Empty);
            }

        }

        int datacount = 0; // Variable, um die Anzahl der Datensätze zu zählen, die von der Webseite extrahiert wurden. Wird für den Fortschritt benötigt.
        bool isURLValid = true; // Variable, um zu überprüfen, ob die URL gültig ist. Wird für den Fortschritt benötigt.
        public async Task ScrapeParkrunDataAsync(string searchName)
        {
            //suche aus der Datenbank den höchsten Parkrun Nr. und setze den Startwert für die Schleife
            var data = await DatabaseService.GetDataAsync();

            IsScrapping = true; // Setze den Status auf "Daten werden von der Webseite extrahiert"
            bool isScrapSuccess = false; // Variable, um den Erfolg des Scrappens zu verfolgen
            IsScrappingProgressEnabled = true;

            List<string> parkrunLocations = GetParkrunLocations();

            // Wandelt den String, wo alle Namen von den Parkrun-Standorten gespeichert sind, in eine Liste um.
            List<string> GetParkrunLocations()
            {
                // Mapping zwischen gespeicherter Schreibweise und gewünschtem Format. 
                // Dient dazu, falls die Schreibweise der gespeicherten Parkrun-Standorte in den Preferences nicht mit der Schreibweise der URL übereinstimmt.
                //var nameMapping = new Dictionary<string, string>
                //{
                //    { "Prießnitzgrund", "priessnitzgrund" },
                //    // Weitere Namen können hier hinzugefügt werden
                //};

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
                ParkrunInfo = "Starte mit dem Exrahieren der Daten von der Webseite...";
            else
                ParkrunInfo = "Keine Parkrun-Standorte ausgewählt. Bitte gehe zu den Einstellungen und wähle mindestens einen Parkrun-Standort aus.";


            foreach (var location in parkrunLocations)
            {
                int nextParkrunNr = data.Any(x => x.TrackName == location) ? data
                    .Where(x => x.TrackName == location)
                    .Max(x => x.ParkrunNr + 1) : 1; //Holt sich die Nr. vom letzten Run von der Datenbank und addiere 1 dazu, so dass man mit der nächsten Seite, welche man scrappen will, fortsetzen kann. Falls die Datenbank keine Einträge hat, setze 1

                int totalRuns = CalculateTotalRuns(location);
                //totalRuns = 20; //test
                for (int run = nextParkrunNr; run <= totalRuns; run++)
                {
                    string url = $"https://www.parkrun.com.de/{location}/results/{run}/";
                    var htmlContent = await ScrapeSingleParkrunSiteAsync(url);

                    //todo: Hier sollte eine Überprüfung erfolgen, ob die URL gültig ist. Falls nicht, dann soll der Scrapper abbrechen und die Schleife verlassen


                    /////

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
                            if (dataFromWebsite == null) { return; }

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

                                isScrapSuccess = true;
                                

                                await DatabaseService.SaveDataAsync(parkrunData);
                            }
                        }

                        // Aktualisiere die Fortschrittsanzeige
                        var bar = (double)(run - nextParkrunNr + 1) / (totalRuns - nextParkrunNr + 1); // Berechnet den Fortschritt in Prozent
                        Progress = bar;

                        int waitTime = new Random().Next(14, 20); // Zufällige Wartezeit 

                        if (run < totalRuns)
                            await Task.Delay(TimeSpan.FromSeconds(waitTime));
                    }
                    else
                    {
                        break;
                    }
                }
                datacount += (totalRuns - nextParkrunNr - 1);


            } // Ende der foreach-Schleife für die Parkrun-Standorte
            IsScrappingProgressEnabled = false; // Deaktiviere die Anzeige für den Status der Fortschrittsanzeige 

            if (!isURLValid) // Wenn das Scrappen nicht erfolgreich war
            {
                //ParkrunInfo = "Es konnte keine Verbindung zur Webseite aufgebaut werden.";
                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Fehler", "Es konnte keine Verbindung zur Webseite aufgebaut werden.", "OK");
            }
            else if (!isScrapSuccess)
            {
                //ParkrunInfo = "Es sind keine neuen Daten vorhanden.";
                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Hinweis", "Es sind keine neuen Daten vorhanden.", "OK");

            }
            else if (isScrapSuccess) // Wenn das Scrappen erfolgreich war
            {
                //ParkrunInfo = "Die Datenbank wurde erfolgreich aktualisiert. Es sind " + (totalRuns - currentParkrunNr) + " neue Daten vorhanden.";
                if (Application.Current?.MainPage != null)
                {
                    string message = datacount == 1
                        ? $"Die Datenbank wurde erfolgreich aktualisiert. Es ist {datacount} neuer Datensatz vorhanden."
                        : $"Die Datenbank wurde erfolgreich aktualisiert. Es sind {datacount} neue Datensätze vorhanden.";

                    await Application.Current.MainPage.DisplayAlert("Hinweis", message, "OK");
                }
            }



            IsScrapping = false; // Setze den Status zurück, wenn die Datenextraktion abgeschlossen ist


            // Berechnet die Gesamtanzahl der Parkruns basierend auf dem Datum des ersten Parkruns in Deutschland
            int CalculateTotalRuns(string location)
            {
                int extraRuns = 1; // Falls extra Läufe stattfanden. Normalerweise finden alle 7 Tage ein Parkrun statt, aber ich vermute, dass min. 1 extra Lauf stattfand. 

                //Dictionary<string, DateTime> firstParkrunDates = new Dictionary<string, DateTime>
                //{
                //    { "priessnitzgrund", new DateTime(2022, 7, 9) },    // Das Datum des ersten Parkruns im Prießnitzgrund in Dresden
                //    { "oberwald", new DateTime(2020, 10, 25) }          // Das Datum des ersten Parkruns im Oberwald in Deutschland
                //    // Weitere Standorte und deren erste Parkrun-Daten können hier hinzugefügt werden
                //};
                //Dictionary<string, DateTime> firstParkrunDates = ParkrunTracks.AvailableTracks.ToDictionary(track => track.TrackNameURL, track => track.FirstParkrunDate);
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
                pendingEntries.Clear(); // Leere die Warteschlange
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
                            //Console.WriteLine($"403 Forbidden – Warte {waitTime} Sekunden und versuche mit neuem User-Agent erneut...");
                            //ParkrunInfo = "Statuscode: " + response.StatusCode + " – Warten Sie bitte " + waitTime + " Sekunden. Es wird danach erneut versucht.";
                            ParkrunInfo = $"Die Verbindung konnte nicht hergestellt werden. In {waitTime} Sekunden wird ein neuer Versuch gestartet.";
                            await Task.Delay(TimeSpan.FromSeconds(waitTime));
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
                        //tryCount++;
                    }
                }
            }

            return string.Empty;
        }
    }
}
