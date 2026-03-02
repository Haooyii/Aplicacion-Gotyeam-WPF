using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Gotyeam.MVVM.Model
{
    public class PerfilEmpleado : INotifyPropertyChanged
    {
        public string _id { get; set; } = string.Empty;
        public string id { get; set; }
        public string contrasenna { get; set; } = string.Empty;
        public string nombre { get; set; }
        public string apellido1 { get; set; }
        public string apellido2 { get; set; }
        public string correo { get; set; }
        public string foto_url { get; set; }
        public int nivel_rol { get; set; }
        public bool activo { get; set; }

        private bool _isEditable;
        public bool IsEditable
        {
            get => _isEditable;
            set { _isEditable = value; OnPropertyChanged(); }
        }

        public string NombreCompleto => $"{nombre} {apellido1} {apellido2}".Trim();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
