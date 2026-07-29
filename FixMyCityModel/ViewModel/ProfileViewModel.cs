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
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string Name { get; set; }

        // Email is read-only — cannot be changed via profile
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(15, ErrorMessage = "Contact must be at most 15 characters.")]
        public string Contact { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(250)]
        public string AddressLine { get; set; }

        public int? CityId { get; set; }
        public int? WardId { get; set; }

        [StringLength(100)]
        public string Designation { get; set; }

        // Dropdown sources
        public IEnumerable<City> Cities { get; set; }
        public IEnumerable<Ward> Wards { get; set; }
    }
}
