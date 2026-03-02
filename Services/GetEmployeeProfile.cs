using Gotyeam.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http; // Asegúrate de incluir este using
using System.Net.Http.Headers; // Asegúrate de incluir este using
using System.Text.Json; // Asegúrate de incluir este using
using System.Windows; // Agrega este using para Application.Current

namespace Gotyeam.Services
{
    public class GetEmployeeProfile
    {
        public async Task<PerfilEmpleado> GetEmployeeAsync()
        {
            string token = Application.Current.Properties["Token"] as string;
            using (var cliente = new HttpClient())
            {
                cliente.BaseAddress = new Uri("http://localhost:5000/");
                cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await cliente.GetAsync("empleado/mis_detalles");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // IMPORTANTE: Añadir opciones para que no importe si es Nombre o nombre
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<PerfilEmpleado>(content, options);
                }
                return null; // En lugar de lanzar excepción, devolvemos null para evitar el cierre de la app
            }
        }
    }
}
