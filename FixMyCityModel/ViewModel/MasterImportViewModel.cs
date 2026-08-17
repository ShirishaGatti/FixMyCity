using System;
using System.ComponentModel.DataAnnotations;

namespace FixMyCityModel.ViewModel
{
    public class MasterImportViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Entity type is required.")]
        [StringLength(50)]
        public string EntityType { get; set; }
    }
}