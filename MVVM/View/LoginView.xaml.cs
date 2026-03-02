using Gotyeam.MVVM.ViewModel;
using Gotyeam.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gotyeam.MVVM.View
{
    /// <summary>
    /// Lógica de interacción para LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove(); 
            }
        }

        private void btnMinimizar_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var authService = new AuthService();

            string token = await authService.LoginAsync(txtCorreo.Text, txtPass.Password);
            
            if (token != null)
            {
                Application.Current.Properties["Token"] = token;
                Application.Current.Properties["LoggedUserEmail"] = txtCorreo.Text;
                
                MainView mainWindow = new MainView();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Credenciales incorrectas o usuario desactivado");
            }
        }
    }
}
