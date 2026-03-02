using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace Gotyeam.MVVM.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //metodo para leer el token de autentificacion y sacar el nivel de rol
        public int NivelRol
        {
            get
            {
                //comprueba que hay un token guardado
                if (Application.Current != null && Application.Current.Properties.Contains("Token"))
                {
                    //lo guarda como un string
                    string token = Application.Current.Properties["Token"] as string;
                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            //el token está dividido en 3 partes, la primera es la cabecera y especifica el algoritmo,
                            //la segunda es la carga util, nuestro identity y additional claims de la api y la tercera es 
                            //la firma, para verificar que el token no está manipulado
                            var parts = token.Split('.');
                            if (parts.Length > 1)
                            {
                                var payload = parts[1];
                                //necesita convertir de formato Base64url(utilizado por el token) a Base64 Estandar(utilizado por C#)
                                //entonces para convertirlo, Base64 Estandar necesita que la longitud sea multiplo de 4,sin embargo, el formato utilizado
                                //por el token suele eliminar el signo '=' de la url ya que da errores, por lo que la longitud no suele
                                //ser multiplo de 4, entonces se comprueba el resto y en base a eso se añade '=' para que sea multiplo de 4
                                switch (payload.Length % 4)
                                {
                                    case 2: payload += "=="; break;
                                    case 3: payload += "="; break;
                                }

                                //convierte de Base64 Estandar(tmb llamado Base64String) a binario, tmb reemplaza los signos - y _ por sus respectivos + y /, esto
                                //ultimo es debido a que al utilizar los + y / de Base64String en una URL, se convierten en un espacio y como signo separador de carpetas
                                //por lo que en nuestra api Base64url se reemplaza por - y _, aqui los estamos volviendo a convertir de Base64url a Base64String
                                var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                                //convierte de binario a string con formato json
                                var json = Encoding.UTF8.GetString(jsonBytes);

                                //convierte el JSON en un objeto temporal(temporal porque se usa using y se elimina el objeto al terminar de leerlo) de clase 
                                //JsonDocument para que se pueda navegar por él, ya que es mas facil, seguro y eficiente que guardarlo como un objeto permanente,
                                //tmb tenemos la ventana de poder usar propiedades para buscar cosas especificas
                                using (var doc = JsonDocument.Parse(json))
                                {
                                    //doc.RootElement le comunica que empiece a buscar desde el principio del json, si encuentra, lo guarda en una variable temporal llamada
                                    //nivelRolElement
                                    if (doc.RootElement.TryGetProperty("nivel_rol", out var nivelRolElement))
                                    {
                                        //comprueba que el valor de la clave nivel_rol sea un int, si es asi retorna el valor
                                        if (nivelRolElement.TryGetInt32(out int rol))
                                        {
                                            return rol;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore parsing errors and default to 0
                        }
                    }
                }
                return 0; // Default role if token is missing or parsing fails
            }
        }

        //metodo para sacar el id del empleado
        public string IdEmpleado
        {
            get
            {
                if (Application.Current != null && Application.Current.Properties.Contains("Token"))
                {
                    string token = Application.Current.Properties["Token"] as string;
                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            var parts = token.Split('.');
                            if (parts.Length > 1)
                            {
                                var payload = parts[1];
                                switch (payload.Length % 4)
                                {
                                    case 2: payload += "=="; break;
                                    case 3: payload += "="; break;
                                }
                                var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                                var json = Encoding.UTF8.GetString(jsonBytes);
                                using (var doc = JsonDocument.Parse(json))
                                {
                                    if (doc.RootElement.TryGetProperty("sub", out var subElement))
                                    {
                                        return subElement.GetString();
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                return string.Empty;
            }
        }
    }
}
