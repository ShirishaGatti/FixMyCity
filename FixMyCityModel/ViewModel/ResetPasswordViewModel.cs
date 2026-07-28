using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit OTP.")]
        public string EnteredOtp { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
