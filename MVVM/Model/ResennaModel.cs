using System;

namespace Gotyeam.MVVM.Model
{
    public class ResennaModel
    {
        public string _id { get; set; } = string.Empty;
        public string id_juego { get; set; } = string.Empty;
        public string id_usuario { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string foto_url { get; set; } = string.Empty;
        public string titulo { get; set; } = string.Empty;
        public string portada_url { get; set; } = string.Empty;
        public bool activo { get; set; } = true;
        
        public string comentario { get; set; } = string.Empty;
        public int puntuacion { get; set; } = 0;
        public bool recomendado { get; set; } = false;
        
        public string fecha_registro { get; set; } = string.Empty;
        public string fecha_modificacion { get; set; } = string.Empty;

        // Propiedad para mapear el usuario que escribió la reseña (si el backend lo devuelve así)
        public UsuarioResenna usuario { get; set; }
    }

    // Clase auxiliar para mapear los datos del usuario dentro de la reseña
    public class UsuarioResenna
    {
        public string _id { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string apellido1 { get; set; } = string.Empty;
        public string apellido2 { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string foto_url { get; set; } = string.Empty;
        
        // Propiedad calculada
        public string NombreCompleto => $"{nombre} {apellido1} {apellido2}".Trim();
    }

    public class ResennaListResponse
    {
        public System.Collections.Generic.List<ResennaModel> resennas { get; set; }
    }
}
