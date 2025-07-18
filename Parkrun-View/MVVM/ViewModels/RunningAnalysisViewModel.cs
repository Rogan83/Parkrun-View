﻿using Parkrun_View.MVVM.Helpers;
using Parkrun_View.MVVM.Interfaces;
using Parkrun_View.MVVM.Models;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Parkrun_View.MVVM.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class RunningAnalysisViewModel : ILoadableViewModel
    {
        public List<ParkrunData> Data { get; set; } = new List<ParkrunData>();

        public ParkrunData SelectedRun { get; set; } = new ParkrunData();

        public double FontSize { get; set; } = 16;

        private int parkrunIndex;
        public int ParkrunIndex
        {
            get => parkrunIndex;
            set
            {
                parkrunIndex = value;
                CalculateStatistics();
            }
        }

        public double KmH { get; set; } = 0;
        public double MaxKmH { get; set; } = 0;
        public double MinKmH { get; set; } = 0;
        public double MS { get; set; } = 0;
        public double BestTimeMS { get; set; } = 0;

        public int NumberOfRuns { get; set; }

        public int BestTimeInHours { get; set; }
        public int BestTimeInMinutes { get; set; }
        public int BestTimeInSeconds { get; set; }

        public int WorstTimeInHours { get; set; }
        public int WorstTimeInMinutes { get; set; }
        public int WorstTimeInSeconds { get; set; }

        // Wenn keine Daten vorhanden sind, dann sollen die Flags dementsprechend zugewiesen werden und der passende Text dazu in der dazugehörigen xaml ausgegeben werden.
        public bool isDataEmpty { get; set; }
        public bool isDataAvailable { get; set; }

        public bool IsLoading { get; set; } // Wenn true, dann wird der Ladespinner angezeigt, wenn die Seite verlassen wird

        public ICommand CreateAnalysis { get; }

        public ICommand GoToSettingsCommand { get; } = NavigationHelper.GoToSettingsCommand;

        public RunningAnalysisViewModel()
        {
            CreateAnalysis = new Command(() =>
            {
                CalculateStatistics();
            });

            SetContentVisibility();
        }
        /// <summary>
        /// Zeigt das Label "Keine Daten vorhanden" an, indem die IsVisible-Eigenschaft auf true gesetzt wird, falls keine Daten vorliegen.
        /// </summary>
        public void CalculateStatistics()
        {
            if (Data.Count > 0 && parkrunIndex < Data.Count)
            {
                KmH = Data[parkrunIndex].DistanceKm / Data[parkrunIndex].Time.TotalHours;
                MS = Data[parkrunIndex].DistanceKm * 1000 / Data[parkrunIndex].Time.TotalSeconds;
                NumberOfRuns = Data.Count;

                CalculateTimes();

                MaxKmH = Data.Max(d => d.DistanceKm / d.Time.TotalHours);
                MinKmH = Data.Min(d => d.DistanceKm / d.Time.TotalHours);
                BestTimeMS = Data.Min(d => d.DistanceKm * 1000 / d.Time.TotalSeconds);
            }
            
            

            void CalculateTimes()
            {
                var bestTime = Data.Min(d => d.Time.TotalHours);
                BestTimeInHours = (int)bestTime;
                BestTimeInMinutes = (int)((bestTime - BestTimeInHours) * 60);
                BestTimeInSeconds = (int)((bestTime - BestTimeInHours - (BestTimeInMinutes / 60d)) * 3600);

                var worstTime = Data.Max(d => d.Time.TotalHours);
                WorstTimeInHours = (int)worstTime;
                WorstTimeInMinutes = (int)((worstTime - WorstTimeInHours) * 60);
                WorstTimeInSeconds = (int)((worstTime - WorstTimeInHours - (WorstTimeInMinutes / 60d)) * 3600);
            }
        }

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
    }
}
