using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Gotyeam.MVVM.Model;
using Gotyeam.Services;

namespace Gotyeam.MVVM.ViewModel
{
    public class EmployeesViewModel : ViewModelBase
    {
        private EmployeesService _employeesService;

        public ObservableCollection<PerfilEmpleado> Empleados { get; set; }
        public ObservableCollection<string> FiltrosEstado { get; set; }
        public ObservableCollection<int> RolesDisponibles { get; set; }

        public bool CanManageEmployees => NivelRol == 3 || NivelRol == 4;

        private PerfilEmpleado _selectedEmpleado;
        public PerfilEmpleado SelectedEmpleado
        {
            get => _selectedEmpleado;
            set { _selectedEmpleado = value; OnPropertyChanged(); }
        }

        private PerfilEmpleado _empleadoForm;
        public PerfilEmpleado EmpleadoForm
        {
            get => _empleadoForm;
            set { _empleadoForm = value; OnPropertyChanged(); }
        }

        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set { _isFormVisible = value; OnPropertyChanged(); }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
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
                _ = CargarEmpleados();
            }
        }

        public RelayCommand BuscarEmpleadosCommand { get; set; }
        public RelayCommand NuevoEmpleadoCommand { get; set; }
        public RelayCommand EditarEmpleadoCommand { get; set; }
        public RelayCommand GuardarEmpleadoCommand { get; set; }
        public RelayCommand ToggleEstadoCommand { get; set; }
        public RelayCommand CancelarFormCommand { get; set; }
        public RelayCommand SeleccionarImagenCommand { get; set; }

        public EmployeesViewModel()
        {
            _employeesService = new EmployeesService();
            Empleados = new ObservableCollection<PerfilEmpleado>();
            FiltrosEstado = new ObservableCollection<string> { "Activos", "Inactivos", "Todos" };
            RolesDisponibles = new ObservableCollection<int>();
            _selectedFiltroEstado = "Activos"; // initial avoid trigger

            if (NivelRol == 3)
            {
                RolesDisponibles.Add(1);
                RolesDisponibles.Add(2);
            }
            else if (NivelRol == 4)
            {
                RolesDisponibles.Add(1);
                RolesDisponibles.Add(2);
                RolesDisponibles.Add(3);
                RolesDisponibles.Add(4);
            }

            BuscarEmpleadosCommand = new RelayCommand(async o => await CargarEmpleados());
            NuevoEmpleadoCommand = new RelayCommand(o => IniciarCreacion());
            EditarEmpleadoCommand = new RelayCommand(o => IniciarEdicion(o as PerfilEmpleado));
            GuardarEmpleadoCommand = new RelayCommand(async o => await GuardarEmpleado());
            ToggleEstadoCommand = new RelayCommand(async o => await AlternarEstado(o as PerfilEmpleado));
            CancelarFormCommand = new RelayCommand(o => { IsFormVisible = false; });
            SeleccionarImagenCommand = new RelayCommand(o => SeleccionarImagen());

            _ = CargarEmpleados();
        }

        private async Task CargarEmpleados()
        {
            StatusMessage = "Cargando empleados...";
            string estadoParam = SelectedFiltroEstado?.ToLower() ?? "activo";
            if (estadoParam == "activos") estadoParam = "activo";
            if (estadoParam == "inactivos") estadoParam = "inactivo";

            var lista = await _employeesService.ListarEmpleadosAsync(estadoParam, TextoBusqueda);
            Empleados.Clear();

            // Handle potential 403 empty lists silently but display a message
            if (lista.Count == 0 && string.IsNullOrEmpty(TextoBusqueda)) 
            {
                StatusMessage = "Sin acceso o sin empleados.";
            }
            else
            {
                foreach (var e in lista)
                {
                    if (e._id == IdEmpleado) continue;

                    if (NivelRol >= 4)
                        e.IsEditable = true;
                    else if (NivelRol == 3 && e.nivel_rol <= 2)
                        e.IsEditable = true;
                    else
                        e.IsEditable = false;

                    Empleados.Add(e);
                }
                StatusMessage = $"Lista cargada: {Empleados.Count} empleados.";
            }
        }

        private void IniciarCreacion()
        {
            EmpleadoForm = new PerfilEmpleado
            {
                nivel_rol = 1,
                foto_url = "https://via.placeholder.com/150"
            };
            IsEditing = false;
            IsFormVisible = true;
        }

        private void SeleccionarImagen()
        {
            if (EmpleadoForm == null) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar Imagen de Perfil",
                Filter = "Archivos de imagen (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos los archivos (*.*)|*.*",
                InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;
                    byte[] imageBytes = File.ReadAllBytes(filePath);
                    string extension = Path.GetExtension(filePath).ToLower();
                    
                    // Determine mime type
                    string mimeType = "image/jpeg";
                    if (extension == ".png") mimeType = "image/png";

                    string base64String = System.Convert.ToBase64String(imageBytes);
                    
                    // Store as data URI
                    EmpleadoForm.foto_url = $"data:{mimeType};base64,{base64String}";
                    OnPropertyChanged(nameof(EmpleadoForm));
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void IniciarEdicion(PerfilEmpleado empleado)
        {
            if (empleado == null) return;

            if (NivelRol < 4 && !(NivelRol == 3 && empleado.nivel_rol <= 2))
            {
                MessageBox.Show("No tienes permisos para modificar este empleado.", "Permiso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            EmpleadoForm = new PerfilEmpleado
            {
                _id = empleado._id,
                nombre = empleado.nombre,
                apellido1 = empleado.apellido1,
                apellido2 = empleado.apellido2,
                correo = empleado.correo,
                foto_url = empleado.foto_url,
                nivel_rol = empleado.nivel_rol,
                activo = empleado.activo
            };
            IsEditing = true;
            IsFormVisible = true;
        }

        private async Task GuardarEmpleado()
        {
            if (EmpleadoForm == null) return;

            StatusMessage = IsEditing ? "Actualizando empleado..." : "Registrando empleado...";
            bool success = false;

            if (IsEditing)
            {
                success = await _employeesService.EditarEmpleadoAsync(EmpleadoForm._id, EmpleadoForm);
            }
            else
            {
                success = await _employeesService.RegistrarEmpleadoAsync(EmpleadoForm);
            }

            if (success)
            {
                StatusMessage = IsEditing ? "Empleado actualizado." : "Empleado registrado exitosamente.";
                IsFormVisible = false;
                await CargarEmpleados();
            }
            else
            {
                StatusMessage = "Error al guardar empleado. Verifica los datos y requerimientos de rol.";
                MessageBox.Show("Ocurrió un error (ej. permisos insuficientes o el correo ya existe).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AlternarEstado(PerfilEmpleado empleado)
        {
            if (empleado == null) return;

            if (NivelRol < 4 && !(NivelRol == 3 && empleado.nivel_rol <= 2))
            {
                MessageBox.Show("No tienes permisos para cambiar el estado de este empleado.", "Permiso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"¿Cambiar estado del empleado {empleado.NombreCompleto}?", 
                                         "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = "Cambiando estado...";
                bool success = await _employeesService.CambiarEstadoEmpleadoAsync(empleado._id);
                if (success)
                {
                    StatusMessage = "Estado modificado exitosamente.";
                    await CargarEmpleados();
                }
                else
                {
                    StatusMessage = "Error al cambiar de estado. O puede que estés intentando cambiar el tuyo propio.";
                }
            }
        }
    }
}
