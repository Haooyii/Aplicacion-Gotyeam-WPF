using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Gotyeam.MVVM.Model
{
    public class JuegoModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        [JsonPropertyName("_id")]
        public string id 
        { 
            get => _id; 
            set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(id_db)); } 
        }

        private int? _apiRawg_id;
        public int? apiRawg_id 
        { 
            get => _apiRawg_id; 
            set { _apiRawg_id = value; OnPropertyChanged(); } 
        }

        private string _titulo = string.Empty;
        public string titulo 
        { 
            get => _titulo; 
            set { _titulo = value; OnPropertyChanged(); } 
        }

        private string _descripcion = string.Empty;
        public string descripcion 
        { 
            get => _descripcion; 
            set { _descripcion = value; OnPropertyChanged(); } 
        }

        private List<string> _generos = new List<string>();
        public List<string> generos 
        { 
            get => _generos; 
            set { _generos = value; OnPropertyChanged(); } 
        }

        private string _generos_string = string.Empty;
        public string generos_string 
        { 
            get => _generos_string; 
            set { _generos_string = value; OnPropertyChanged(); } 
        }

        private string _fecha_lanzamiento = string.Empty;
        public string fecha_lanzamiento 
        { 
            get => _fecha_lanzamiento; 
            set { _fecha_lanzamiento = value; OnPropertyChanged(); } 
        }

        private string _portada_url = string.Empty;
        public string portada_url 
        { 
            get => _portada_url; 
            set { _portada_url = value; OnPropertyChanged(); } 
        }

        private string _desarrolladora = string.Empty;
        public string desarrolladora 
        { 
            get => _desarrolladora; 
            set { _desarrolladora = value; OnPropertyChanged(); } 
        }

        private bool _activo = true;
        public bool activo 
        { 
            get => _activo; 
            set { _activo = value; OnPropertyChanged(); } 
        }
        
        private int _cantidad_resennas = 0;
        [JsonPropertyName("resennas_totales")]
        public int cantidad_resennas 
        { 
            get => _cantidad_resennas; 
            set { _cantidad_resennas = value; OnPropertyChanged(); } 
        }
        
        private double _valoracion_media = 0.0;
        [JsonPropertyName("valoracion_promedia")]
        public double valoracion_media 
        { 
            get => _valoracion_media; 
            set { _valoracion_media = value; OnPropertyChanged(); } 
        }

        public string id_db => id;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
