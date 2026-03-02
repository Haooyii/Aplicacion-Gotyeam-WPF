using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gotyeam.MVVM.Model;

namespace Gotyeam.Services
{
    public class GamesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:5000";

        public GamesService()
        {
            _httpClient = new HttpClient();
        }

        private void SetupBearerToken()
        {
            var token = App.Current.Properties["AuthToken"] as string;
            if (string.IsNullOrEmpty(token)) token = App.Current.Properties["Token"] as string;
            
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<JuegoModel>> ListarJuegosAsync(string estado = "activo", string search = null, string genero = null)
        {
            SetupBearerToken();
            
            var url = $"{_baseUrl}/juego/listar?estado={estado}";
            if (!string.IsNullOrEmpty(search)) url += $"&search={search}";
            if (!string.IsNullOrEmpty(genero)) url += $"&genero={genero}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                    };
                    return JsonSerializer.Deserialize<List<JuegoModel>>(content, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<JuegoModel>();
        }

        public async Task<JuegoModel> ObtenerDetalleJuegoAsync(string id)
        {
            SetupBearerToken();
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/juego/detalle/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                    };
                    return JsonSerializer.Deserialize<JuegoModel>(content, options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<(bool, string)> AnnadirJuegoAsync(JuegoModel juego)
        {
            SetupBearerToken();
            try
            {
                // Solo enviar los datos permitidos por la base de datos
                var payload = new
                {
                    apiRawg_id = juego.apiRawg_id,
                    titulo = juego.titulo,
                    descripcion = juego.descripcion,
                    portada_url = juego.portada_url,
                    fecha_lanzamiento = juego.fecha_lanzamiento,
                    generos = juego.generos.Take(3).ToList(),
                    activo = juego.activo
                };

                var json = JsonSerializer.Serialize(payload);
                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/juego/annadir", body);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    return (false, errorMsg);
                }
                
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<bool> EditarJuegoAsync(string id, JuegoModel juego)
        {
            SetupBearerToken();
            try
            {
                var payload = new
                {
                    titulo = juego.titulo,
                    portada_url = juego.portada_url,
                    descripcion = juego.descripcion,
                    fecha_lanzamiento = juego.fecha_lanzamiento,
                    generos = juego.generos.Take(3).ToList()
                };

                var json = JsonSerializer.Serialize(payload);
                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_baseUrl}/juego/editar/{id}", body);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> EditarEstadoJuegoAsync(string id)
        {
            SetupBearerToken();
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{_baseUrl}/juego/estado/{id}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<ResennaModel>> ListarResennasDeJuegoAsync(string idJuego)
        {
            SetupBearerToken();
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/resenna/listar/{idJuego}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var doc = JsonDocument.Parse(content);
                        var options = new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        };

                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<List<ResennaModel>>(content, options) ?? new List<ResennaModel>();
                        }
                        else if (doc.RootElement.TryGetProperty("resennas", out var resennasElement))
                        {
                            return JsonSerializer.Deserialize<List<ResennaModel>>(resennasElement.GetRawText(), options) ?? new List<ResennaModel>();
                        }
                        return new List<ResennaModel>();
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"Error parsing JSON reviews in game service: {jsonEx.Message}");
                        Console.WriteLine($"Raw JSON content was: {content}");
                        return new List<ResennaModel>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<ResennaModel>();
        }
    }
}
