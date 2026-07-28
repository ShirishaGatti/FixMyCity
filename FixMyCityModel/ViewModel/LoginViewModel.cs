using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        // Populated by the service on success — not bound from the form.
        public bool Success { get; set; }
        public int ConsumerId { get; set; }
        public int RoleId { get; set; }
        public string Name { get; set; }
    }
}
