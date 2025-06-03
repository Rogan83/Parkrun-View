using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Parkrun_View.MVVM.Helpers
{
    internal static class NavigationHelper
    {
        public static string LastPageRoute { get; set; } = String.Empty;

        public static ICommand GoToSettingsCommand { get; } = new Command(async () =>
        {
            NavigationHelper.LastPageRoute = Shell.Current.CurrentState.Location.ToString();
            await Shell.Current.GoToAsync("//SettingsPage");
        });
    }
}
