using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Gotyeam.MVVM.Model;
using Gotyeam.Services;

namespace Gotyeam.MVVM.ViewModel
{
    public class GamesViewModel : ViewModelBase
    {
        private RawgService _rawgService;
        private GamesService _gamesService;

        public ObservableCollection<RawgGame> ResultadosBusqueda { get; set; }
        public ObservableCollection<JuegoModel> MisJuegos { get; set; }
        public ObservableCollection<ResennaModel> ResennasJuego { get; set; }
        public ObservableCollection<string> FiltrosEstado { get; set; }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
            }
        }

        private bool _isShowingMisJuegos;
        public bool IsShowingMisJuegos
        {
            get => _isShowingMisJuegos;
            set
            {
                _isShowingMisJuegos = value;
                OnPropertyChanged();
                if (value)
                {
                    _ = CargarMisJuegos();
                }
            }
        }

        private string _selectedFiltroEstado;
        public string SelectedFiltroEstado
        {
            get => _selectedFiltroEstado;
            set
            {
                _selectedFiltroEstado = value;
                OnPropertyChanged();
                if (IsShowingMisJuegos)
                {
                    _ = CargarMisJuegos();
                }
            }
        }

        private RawgGame _selectedRawgGame;
        public RawgGame SelectedRawgGame
        {
            get => _selectedRawgGame;
            set
            {
                _selectedRawgGame = value;
                OnPropertyChanged();
                if (value != null)
                {
                    _ = MostrarDetalleRawg(value.id);
                }
            }
        }

        private JuegoModel _selectedMiJuego;
        public JuegoModel SelectedMiJuego
        {
            get => _selectedMiJuego;
            set
            {
                _selectedMiJuego = value;
                OnPropertyChanged();
                if (value != null)
                {
                    _ = MostrarDetalleMiJuego(value);
                }
            }
        }

        // Propiedades de Detalle
        private JuegoModel _juegoDetalle;
        public JuegoModel JuegoDetalle
        {
            get => _juegoDetalle;
            set { _juegoDetalle = value; OnPropertyChanged(); }
        }

        private bool _isDetailVisible;
        public bool IsDetailVisible
        {
            get => _isDetailVisible;
            set { _isDetailVisible = value; OnPropertyChanged(); }
        }

        private bool _canEdit;
        public bool CanEdit
        {
            get => _canEdit;
            set { _canEdit = value; OnPropertyChanged(); }
        }

        private bool _isAddingNew;
        public bool IsAddingNew
        {
            get => _isAddingNew;
            set { _isAddingNew = value; OnPropertyChanged(); }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { _isReadOnly = value; OnPropertyChanged(); }
        }

        public bool CanManageGames => NivelRol == 1 || NivelRol == 4;

        // Commands
        public RelayCommand BuscarRawgCommand { get; set; }
        public RelayCommand SwitchToMisJuegosCommand { get; set; }
        public RelayCommand SwitchToSearchCommand { get; set; }
        public RelayCommand AddGameCommand { get; set; }
        public RelayCommand UpdateGameCommand { get; set; }
        public RelayCommand CloseDetailCommand { get; set; }
        public RelayCommand ToggleGameStatusCommand { get; set; }

        // Mensajes de feedback
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public GamesViewModel()
        {
            _rawgService = new RawgService();
            _gamesService = new GamesService();

            ResultadosBusqueda = new ObservableCollection<RawgGame>();
            MisJuegos = new ObservableCollection<JuegoModel>();
            ResennasJuego = new ObservableCollection<ResennaModel>();
            FiltrosEstado = new ObservableCollection<string> { "Activos", "Inactivos", "Todos" };
            SelectedFiltroEstado = "Activos";

            BuscarRawgCommand = new RelayCommand(async o => await BuscarEnRawg());
            SwitchToMisJuegosCommand = new RelayCommand(o => { IsShowingMisJuegos = true; IsDetailVisible = false; });
            SwitchToSearchCommand = new RelayCommand(o => { IsShowingMisJuegos = false; IsDetailVisible = false; });
            
            AddGameCommand = new RelayCommand(async o => await AddGameToDb());
            UpdateGameCommand = new RelayCommand(async o => await UpdateGameInDb());
            CloseDetailCommand = new RelayCommand(o => { IsDetailVisible = false; });
            ToggleGameStatusCommand = new RelayCommand(async o => await ToggleGameStatus());

            // Cargar inicial
            IsShowingMisJuegos = false;
        }

        private async Task BuscarEnRawg()
        {
            StatusMessage = "Buscando...";
            var resultados = await _rawgService.BuscarJuegosAsync(TextoBusqueda);
            ResultadosBusqueda.Clear();
            foreach (var r in resultados)
            {
                ResultadosBusqueda.Add(r);
            }
            StatusMessage = resultados.Any() ? "Búsqueda completada." : "No se encontraron resultados.";
        }

        public async Task CargarMisJuegos()
        {
            StatusMessage = "Cargando mis juegos...";
            string estadoParam = SelectedFiltroEstado.ToLower();
            if (estadoParam == "activos") estadoParam = "activo";
            if (estadoParam == "inactivos") estadoParam = "inactivo";

            var juegos = await _gamesService.ListarJuegosAsync(estadoParam);
            MisJuegos.Clear();
            foreach (var j in juegos)
            {
                MisJuegos.Add(j);
            }
            StatusMessage = "Juegos cargados.";
        }

        private async Task MostrarDetalleRawg(int rawgId)
        {
            StatusMessage = "Cargando detalles desde RAWG...";
            var detalle = await _rawgService.ObtenerDetallesJuegoAsync(rawgId);
            if (detalle != null)
            {
                var genreList = detalle.genres?.Select(g => g.name).ToList() ?? new System.Collections.Generic.List<string>();
                
                JuegoDetalle = new JuegoModel
                {
                    apiRawg_id = detalle.id,
                    titulo = detalle.name,
                    portada_url = detalle.background_image,
                    fecha_lanzamiento = detalle.released,
                    descripcion = !string.IsNullOrEmpty(detalle.description_raw) ? detalle.description_raw : "Sin descripción.",
                    generos = genreList,
                    generos_string = string.Join(", ", genreList),
                    desarrolladora = detalle.developers?.FirstOrDefault()?.name ?? "Desconocida",
                    activo = true // Default to active when adding from RAWG
                };
                CanEdit = false; // Add mode uses CanEdit=false to hide "Save/Toggle" buttons
                IsAddingNew = true; // Add mode uses IsAddingNew=true to show the "Add" button
                IsReadOnly = true; // Still read-only except for maybe some specific fields, or keep as is.
                IsDetailVisible = true;
                StatusMessage = "";
            }
            else
            {
                StatusMessage = "Error al obtener detalles.";
            }
        }

        private async Task MostrarDetalleMiJuego(JuegoModel juegoDb)
        {
            StatusMessage = "Cargando detalles del juego...";
            var fullJuego = await _gamesService.ObtenerDetalleJuegoAsync(juegoDb.id);
            if (fullJuego != null)
            {
                juegoDb = fullJuego;
            }

            // Clonamos para la edición
            var listaGeneros = juegoDb.generos != null ? new System.Collections.Generic.List<string>(juegoDb.generos) : new System.Collections.Generic.List<string>();
            JuegoDetalle = new JuegoModel
            {
                id = juegoDb.id,
                apiRawg_id = juegoDb.apiRawg_id,
                titulo = juegoDb.titulo,
                descripcion = juegoDb.descripcion,
                fecha_lanzamiento = juegoDb.fecha_lanzamiento,
                portada_url = juegoDb.portada_url,
                generos = listaGeneros,
                generos_string = string.Join(", ", listaGeneros),
                activo = juegoDb.activo,
                valoracion_media = juegoDb.valoracion_media,
                cantidad_resennas = juegoDb.cantidad_resennas
            };
            CanEdit = CanManageGames;
            IsAddingNew = false;
            IsReadOnly = !CanManageGames;
            IsDetailVisible = true;
            StatusMessage = "Detalles cargados.";
            _ = CargarResennasJuego(juegoDb.id);
        }

        private async Task CargarResennasJuego(string idJuego)
        {
            if (string.IsNullOrEmpty(idJuego)) return;
            var resennas = await _gamesService.ListarResennasDeJuegoAsync(idJuego);
            ResennasJuego.Clear();
            foreach (var r in resennas)
            {
                ResennasJuego.Add(r);
            }
        }



        private async Task ToggleGameStatus()
        {
            if (JuegoDetalle == null || string.IsNullOrEmpty(JuegoDetalle.id)) return;
            var result = MessageBox.Show($"¿Deseas {(JuegoDetalle.activo ? "desactivar" : "activar")} {JuegoDetalle.titulo}?", "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = "Cambiando estado del juego...";
                bool success = await _gamesService.EditarEstadoJuegoAsync(JuegoDetalle.id);
                if (success)
                {
                    StatusMessage = "Estado del juego actualizado.";
                    await CargarMisJuegos();
                    IsDetailVisible = false;
                }
                else
                {
                    StatusMessage = "Error al cambiar estado del juego.";
                }
            }
        }

        private async Task AddGameToDb()
        {
            if (JuegoDetalle == null || CanEdit) return;

            StatusMessage = "Añadiendo juego a la BD...";
            if (!string.IsNullOrWhiteSpace(JuegoDetalle.generos_string))
            {
                JuegoDetalle.generos = JuegoDetalle.generos_string
                    .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Where(g => !string.IsNullOrEmpty(g))
                    .ToList();
            }

            var result = await _gamesService.AnnadirJuegoAsync(JuegoDetalle);
            if (result.Item1)
            {
                StatusMessage = "¡Juego añadido exitosamente!";
                MessageBox.Show("Juego agregado a tu base de datos.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                IsDetailVisible = false;
            }
            else
            {
                StatusMessage = "Error al añadir juego.";
                MessageBox.Show($"El juego no pudo ser guardado. Detalle del servidor:\n\n{result.Item2}", "Error de API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateGameInDb()
        {
            if (JuegoDetalle == null || !CanEdit) return;

            StatusMessage = "Actualizando juego...";
            if (!string.IsNullOrWhiteSpace(JuegoDetalle.generos_string))
            {
                JuegoDetalle.generos = JuegoDetalle.generos_string
                    .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Where(g => !string.IsNullOrEmpty(g))
                    .ToList();
            }
            // Avoid sending modifications of Cover/Title by enforcing them to null or relying on backend handling
            var juegoParaActualizar = new JuegoModel
            {
                id = JuegoDetalle.id,
                descripcion = JuegoDetalle.descripcion,
                fecha_lanzamiento = JuegoDetalle.fecha_lanzamiento,
                generos = JuegoDetalle.generos
            };

            var success = await _gamesService.EditarJuegoAsync(JuegoDetalle.id, juegoParaActualizar);
            if (success)
            {
                StatusMessage = "¡Juego actualizado!";
                MessageBox.Show("Juego actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await CargarMisJuegos(); // Refrescar lista
                IsDetailVisible = false;
            }
            else
            {
                StatusMessage = "Error al actualizar juego.";
                MessageBox.Show("Hubo un error al guardar los cambios.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
