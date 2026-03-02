using System;
using System.Collections.Generic;

namespace Gotyeam.MVVM.Model
{
    public class UserModel
    {
        public string _id { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string apellido1 { get; set; } = string.Empty;
        public string apellido2 { get; set; } = string.Empty;
        public string correo { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string foto_url { get; set; } = string.Empty;
        public string fecha_registro { get; set; } = string.Empty;
        public bool activo { get; set; } = true;
        
        public string contrasenna { get; set; } = string.Empty;
        
        // Tops ids can be added if needed, but not strictly necessary for the user list
        // public List<string> favoritos { get; set; } = new List<string>();

        public string NombreCompleto => $"{nombre} {apellido1} {apellido2}".Trim();
    }
}
