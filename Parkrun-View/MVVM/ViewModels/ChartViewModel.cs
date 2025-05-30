using Parkrun_View.MVVM.Models;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microcharts;
using SkiaSharp;        // Wird für die Grafiken benötigt

namespace Parkrun_View.MVVM.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class ChartViewModel
    {
        public List<ParkrunData> Data { get; set; } = new List<ParkrunData>();
        public Chart LineChart { get; private set; }

        public int ChartWidth { get; set; }
        int maxChartWidth = 0;               // Maximale Breite, die der Linechart haben darf, damit die Infos zu jeden Datenpunkt vollständig zu sehen sind.
        public int ChartHeight { get; set; }

        public bool IsToManyData { get; set; }  // Wenn zu viele Daten vorhanden sind, dann gibt es die Möglichkeit zwischen einer kompakten und einer detaillierten Ansicht zu wechseln.
        bool isCompactView = false;
        public string IsCompleteLabelName { get; set; } = "Detailansicht"; // Label für die Ansicht
        public ICommand ToggleViewModus { get; set; }
        public ChartViewModel()
        {
            LineChart = new LineChart();

            ToggleViewModus = new Command((parkrunData) =>
            {
                isCompactView = !isCompactView;
                IsCompleteLabelName = isCompactView ? "Kompaktansicht" : "Detailansicht";

                UpdateChartDimensions(); // Breite aktualisieren
                UpdateChart();
            });

            DeviceDisplay.Current.MainDisplayInfoChanged += OnDisplayChanged;

        }
        //Wird ausgerufen, wenn das DisplayInfo sich ändert, z.B. bei der Drehung des Bildschirms
        void OnDisplayChanged(object? sender, DisplayInfoChangedEventArgs e)
        {
            UpdateChartDimensions(); // Aktualisiert die Breite auch nach dem drehen des Bildschirms
            UpdateChart();
        }

        int expandedChartWidth = 0; // Breite der Detailansicht
        public void UpdateChartDimensions()
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

            expandedChartWidth = Data.Count * dataPointWidth;   // Die Gesamtbreite des Diagramms, welches mit einem horizontalen Balken angeschaut werden kann, wird anhand der Anzahl der Datenpunkte und der Breite pro Datenpunkt berechnet.

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


        /// <summary>
        /// Aktualisiert das Diagramm.
        /// </summary>
        public void UpdateChart()
        {
            List<ChartEntry> entries;
            if (Data.Count == 0)
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
                Data = Data.OrderBy(d => d.Date).ToList();

                var maxTime = Data.Max(d => d.Time.TotalSeconds); // Höchster Wert bestimmen
                var minTime = Data.Min(d => d.Time.TotalSeconds); // Niedrigsten Wert bestimmen
                entries = Data.Select((result, index) =>
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
                        color = SKColor.Parse("#f1c40f"); // Gelb für normale Zeiten
                    }
                    // Wenn die Detailansicht aktiv ist oder die Breite von der kompletten Ansicht kleiner als die maximale Breite ist, dann wird das Datum und die Zeit angezeigt
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
            ///
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
                LabelTextSize = 30,

                //MaxValue = 1000,  // Höchster Wert ein wenig über deinem höchsten Punkt setzen
                //MinValue = 0   // Niedrigster Wert nahe deinem kleinsten Punkt setzen
            };
        }
    }
}