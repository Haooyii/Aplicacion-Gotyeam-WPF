using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gotyeam.MVVM.Model;

namespace Gotyeam.Services
{
    public class EmployeesService
    {
        //objeto de clase HttpClient para poder hacer y recibir peticiones http
        private readonly HttpClient _httpClient;

        //constructor donde se instancia un objeto de clase HttpClient e indica la URL base
        public EmployeesService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        }
        
        //funcion utilizado para enviar junto a la peticion http la cabecera con el token
        private void SetupBearerToken()
        {
            //al hacer clic en Iniciar Sesion, se guardó el token en las propiedades de la aplicacion y aqui se recupera ese token, se guarda y
            // y convierte a string ya que por defecto, al estar guardado en las propiedades, es de tipo object
            var token = App.Current.Properties["Token"] as string;

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        //metodo que devuelve una lista de empleados
        public async Task<List<PerfilEmpleado>> ListarEmpleadosAsync(string estado = "activo", string search = null)
        {
            //se llama a esta funcion para enviar junto a la peticion http la cabecera con el token de autentificacion
            SetupBearerToken();

            //variable que guarda la URL personalizada
            var url = $"empleado/listar?estado={estado}";

            //comprueba que la variable search no esté nula y agrega a la URL de busqueda esta variable, se usa 
            //Uri.EscapeDataString ya que las URLs no permiten caracteres especiales como espacios,etc
            if (!string.IsNullOrEmpty(search)) url += $"&buscar={Uri.EscapeDataString(search)}";

            try
            {
                //guarda en la variable response la respuesta del servidor, envía por el metodo GET 
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    //guarda el contenido devuelto como string
                    var content = await response.Content.ReadAsStringAsync();
                    //convertir de JSON a lista de objetos de clase PerfilEmpleado
                    return JsonSerializer.Deserialize<List<PerfilEmpleado>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<PerfilEmpleado>();
        }

        //metodo que devuelve bool por facilitar el UX
        public async Task<bool> RegistrarEmpleadoAsync(PerfilEmpleado empleado)
        {
            SetupBearerToken();
            try
            {
                //se hace lo siguiente para faciliar las cosas ya que Dictionary te organiza y prepara el objeto de C# con un formato
                //json, es decir, clave valor, <string> indica que la clave es texto mientras que <object> indica que el valor puede
                //ser diferentes tipos de datos, string, int,etc
                var dict = new Dictionary<string, object>
                {
                    { "nombre", empleado.nombre },
                    { "apellido1", empleado.apellido1 },
                    { "apellido2", empleado.apellido2 ?? "" },
                    { "correo", empleado.correo },
                    { "contrasenna", empleado.contrasenna },
                    { "nivel_rol", empleado.nivel_rol }
                };

                if (!string.IsNullOrEmpty(empleado.foto_url))
                {
                    dict.Add("foto_url", empleado.foto_url);
                }

                //instancia e inicializa un objeto llamado content de clase StringContent donde convierto mi objeto C#
                //en formato JSON, encoding UTF8 para permitir la 'ñ' y 'application/json' para indicarle al servidor que lo que se envia es un JSON
                var content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, "application/json");

                //guarda la respuesta devuelta por la API, await para no congelar la aplicacion en lo que espera la respuesta, PostAsync para indicarle
                //que es una peticion tipo POST y luego le pasa la URL y el JSON
                var response = await _httpClient.PostAsync($"empleado/registrar", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        //metodo que devuelve bool por facilitar el UX
        public async Task<bool> EditarEmpleadoAsync(string idDestino, PerfilEmpleado empleado)
        {
            SetupBearerToken();
            try
            {
                //se hace lo siguiente para faciliar las cosas ya que Dictionary te organiza y prepara el objeto de C# con un formato
                //json, es decir, clave valor, <string> indica que la clave es texto mientras que <object> indica que el valor puede
                //ser diferentes tipos de datos, string, int,etc
                var dict = new Dictionary<string, object>
                {
                    { "nombre", empleado.nombre },
                    { "apellido1", empleado.apellido1 },
                    { "apellido2", empleado.apellido2 ?? "" },
                    { "correo", empleado.correo },
                    { "nivel_rol", empleado.nivel_rol }
                };

                if (!string.IsNullOrEmpty(empleado.foto_url))
                {
                    dict.Add("foto_url", empleado.foto_url);
                }

                if (!string.IsNullOrEmpty(empleado.contrasenna))
                {
                    dict.Add("contrasenna", empleado.contrasenna);
                }

                //instancia e inicializa un objeto llamado content de clase StringContent donde convierto mi objeto C#
                //en formato JSON, encoding UTF8 para permitir la 'ñ' y 'application/json' para indicarle al servidor que lo que se envia es un JSON
                var content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, "application/json");

                //guarda la respuesta devuelta por la API, await para no congelar la aplicacion en lo que espera la respuesta, PatchAsync para indicarle
                //que es una peticion tipo PATCH y luego le pasa la URL y el JSON
                var response = await _httpClient.PatchAsync($"empleado/editar/{idDestino}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        //metodo que devuelve bool por facilitar el UX
        public async Task<bool> CambiarEstadoEmpleadoAsync(string idDestino)
        {
            SetupBearerToken();
            try
            {
                //instancia e inicializa un objeto llamado content de clase StringContent donde convierto mi objeto C#
                //en formato JSON, encoding UTF8 para permitir la 'ñ' y 'application/json' para indicarle al servidor que lo que se envia es un JSON
                //en este caso se envia vació ya que mi endpoint no requiere de un json
                var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");

                //guarda la respuesta devuelta por la API, await para no congelar la aplicacion en lo que espera la respuesta, PatchAsync para indicarle
                //que es una peticion tipo PATCH y luego le pasa la URL y el JSON
                var response = await _httpClient.PatchAsync($"empleado/estado/{idDestino}", emptyContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}
