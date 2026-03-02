using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Gotyeam.MVVM.Model;

namespace Gotyeam.MVVM.ViewModel
{
    public class UserSettings
    {
        public string SelectedWindowSize { get; set; } = "Mediano (1300x700)";
        public bool IsMusicEnabled { get; set; } = true;
        public double MusicVolume { get; set; } = 50.0;
    }

    public class SettingsViewModel : ViewModelBase
    {
        private bool _isMusicEnabled;

        /// <summary>
        /// Indica si la música de fondo está habilitada (mute/unmute).
        /// Al cambiar, se aplican los ajustes de música (play/pause) 
        /// y se guardan las configuraciones del usuario en un archivo JSON.
        /// </summary>
        public bool IsMusicEnabled
        {
            get => _isMusicEnabled;
            set
            {
                _isMusicEnabled = value;
                OnPropertyChanged();
                ApplyMusicSettings();
                SaveSettings();
            }
        }

        private double _musicVolume = 50.0;

        /// <summary>
        /// Volumen actual de la música, en un rango general de 0 a 100.
        /// Al cambiar su valor, ajusta el volumen del MediaPlayer y guarda la configuración.
        /// </summary>
        public double MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = value;
                OnPropertyChanged();
                ApplyMusicSettings();
                SaveSettings();
            }
        }

        private string _selectedWindowSize;
        public string SelectedWindowSize
        {
            get => _selectedWindowSize;
            set
            {
                _selectedWindowSize = value;
                OnPropertyChanged();
                ApplyWindowSize();
                SaveSettings();
            }
        }

        public List<string> WindowSizes { get; } = new List<string> { "Pequeño (1024x768)", "Mediano (1300x700)", "Grande (1920x1080)" };

        public SettingsViewModel()
        {
            // Ya no cargamos los settings aquí en el constructor porque
            // la propiedad "LoggedUserEmail" aún no ha sido guardada.
        }

        /// <summary>
        /// Inicializa las configuraciones cargando el archivo de ajustes del usuario, 
        /// y luego carga y reproduce la pista de música si el archivo existe.
        /// </summary>
        public void InitializeSettings()
        {
            LoadSettings(); // <--- AQUI FALTABA!
            
            // Try to load the audio file initially if it exists in the application's Music folder
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string audioPath = Path.Combine(appDir, "Music", "musica.mp3");
            
            if (!File.Exists(audioPath))
            {
                // Fallback para cuando se ejecuta desde Visual Studio y la carpeta está en la raíz del proyecto
                audioPath = Path.GetFullPath(Path.Combine(appDir, @"..\..\..\Music\musica.mp3"));
            }
            
            if (File.Exists(audioPath) && App.BackgroundMusicPlayer != null)
            {
                App.BackgroundMusicPlayer.Open(new Uri(audioPath));
                ApplyMusicSettings(); // Empezar a reproducir automáticamente si está activado
            }
        }

        /// <summary>
        /// Obtiene la ruta al archivo JSON donde se guardan las configuraciones 
        /// del usuario, utilizando su correo (LoggedUserEmail) como identificador.
        /// De esta forma las preferencias (como la música o resolución) son únicas por usuario.
        /// </summary>
        private string GetSettingsFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string userEmail = Application.Current.Properties["LoggedUserEmail"] as string ?? "default_user";
            
            // Limpiar el email para que sea un nombre de archivo válido
            userEmail = userEmail.Replace("@", "_").Replace(".", "_");
            
            return Path.Combine(appDir, $"settings_{userEmail}.json");
        }

        private void LoadSettings()
        {
            string path = GetSettingsFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    _selectedWindowSize = settings.SelectedWindowSize;
                    _isMusicEnabled = settings.IsMusicEnabled;
                    _musicVolume = settings.MusicVolume;
                }
                catch { SetDefaultSettings(); }
            }
            else
            {
                SetDefaultSettings();
            }

            OnPropertyChanged(nameof(SelectedWindowSize));
            OnPropertyChanged(nameof(IsMusicEnabled));
            OnPropertyChanged(nameof(MusicVolume));
            
            // Aplicar configuraciones al arrancar
            ApplyWindowSize();
            ApplyMusicSettings();
        }

        private void SetDefaultSettings()
        {
            _selectedWindowSize = "Mediano (1300x700)";
            _isMusicEnabled = true; // Empieza a sonar por defecto
            _musicVolume = 50.0;
        }

        /// <summary>
        /// Guarda el estado actual de la configuración de la ventana y de la música
        /// en el archivo JSON específico del usuario. Se invoca cada vez que cambian propiedades.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var settings = new UserSettings
                {
                    SelectedWindowSize = _selectedWindowSize,
                    IsMusicEnabled = _isMusicEnabled,
                    MusicVolume = _musicVolume
                };
                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(GetSettingsFilePath(), json);
            }
            catch { }
        }

        /// <summary>
        /// Aplica la configuración de música actual al reproductor estático de la App.
        /// Ajusta el volumen y reproduce o pausa la música según corresponda.
        /// </summary>
        private void ApplyMusicSettings()
        {
            if (App.BackgroundMusicPlayer == null) return;

            App.BackgroundMusicPlayer.Volume = MusicVolume / 100.0; // Volume is 0.0 to 1.0

            if (IsMusicEnabled)
            {
                App.BackgroundMusicPlayer.Play();
            }
            else
            {
                App.BackgroundMusicPlayer.Pause();
            }
        }

        private void ApplyWindowSize()
        {
            Window mainWindow = null;
            
            // Search for the MainView window since Application.Current.MainWindow might not be set correctly
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType().Name == "MainView")
                {
                    mainWindow = window;
                    break;
                }
            }

            if (mainWindow == null) return;

            switch (SelectedWindowSize)
            {
                case "Pequeño (1024x768)":
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Width = 1024;
                    mainWindow.Height = 768;
                    CenterWindow(mainWindow);
                    break;
                case "Mediano (1300x700)":
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Width = 1300;
                    mainWindow.Height = 700;
                    CenterWindow(mainWindow);
                    break;
                case "Grande (1920x1080)":
                    mainWindow.WindowState = WindowState.Maximized;
                    break;
            }
        }
        
        private void CenterWindow(Window window)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = window.Width;
            double windowHeight = window.Height;
            window.Left = (screenWidth / 2) - (windowWidth / 2);
            window.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}
