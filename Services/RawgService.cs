using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Gotyeam.MVVM.Model;

namespace Gotyeam.Services
{
    public class RawgService
    {
        private readonly HttpClient _httpClient;
        // Reemplaza con tu clave de API de RAWG
        private readonly string _apiKey = "d3bc5f1c2d024671b87b1ac201110600";
        private readonly string _baseUrl = "https://api.rawg.io/api";

        public RawgService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<RawgGame>> BuscarJuegosAsync(string query)
        {
            try
            {
                var url = $"{_baseUrl}/games?key={_apiKey}&search={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RawgGameResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.results ?? new List<RawgGame>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<RawgGame>();
        }

        public async Task<RawgGameDetail> ObtenerDetallesJuegoAsync(int id)
        {
            try
            {
                var url = $"{_baseUrl}/games/{id}?key={_apiKey}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<RawgGameDetail>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
    }
}
