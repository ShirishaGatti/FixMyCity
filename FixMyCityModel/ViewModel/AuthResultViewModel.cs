using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{
    public class AuthResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }
    }
}
