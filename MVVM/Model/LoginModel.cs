using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gotyeam.MVVM.Model
{
    public class LoginRequestModel
    {
        public string correo { get; set; }
        public string contrasenna { get; set; }
    }

    public class LoginResponseModel
    {
        public string access_token { get; set; }
    }
}
