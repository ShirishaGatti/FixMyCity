using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FixMyCityModel.Model;

namespace FixMyCityModel.ViewModel
{
    public class ProfileViewModel
    {
        public int ConsumerId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[a-zA-Z\s]{3,100}$", ErrorMessage = "Name must contain only letters and be at least 3 characters long.")]
        public string Name { get; set; }

        // Email is read-only — cannot be changed via profile
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^(?:\+91|91)?[6789]\d{9}$", ErrorMessage = "Enter a valid 10-digit contact number.")]
        public string Contact { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(250)]
        public string AddressLine { get; set; }

        public int? CityId { get; set; }
        public int? WardId { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^(?!\d+$).*$", ErrorMessage = "Designation cannot be only numbers.")]
        public string Designation { get; set; }

        // Dropdown sources
        public IEnumerable<City> Cities { get; set; }
        public IEnumerable<Ward> Wards { get; set; }

        // DOB has a runtime rule (relative to DateTime.Today), not a fixed
        // pattern, so it doesn't fit a DataAnnotations attribute cleanly —
        // same reasoning as ValidateDates() on ComplaintListFilterViewModel.
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