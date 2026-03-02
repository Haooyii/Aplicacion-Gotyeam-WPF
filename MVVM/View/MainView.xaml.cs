using Gotyeam.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gotyeam.MVVM.View
{
    /// <summary>
    /// Lógica de interacción para MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            DataContext = new Gotyeam.MVVM.ViewModel.MainViewModel();
            CargarDatosEmpleado();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private void pnlControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMaximizar_Click(object sender, RoutedEventArgs e)
        {
            if(this.WindowState == WindowState.Normal) 
                this.WindowState = WindowState.Maximized;
            else this.WindowState = WindowState.Normal;
        }

        private void btnMinimizar_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Properties["AuthToken"] = null;
            App.Current.Properties["Token"] = null;
            App.Current.Properties["LoggedUserEmail"] = null;
            
            // Pare la música al cerrar sesión
            if (App.BackgroundMusicPlayer != null)
            {
                App.BackgroundMusicPlayer.Stop();
            }

            LoginView loginView = new LoginView();
            loginView.Show();
            this.Close();
        }

        private async void CargarDatosEmpleado()
        {
            var servicio = new GetEmployeeProfile();
            var empleado = await servicio.GetEmployeeAsync();

            if (empleado != null)
            {
                txtNombreCompletoEmpleado.Text = empleado.NombreCompleto;

                if (!string.IsNullOrEmpty(empleado.foto_url))
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();
                        
                        if (empleado.foto_url.StartsWith("data:image"))
                        {
                            var base64Data = empleado.foto_url.Substring(empleado.foto_url.IndexOf(",") + 1);
                            byte[] imageBytes = Convert.FromBase64String(base64Data);
                            using (var ms = new System.IO.MemoryStream(imageBytes))
                            {
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = ms;
                                bitmap.EndInit();
                            }
                        }
                        else
                        {
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(empleado.foto_url, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                        }

                        imgPerfilEmpleado.ImageSource = bitmap;
                    }
                    catch (Exception)
                    {
                        imgPerfilEmpleado.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Imagenes/user.png"));
                    }
                }
                else
                {
                    imgPerfilEmpleado.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Imagenes/user.png"));
                }
            }
        }
    }
}
