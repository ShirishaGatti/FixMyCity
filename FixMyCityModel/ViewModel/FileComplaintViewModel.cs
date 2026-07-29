using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace FixMyCityModel.ViewModel
{
    public class FileComplaintViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, MinimumLength = 10)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a priority.")]
        public int PriorityId { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250)]
        public string AddressLine { get; set; }

        [StringLength(150)]
        public string Landmark { get; set; }

        [Required(ErrorMessage = "Please select a city.")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "Please select a ward.")]
        public int WardId { get; set; }
    }
}
