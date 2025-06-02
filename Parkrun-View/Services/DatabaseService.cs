using Parkrun_View.MVVM.Helpers;
using Parkrun_View.MVVM.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkrun_View.Services
{
    internal class DatabaseService
    {
        public readonly static SQLiteAsyncConnection database;

        static DatabaseService()
        {
            database = new SQLiteAsyncConnection(DatabaseConfig.DatabasePath);
            database.CreateTableAsync<ParkrunData>().Wait();
            //CreateTableForTrackAsync().GetAwaiter().GetResult(); // Tabelle wird über die normale SQL Syntax erstellt. Funktioniert nicht.
        }
        #region direkte SQL-Abfragen
        static async Task CreateTableForTrackAsync()
        {
            try
            {
                if (database == null)
                {
                    Console.WriteLine("Error: Database connection is null!");
                    return;
                }

                var tables = await database.QueryAsync<object>(@"
SELECT name FROM sqlite_master WHERE type='table'");

                foreach (var table in tables)
                {
                    Console.WriteLine($"Found table: {table}");
                }

                Console.WriteLine("Checking if table 'priessnitzgrund' exists...");

                var tableExists = await database.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='priessnitzgrund'");

                if (tableExists > 0)
                {
                    Console.WriteLine("Table 'priessnitzgrund' already exists.");
                    return;
                }

                Console.WriteLine("Creating table 'priessnitzgrund'...");

                await database.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS priessnitzgrund (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                ParkrunNr INTEGER, 
                AgeGroup TEXT, 
                Gender TEXT, 
                Runs INTEGER, 
                AgeGrade REAL, 
                PersonalBest TEXT, 
                PersonalWorst TEXT, 
                Date TEXT, 
                Time TEXT, 
                DistanceKm REAL
            )");

                Console.WriteLine("Table 'priessnitzgrund' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating table 'priessnitzgrund': {ex.Message}");
            }
        }


        public static Task<List<ParkrunData>> GetDataAsync(string trackName = "priessnitzgrund")
        {
            return database.QueryAsync<ParkrunData>($"SELECT * FROM {trackName}");
        }

        public static List<ParkrunData> GetDataSync(string trackName = "priessnitzgrund")
        {
            return database.QueryAsync<ParkrunData>($"SELECT * FROM {trackName}").GetAwaiter().GetResult();
        }

        public static Task<int> SaveDataAsync(ParkrunData data, string trackName = "priessnitzgrund")
        {
            return database.ExecuteAsync($"INSERT INTO {trackName} (ParkrunNr, AgeGroup, Gender, Runs, AgeGrade, PersonalBest, PersonalWorst, Date, Time, DistanceKm) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                data.ParkrunNr, data.AgeGroup, data.Gender, data.Runs, data.AgeGrade, data.PersonalBest, data.PersonalWorst, data.Date, data.Time, data.DistanceKm);
        }

        public static Task<int> DeleteDataAsync(ParkrunData data, string trackName = "priessnitzgrund")
        {
            return database.ExecuteAsync($"DELETE FROM {trackName} WHERE Id = ?", data.Id);
        }

        public static Task<int> DeleteAllDataAsync(string trackName = "priessnitzgrund")
        {
            return database.ExecuteAsync($"DELETE FROM {trackName}");
        }
        #endregion


        public static Task<List<ParkrunData>> GetDataAsync()
        {
            return database.Table<ParkrunData>().ToListAsync();
        }

        public static List<ParkrunData> GetDataSync()
        {
            return database.Table<ParkrunData>().ToListAsync().GetAwaiter().GetResult();
        }

        public static Task<int> SaveDataAsync(ParkrunData data)
        {
            return database.InsertAsync(data);
        }

        public static Task<int> DeleteDataAsync(ParkrunData data)
        {
            return database.DeleteAsync(data);
        }

        public static Task<int> DeleteAllDataAsync()
        {
            return database.DeleteAllAsync<ParkrunData>();
        }
    }
}
