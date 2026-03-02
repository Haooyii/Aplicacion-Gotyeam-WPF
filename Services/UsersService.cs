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
    public class UsersService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:5000";

        public UsersService()
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

        public async Task<List<UserModel>> ListarUsuariosAsync(string estado = "activo", string search = null)
        {
            SetupBearerToken();
            var url = $"{_baseUrl}/usuario/listar?estado={estado}";
            if (!string.IsNullOrEmpty(search)) url += $"&buscar={Uri.EscapeDataString(search)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<UserModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<UserModel>();
        }

        public async Task<bool> CambiarEstadoUsuarioAsync(string id)
        {
            SetupBearerToken();
            try
            {
                // Un parche vacío ya que cambia el estado al contrario del que esté
                var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{_baseUrl}/usuario/estado/{id}", emptyContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<(bool success, string message)> EditarUsuarioAsync(string id, UserModel user)
        {
            SetupBearerToken();
            try
            {
                var payloadDict = new Dictionary<string, object>
                {
                    { "nombre", user.nombre },
                    { "apellido1", user.apellido1 },
                    { "apellido2", user.apellido2 },
                    { "username", user.username },
                    { "correo", user.correo },
                    { "foto_url", user.foto_url }
                };
                
                if (!string.IsNullOrWhiteSpace(user.contrasenna))
                {
                    payloadDict["contrasenna"] = user.contrasenna;
                }
                
                var payload = payloadDict;
                var json = JsonSerializer.Serialize(payload);
                var body = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_baseUrl}/usuario/editar/{id}", body);
                if (response.IsSuccessStatusCode) return (true, "Usuario actualizado exitosamente.");
                
                var errorContent = await response.Content.ReadAsStringAsync();
                try 
                {
                    var errorDoc = JsonDocument.Parse(errorContent);
                    if (errorDoc.RootElement.TryGetProperty("error", out var errorElement)) return (false, errorElement.GetString());
                    if (errorDoc.RootElement.TryGetProperty("msg", out var msgElement)) return (false, msgElement.GetString());
                } catch { }

                return (false, $"Error del servidor: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> RegistrarUsuarioAsync(UserModel user)
        {
            SetupBearerToken();
            try
            {
                var dict = new Dictionary<string, object>
                {
                    { "nombre", user.nombre ?? "" },
                    { "apellido1", user.apellido1 ?? "" },
                    { "apellido2", user.apellido2 ?? "" },
                    { "username", user.username ?? "" },
                    { "correo", user.correo ?? "" },
                    { "contrasenna", user.contrasenna ?? "" },
                    { "foto_url", user.foto_url ?? "https://via.placeholder.com/150" }
                };

                var content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/usuario/registrar", content);
                
                if (response.IsSuccessStatusCode) return (true, "Usuario registrado exitosamente.");

                var errorContent = await response.Content.ReadAsStringAsync();
                try 
                {
                    var errorDoc = JsonDocument.Parse(errorContent);
                    if (errorDoc.RootElement.TryGetProperty("error", out var errorElement)) return (false, errorElement.GetString());
                    if (errorDoc.RootElement.TryGetProperty("msg", out var msgElement)) return (false, msgElement.GetString());
                } catch { }

                return (false, $"Error del servidor: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        public async Task<List<ResennaModel>> ListarResennasDeUsuarioAsync(string idUsuario)
        {
            SetupBearerToken();
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/resenna/listar/mis_resennas/{idUsuario}");
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
                        Console.WriteLine($"Error parsing JSON reviews in user service: {jsonEx.Message}");
                        Console.WriteLine($"Raw JSON content was: {content}");
                        return new List<ResennaModel>();
                    }
                }
                else
                {
                    Console.WriteLine($"Error HTTP fetch user reviews: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<ResennaModel>();
        }

        public async Task<bool> EliminarResennaAsync(string idResenna)
        {
            SetupBearerToken();
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/resenna/eliminar/{idResenna}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
