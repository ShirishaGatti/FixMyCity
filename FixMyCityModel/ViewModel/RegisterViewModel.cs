using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using FixMyCityModel.Model;

namespace FixMyCityModel.ViewModel
{
    public class RegisterViewModel
    {
        public RegisterViewModel()
        {
            RoleId = 2; // Default to Citizen
        }

        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z\s]{3,100}$", ErrorMessage = "Name must contain only letters and be at least 3 characters long.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^(?:\+91|91)?[6789]\d{9}$", ErrorMessage = "Enter a valid 10-digit contact number.")]
        public string Contact { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(250)]
        public string AddressLine { get; set; }

        public int? CityId { get; set; }
        public int? WardId { get; set; }
        public int RoleId { get; set; }
        public int? DepartmentId { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^(?!\d+$).*$", ErrorMessage = "Designation cannot be only numbers.")]
        public string Designation { get; set; }

        // SelectList — not IEnumerable<City/Ward> — because:
        //   a) @Html.DropDownListFor needs a SelectList, not a raw entity list.
        //   b) A user selects exactly ONE city and ONE ward, so storing a
        //      collection of entities here is the wrong abstraction for a form VM.
        //   c) SelectList already carries the selected-value marker, so re-populating
        //      on validation failure pre-selects the user's previous choice.
        public IEnumerable<City> Cities { get; set; }
        public IEnumerable<Ward> Wards { get; set; }

        // Same reasoning as ProfileViewModel.ValidateDob — a runtime "not in
        // the future" rule doesn't fit a fixed DataAnnotations pattern.
        public bool ValidateDob(out string errorMessage)
        {
            errorMessage = null;
            if (DOB.HasValue && DOB.Value.Date > DateTime.Today)
            {
                errorMessage = "Date of birth cannot be a future date.";
                return false;
            }
            return true;
        }
    }
}