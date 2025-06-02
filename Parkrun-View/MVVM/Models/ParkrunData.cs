using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkrun_View.MVVM.Models
{
    internal class ParkrunData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int ParkrunNr { get; set; } // Beschreibt, der wie vielte Parkrun es ist
        public string Name { get; set; } = string.Empty; // Name des Läufers
        public string AgeGroup { get; set; } = string.Empty; // Altersgruppe des Läufers
        public string Gender { get; set; } = string.Empty; // Geschlecht des Läufers
        public int Runs { get; set; } // Anzahl der Läufe
        public float AgeGrade { get; set; } // Altersanpassung in Prozent
        public TimeSpan PersonalBest { get; set; } // Persönliche Bestzeit
        public TimeSpan PersonalWorst { get; set; } // Persönliche schlechteste Zeit

        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string CourseName { get; set; } = string.Empty;      // Name der Laufstrecke
        public double DistanceKm { get; set; }                      // Länge der Strecke in Km

        public override string ToString()
        {
            return $"{Date.ToShortDateString()} - {Time:mm\\:ss}";
        }
    }
}
