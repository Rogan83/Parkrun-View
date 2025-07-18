using Parkrun_View.MVVM.Helpers;
using Parkrun_View.MVVM.Interfaces;
using Parkrun_View.MVVM.ViewModels;
using Parkrun_View.MVVM.Views;

namespace Parkrun_View
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            this.Navigating += OnShellNavigating;
        }
        private bool navigatingInternally = false;
        private async void OnShellNavigating(object sender, ShellNavigatingEventArgs e)
        {
            if (navigatingInternally)
                return;

            if (e.Source == ShellNavigationSource.ShellItemChanged)
            {
                var currentPage = Shell.Current.CurrentPage;

                e.Cancel(); // ⛔ Abbrechen der automatischen Navigation

                if (currentPage?.BindingContext is ILoadableViewModel vm)
                {
                    vm.IsLoading = true; // Zeigt einen Ladeindikator an, während die Daten geladen werden
                    await NavigationHelper.LoadFilteredParkrunDataAsync();
                    vm.IsLoading = false; // deaktiviert diesen wieder
                }
            }

            navigatingInternally = true;
            await Shell.Current.GoToAsync(e.Target.Location.OriginalString); // ⛳ Manuell navigieren nach dem Laden
            navigatingInternally = false;
        }
    }
}
