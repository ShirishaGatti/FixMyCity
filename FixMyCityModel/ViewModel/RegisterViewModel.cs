using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace FixMyCityModel
{
    public class RegisterViewModel
    {
        public RegisterViewModel()
        {
            RoleId = 2; // Default to Citizen
        }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(15)]
        public string Contact { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(250)]
        public string AddressLine { get; set; }

        public int? CityId { get; set; }
        public int? WardId { get; set; }

        public int RoleId { get; set; }

        public int? DepartmentId { get; set; }
        public string Designation { get; set; }
        public IEnumerable<SelectListItem> Cities { get; set; }
        public IEnumerable<SelectListItem> Wards { get; set; }
    }
}
