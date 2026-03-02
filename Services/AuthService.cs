using Gotyeam.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gotyeam.Services
{
    public class AuthService
    {
        //clase que se utiliza para enviar y recibir solicitudes http
        private readonly HttpClient _http;
        //clase para convertir objetos de C# en json y json en C#, se usa PropertyNameCaseInsensitive para que no
        //de error si no coinciden las mayusculas y minusculas de las claves del json con los nombres de los atributos de la clase C#
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //constructor
        public AuthService()    
        {
            //instancio el objeto y con la propiedad BaseAddress le digo que cada vez que se envía algo, la direccion empieza de la siguiente manera
            _http = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        }

        //metodo asincrono que devuelve que string
        public async Task<string> LoginAsync(string correo, string contrasenna)
        {
            //instancia e inicializa un objeto llamado data de clase LoginRequestModel con correo y contraseña
            var data = new LoginRequestModel { correo = correo, contrasenna = contrasenna };

            //instancia e inicializa un objeto llamado content de clase StringContent donde convierto mi objeto C# llamado data
            //en formato JSON, encoding UTF8 para permitir la 'ñ' y 'application/json' para indicarle al servidor que lo que se envia es un JSON
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            //guarda la respuesta devuelta por la API, await para no congelar la aplicacion en lo que espera la respuesta, PostAsync para indicarle
            //que es una peticion tipo POST y luego le pasa la URL y el JSON
            var response = await _http.PostAsync("empleado/login", content);

            if (response.IsSuccessStatusCode)
            {
                //guarda en json la respuesta devuelta por el servidor, ReadAsStringAsinc se usa para devolver la respuesta como un string, ya que por
                //defecto los datos viajan por Internet como una serie de bytes
                var json = await response.Content.ReadAsStringAsync();

                //convierte el JSON en un objeto C# de clase LoginResponseModel, se usa _options para ignorar si no coinciden mayusculas y minusculas
                var result = JsonSerializer.Deserialize<LoginResponseModel>(json, _options);

                //devuelve el atributo del objeto result de clase LoginResponseModel
                return result.access_token;
            }
            return null;
        }
    }
}
