using System.ComponentModel.DataAnnotations;

namespace FixMyCityModel.ViewModel
{
    // Separate from RegisterViewModel on purpose: the public form must never
    // be able to set RoleId/DeptId/Designation (AuthService.Register() forces
    // RoleId = Citizen regardless of input). This VM is only ever bound on
    // an action gated by [RoleAuthorize(RoleIds.Admin)], so exposing RoleId
    // here is safe — the caller is already known to be an Admin.
    public class StaffRegisterViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 3)]
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
        [StringLength(15)]
        public string Contact { get; set; }

        // Only SupportExecutive or Admin — enforced again in AuthService.RegisterStaff,
        // never trust the dropdown alone.
        [Required]
        [Range(2, 3, ErrorMessage = "Role must be Support Executive or Admin.")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Department is required for staff accounts.")]
        public int DeptId { get; set; }

        [StringLength(100)]
        public string Designation { get; set; }
    }
}
