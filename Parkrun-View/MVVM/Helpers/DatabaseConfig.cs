using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkrun_View.MVVM.Helpers
{
    internal class DatabaseConfig
    {
        public const string DatabaseFilename = "ParkrunData.db3";
        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
    }
}
