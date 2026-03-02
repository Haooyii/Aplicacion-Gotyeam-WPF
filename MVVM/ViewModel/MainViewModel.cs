using System;

namespace Gotyeam.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        // ViewModels
        public GamesViewModel GamesVM { get; set; }
        public UsersViewModel UsersVM { get; set; }
        public EmployeesViewModel EmployeesVM { get; set; }
        public ProfileViewModel ProfileVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }
        public AboutViewModel AboutVM { get; set; }

        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public RelayCommand GamesViewCommand { get; set; }
        public RelayCommand UsersViewCommand { get; set; }
        public RelayCommand EmployeesViewCommand { get; set; }
        public RelayCommand ProfileViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand AboutViewCommand { get; set; }

        public MainViewModel()
        {
            GamesVM = new GamesViewModel();
            UsersVM = new UsersViewModel();
            EmployeesVM = new EmployeesViewModel();
            ProfileVM = new ProfileViewModel();
            SettingsVM = new SettingsViewModel();
            SettingsVM.InitializeSettings(); // Cargar la música y ventana sabiendo quién se logueó
            AboutVM = new AboutViewModel();

            // Default view
            CurrentView = GamesVM;

            // Command logic
            GamesViewCommand = new RelayCommand(o => { 
                CurrentView = GamesVM; 
                if (GamesVM.IsShowingMisJuegos)
                {
                    _ = GamesVM.CargarMisJuegos();
                }
            });
            UsersViewCommand = new RelayCommand(o => { CurrentView = UsersVM; });
            EmployeesViewCommand = new RelayCommand(o => { CurrentView = EmployeesVM; });
            ProfileViewCommand = new RelayCommand(o => { CurrentView = ProfileVM; });
            SettingsViewCommand = new RelayCommand(o => { CurrentView = SettingsVM; });
            AboutViewCommand = new RelayCommand(o => { CurrentView = AboutVM; });
        }
    }
}
