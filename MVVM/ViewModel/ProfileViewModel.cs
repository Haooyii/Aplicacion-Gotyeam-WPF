using System.Threading.Tasks;
using System.Windows;
using Gotyeam.MVVM.Model;
using Gotyeam.Services;

namespace Gotyeam.MVVM.ViewModel
{
    public class ProfileViewModel : ViewModelBase
    {
        private GetEmployeeProfile _profileService;
        private EmployeesService _employeesService;

        private PerfilEmpleado _miPerfil;
        public PerfilEmpleado MiPerfil
        {
            get => _miPerfil;
            set { _miPerfil = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand GuardarCambiosCommand { get; set; }

        public ProfileViewModel()
        {
            _profileService = new GetEmployeeProfile();
            _employeesService = new EmployeesService();

            GuardarCambiosCommand = new RelayCommand(async o => await GuardarCambios());

            _ = CargarMiPerfil();
        }

        private async Task CargarMiPerfil()
        {
            StatusMessage = "Cargando perfil...";
            var perfil = await _profileService.GetEmployeeAsync();
            if (perfil != null)
            {
                MiPerfil = perfil;
                StatusMessage = "Perfil cargado.";
            }
            else
            {
                StatusMessage = "No se pudo cargar tu perfil.";
            }
        }



        private async Task GuardarCambios()
        {
            if (MiPerfil == null) return;

            StatusMessage = "Guardando cambios...";
            bool success = await _employeesService.EditarEmpleadoAsync(MiPerfil._id ?? MiPerfil.id, MiPerfil);
            
            if (success)
            {
                StatusMessage = "Perfil actualizado exitosamente.";
                MessageBox.Show("Tu perfil ha sido actualizado. Es posible que debas reiniciar la sesión para ver todos los cambios reflejados.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await CargarMiPerfil();
            }
            else
            {
                StatusMessage = "Error al actualizar perfil.";
                MessageBox.Show("Ocurrió un error al actualizar tu perfil.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
