using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{
    public class VerifyOtpViewModel
    {
        //[Required]
        //[RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit OTP.")]
        public string EnteredOtp { get; set; }
    }
}
