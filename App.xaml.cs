using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace Gotyeam
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Reproductor de música de fondo global de la aplicación.
        /// Se maneja como estático para que la música se mantenga sonando
        /// ininterrumpidamente al cambiar entre distintas vistas o ventanas.
        /// </summary>
        public static MediaPlayer BackgroundMusicPlayer { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            BackgroundMusicPlayer = new MediaPlayer();
            
            // Cuando la música termina de reproducirse, se vuelve a poner
            // la posición a cero y se reproduce de nuevo para crear un bucle infinito.
            BackgroundMusicPlayer.MediaEnded += (sender, args) =>
            {
                BackgroundMusicPlayer.Position = TimeSpan.Zero;
                BackgroundMusicPlayer.Play();
            };
        }
    }

}
