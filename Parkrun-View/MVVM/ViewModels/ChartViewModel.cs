using Parkrun_View.MVVM.Models;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microcharts;
using SkiaSharp;
using Parkrun_View.MVVM.Helpers;
using Parkrun_View.Services;
using Parkrun_View.MVVM.Interfaces;        // Wird für die Grafiken benötigt

namespace Parkrun_View.MVVM.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class ChartViewModel : ILoadableViewModel
    {
        public List<ParkrunData> Data { get; set; } = new List<ParkrunData>();
        public List<ParkrunData> DataPeriod { get; set; } = new List<ParkrunData>();        // Daten, die für die aktuelle Periode ausgewählt wurden
        public Chart LineChart { get; private set; }

        public double FontSize { get; set; } = 16;

        private DateTime dateStart;

        public DateTime DateStart 
        { 
            get => dateStart; 
            set 
            {
                dateStart = value;
                FilterDataByPeriod();
            } 
        }

        private DateTime dateEnd;

        public DateTime DateEnd
        {
            get => dateEnd;
            set
            {
                dateEnd = value;
                FilterDataByPeriod();
            }
        }

        public int ChartWidth { get; set; }
        int maxChartWidth = 0;               // Maximale Breite, die der Linechart haben darf, damit die Infos zu jeden Datenpunkt vollständig zu sehen sind.
        public int ChartHeight { get; set; }

        public bool IsToManyData { get; set; }  // Wenn zu viele Daten vorhanden sind, dann gibt es die Möglichkeit zwischen einer kompakten und einer detaillierten Ansicht zu wechseln.
        bool isCompactView = false;
        public string IsCompleteLabelName { get; set; } = "Detailansicht"; // Label für die Ansicht

        // Wenn keine Daten vorhanden sind, dann sollen die Flags dementsprechend zugewiesen werden und der passende Text dazu in der dazugehörigen xaml ausgegeben werden.
        public bool isDataEmpty { get; set; }
        public bool isDataAvailable { get; set; }

        public bool IsLoading { get; set; }


        public ICommand ToggleViewModus { get; set; }
      
        public ICommand GoToSettingsCommand { get; } = NavigationHelper.GoToSettingsCommand;

        public ChartViewModel(DateTime dateStart = default)
        {
            DataPeriod  = NavigationHelper.Data.ToList(); // Verweis auf die Daten, die von der Datenbank geladen wurden. Wird in der NavigationHelper-Klasse gespeichert, um von anderen ViewModels darauf zuzugreifen.

            LineChart = new LineChart();

            ToggleViewModus = new Command((parkrunData) =>
            {
                isCompactView = !isCompactView;
                IsCompleteLabelName = isCompactView ? "Kompaktansicht" : "Detailansicht";

                UpdateChart();
            });

            DeviceDisplay.Current.MainDisplayInfoChanged += OnDisplayChanged;
            DateStart = dateStart;

            SetContentVisibility();
        }
        //Wird ausgerufen, wenn das DisplayInfo sich ändert, z.B. bei der Drehung des Bildschirms
        void OnDisplayChanged(object? sender, DisplayInfoChangedEventArgs e)
        {
            UpdateChart();
        }

        int expandedChartWidth = 0; // Breite der Detailansicht



        /// <summary>
        /// Aktualisiert das Diagramm.
        /// </summary>
        public void UpdateChart()
        {
            UpdateChartDimensions(); // Breite aktualisieren

            List<ChartEntry> entries;
            if (DataPeriod.Count == 0)
            {
                entries = new List<ChartEntry>
                {
                    new ChartEntry(0) { Label = "", ValueLabel = "", Color = SKColor.Parse("#FF5733") },
                };
            }
            else
            {
                string label = string.Empty;
                string valueLabel = string.Empty;
                DataPeriod = DataPeriod.OrderBy(d => d.Date).ToList();

                var maxTime = DataPeriod.Max(d => d.Time.TotalSeconds); // Höchster Wert bestimmen
                var minTime = DataPeriod.Min(d => d.Time.TotalSeconds); // Niedrigsten Wert bestimmen
                entries = DataPeriod.Select((result, index) =>
                {
                    float calculatedValue = (float)(maxTime - result.Time.TotalSeconds);

                    // Falls die Zeit die Bestzeit erreicht hat, dann wird diese anders eingefärbt
                    SKColor color;
                    if (result.Time.TotalSeconds == minTime)
                    {
                        color = SKColor.Parse("#2ecc71"); // Grün für die Bestzeit
                    }
                    else if (result.Time.TotalSeconds == maxTime)
                    {
                        color = SKColor.Parse("#e74c3c"); // Rot für die langsamste Zeit
                    }
                    else
                    {
                        color = SKColor.Parse("#f1c40f"); // Gelb für die anderen Zeiten
                    }
                    // Wenn die Detailansicht aktiv ist oder die Breite von der kompletten Ansicht kleiner als die maximale Breite ist, dann wird das Datum und die Zeit angezeigt. Sonst nicht, da das Datum und die Zeit zu wenig Platz hätte
                    if (!isCompactView || expandedChartWidth <= maxChartWidth)
                    {
                        label = result.Date.ToShortDateString();
                        valueLabel = $"{result.Time}";
                    }

                    return new ChartEntry(calculatedValue)
                    {
                        Label = label,
                        ValueLabel = valueLabel,
                        Color = color // Dynamische Farbänderung basierend auf der Bedingung
                    };
                }).ToList();
            }

            #region TestData
            
            //var entries = new List<ChartEntry>
            //{

            //    new ChartEntry(100) { Label = "Mo", ValueLabel = "100", Color = SKColor.Parse("#FF5733") },
            //    new ChartEntry(500) { Label = "Do", ValueLabel = "500", Color = SKColor.Parse("#FFD700") },
            //    new ChartEntry(1000) { Label = "Fr", ValueLabel = "1000", Color = SKColor.Parse("#A020F0") },
            //    new ChartEntry(500) { Label = "Do", ValueLabel = "500", Color = SKColor.Parse("#FFD700") },
            //};
            #endregion

            LineChart = new LineChart
            {
                Entries = entries,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                LabelTextSize = (float)FontSize * 2,

                //MaxValue = 1000,  // Höchster Wert ein wenig über deinem höchsten Punkt setzen
                //MinValue = 0   // Niedrigster Wert nahe deinem kleinsten Punkt setzen
            };


            void UpdateChartDimensions()
            {
                int adjustedWidth = 0;
                int dataPointWidth = 100;

                int adjustedHeight = 0;

                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    CalculateAdjustedDimensionsForSmartphone();
                    ChartHeight = (int)(adjustedHeight * 0.5f);
                }
                else if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                {
                    CalculateAdjustedDimensionsForPC();
                    ChartHeight = (int)(adjustedHeight * 0.6f);

                    dataPointWidth = 150;
                }
                maxChartWidth = adjustedWidth;  // Maximale Breite, die der Linechart haben darf, damit alle Infos zu jeden Datenpunkt vollständig auf eine Seite zu sehen sind für die kompakte Ansicht.
                                                // Wird durch die Kalkulation in CalculateAdjustedDimensionsForSmartphone() bzw. CalculateAdjustedDimensionsForPC() ermittelt
                expandedChartWidth = DataPeriod.Count * dataPointWidth;   // Die Gesamtbreite des Diagramms, welches mit einem horizontalen Balken angeschaut werden kann, wird anhand der Anzahl der Datenpunkte und der Breite pro Datenpunkt berechnet.

                if (isCompactView)
                {
                    ChartWidth = adjustedWidth; // Maximale Breite für die vollständige Ansicht
                }
                else
                {
                    // Mindestbreite festlegen und je nach Datenzahl skalieren
                    ChartWidth = Math.Max(adjustedWidth, expandedChartWidth);  // Pro Datenpunkt 50 Pixel Breite
                }

                // Wenn die Anzahl der Datenpunkte und die daraus resultierende Gesamtbreite für das Liniendiagramm zu hoch ist,
                // dann wird der Button sichtbar, wo man in einer kompakten Ansicht wechseln kann,
                // wo aber kein Datenpunkt mehr beschriftet ist.
                IsToManyData = expandedChartWidth > maxChartWidth ? true : false;

                void CalculateAdjustedDimensionsForSmartphone()
                {
                    double screenWidth = DeviceDisplay.Current.MainDisplayInfo.Width; // Bildschirmbreite in Pixel
                    double screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height; // Bildschirmhöhe in Pixel
                    double density = DeviceDisplay.Current.MainDisplayInfo.Density; // Pixeldichte

                    adjustedWidth = (int)(screenWidth / density); // Berechnete Breite in DP
                    adjustedHeight = (int)(screenHeight / density); // Berechnete Höhe in DP
                }

                void CalculateAdjustedDimensionsForPC()
                {
                    var width = Application.Current?.Windows.FirstOrDefault()?.Width;
                    if (width != null)
                        adjustedWidth = (int)width; // Breite des aktuellen Fensters
                    else
                        adjustedWidth = 1000;

                    var height = Application.Current?.Windows.FirstOrDefault()?.Height;

                    if (height != null)
                    {
                        adjustedHeight = (int)height; // Höhe des aktuellen Fensters
                    }
                    else
                    {
                        adjustedHeight = 250;
                    }
                }
            }
        }

        bool isInitPeriod = false; // Flag, ob die Periode bereits initialisiert wurde
        public void InitPeriod()
        {
            if (Data.Count == 0)
            {
                DateStart = DateTime.Now;
                DateEnd = DateTime.Now;
                return;
            }
            DateStart = Data.Min(d => d.Date);
            DateEnd = Data.Max(d => d.Date);
            isInitPeriod = true; // Periode wurde initialisiert
        }

        /// <summary>
        /// Zeigt das Label "Keine Daten vorhanden" an, indem die IsVisible-Eigenschaft auf true gesetzt wird, falls keine Daten vorliegen.
        /// </summary>
        public void SetContentVisibility()
        {
            if (DataPeriod != null && DataPeriod.Count() > 0)
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

        void FilterDataByPeriod()
        {
            if (!isInitPeriod) //Die InitPeriode muss zuerst ausgeführt werden, damit die Start- und Enddaten gesetzt werden können.
            {
                return;
            }

            if (Data.Count > 0)
            {
                DataPeriod = Data.Where(d => d.Date >= DateStart && d.Date <= DateEnd).ToList();
                UpdateChart();
            }

            SetContentVisibility();
        }
    }
}