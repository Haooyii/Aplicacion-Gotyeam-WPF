using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Gotyeam.MVVM.Model;
using Gotyeam.Services;

namespace Gotyeam.MVVM.ViewModel
{
    public class UsersViewModel : ViewModelBase
    {
        private UsersService _usersService;

        public ObservableCollection<UserModel> Usuarios { get; set; }
        public ObservableCollection<ResennaModel> ResennasUsuario { get; set; }
        public ObservableCollection<string> FiltrosEstado { get; set; }

        private UserModel _selectedUsuario;
        public UserModel SelectedUsuario
        {
            get => _selectedUsuario;
            set
            {
                _selectedUsuario = value;
                OnPropertyChanged();
                IsDetailVisible = value != null;
                
                if (value != null && !string.IsNullOrEmpty(value._id))
                {
                    IsCreatingUsuario = false;
                    _ = CargarResennasDelUsuario(value._id);
                }
            }
        }

        private bool _isCreatingUsuario;
        public bool IsCreatingUsuario
        {
            get => _isCreatingUsuario;
            set { _isCreatingUsuario = value; OnPropertyChanged(); }
        }

        private bool _isDetailVisible;
        public bool IsDetailVisible
        {
            get => _isDetailVisible;
            set { _isDetailVisible = value; OnPropertyChanged(); }
        }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; OnPropertyChanged(); }
        }

        private string _selectedFiltroEstado;
        public string SelectedFiltroEstado
        {
            get => _selectedFiltroEstado;
            set
            {
                _selectedFiltroEstado = value;
                OnPropertyChanged();
                _ = CargarUsuarios();
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanManageUsers => NivelRol == 2 || NivelRol == 4;

        public RelayCommand BuscarUsuariosCommand { get; set; }
        public RelayCommand ToggleUserStatusCommand { get; set; }
        public RelayCommand DeleteReviewCommand { get; set; }
        public RelayCommand UpdateUserCommand { get; set; }
        public RelayCommand CloseDetailCommand { get; set; }
        public RelayCommand NuevoUsuarioCommand { get; set; }

        public UsersViewModel()
        {
            _usersService = new UsersService();

            Usuarios = new ObservableCollection<UserModel>();
            ResennasUsuario = new ObservableCollection<ResennaModel>();
            FiltrosEstado = new ObservableCollection<string> { "Activos", "Inactivos", "Todos" };
            _selectedFiltroEstado = "Activos"; // initially set directly to avoid triggering twice on startup

            BuscarUsuariosCommand = new RelayCommand(async o => await CargarUsuarios());
            ToggleUserStatusCommand = new RelayCommand(async o => await ToggleUserStatus());
            DeleteReviewCommand = new RelayCommand(async o => await DeleteReview(o as string ?? string.Empty));
            UpdateUserCommand = new RelayCommand(async o => await UpdateUser(o as System.Windows.Controls.PasswordBox));
            NuevoUsuarioCommand = new RelayCommand(o => IniciarCreacionUsuario());
            CloseDetailCommand = new RelayCommand(o => { 
                IsDetailVisible = false;
                SelectedUsuario = null; // Unselect the user
            });

            // Cargar inicial
            _ = CargarUsuarios();
        }

        private void IniciarCreacionUsuario()
        {
            SelectedUsuario = new UserModel
            {
                foto_url = "",
                fecha_registro = System.DateTime.Now.ToString("yyyy-MM-dd")
            };
            ResennasUsuario.Clear();
            IsCreatingUsuario = true;
            IsDetailVisible = true;
        }


        private async Task CargarUsuarios()
        {
            StatusMessage = "Cargando clientes...";
            string estadoParam = SelectedFiltroEstado?.ToLower() ?? "activo";
            if (estadoParam == "activos") estadoParam = "activo";
            if (estadoParam == "inactivos") estadoParam = "inactivo";

            var lista = await _usersService.ListarUsuariosAsync(estadoParam, TextoBusqueda);
            Usuarios.Clear();
            foreach(var u in lista)
            {
                Usuarios.Add(u);
            }
            StatusMessage = $"Se encontraron {Usuarios.Count} clientes.";
        }

        private async Task ToggleUserStatus()
        {
            if (SelectedUsuario == null) return;

            var confirmResult = MessageBox.Show($"¿Deseas {(SelectedUsuario.activo ? "Bloquear" : "Desbloquear")} al usuario {SelectedUsuario.username}?",
                                     "Confirmar",
                                     MessageBoxButton.YesNo,
                                     MessageBoxImage.Question);
                                     
            if (confirmResult == MessageBoxResult.Yes)
            {
                StatusMessage = "Cambiando estado del cliente...";
                bool success = await _usersService.CambiarEstadoUsuarioAsync(SelectedUsuario._id);
                if (success)
                {
                    StatusMessage = "Estado cambiado exitosamente.";
                    await CargarUsuarios();
                    IsDetailVisible = false; // Ocultar detalle para forzar resellección
                }
                else
                {
                    StatusMessage = "Error al cambiar estado.";
                }
            }
        }

        private async Task UpdateUser(System.Windows.Controls.PasswordBox pwdBox = null)
        {
            if (SelectedUsuario == null) return;
            
            if (pwdBox != null && !string.IsNullOrWhiteSpace(pwdBox.Password))
            {
                SelectedUsuario.contrasenna = pwdBox.Password;
            }
            else if (IsCreatingUsuario)
            {
                MessageBox.Show("La contraseña es obligatoria para nuevos usuarios.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                SelectedUsuario.contrasenna = string.Empty;
            }

            StatusMessage = IsCreatingUsuario ? "Registrando usuario..." : "Guardando cambios del usuario...";
            (bool success, string msg) result;

            if (IsCreatingUsuario)
            {
                result = await _usersService.RegistrarUsuarioAsync(SelectedUsuario);
            }
            else
            {
                result = await _usersService.EditarUsuarioAsync(SelectedUsuario._id, SelectedUsuario);
            }
            
            if (result.success)
            {
                StatusMessage = result.msg;
                MessageBox.Show(StatusMessage, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await CargarUsuarios();
                IsDetailVisible = false;
            }
            else
            {
                StatusMessage = result.msg;
                MessageBox.Show($"Hubo un problema: {result.msg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            if (pwdBox != null)
            {
                pwdBox.Password = string.Empty;
            }
        }
        


        private async Task CargarResennasDelUsuario(string idUsuario)
        {
            StatusMessage = "Cargando reseñas...";
            var resennas = await _usersService.ListarResennasDeUsuarioAsync(idUsuario);
            ResennasUsuario.Clear();
            foreach (var r in resennas)
            {
                ResennasUsuario.Add(r);
            }
            StatusMessage = $"El cliente tiene {ResennasUsuario.Count} reseñas.";
        }

        private async Task DeleteReview(string idResenna)
        {
            if (string.IsNullOrEmpty(idResenna) || SelectedUsuario == null) return;

            var confirmResult = MessageBox.Show("¿Seguro que deseas eliminar esta reseña?",
                                     "Confirmar Eliminación",
                                     MessageBoxButton.YesNo,
                                     MessageBoxImage.Warning);
                                     
            if (confirmResult == MessageBoxResult.Yes)
            {
                StatusMessage = "Eliminando reseña...";
                bool success = await _usersService.EliminarResennaAsync(idResenna);
                if (success)
                {
                    StatusMessage = "Reseña eliminada.";
                    await CargarResennasDelUsuario(SelectedUsuario._id);
                }
                else
                {
                    StatusMessage = "Error al eliminar reseña.";
                }
            }
        }
    }
}
