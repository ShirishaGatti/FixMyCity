using FixMyCityModel.Model;
using System;
using System.Collections.Generic;

namespace FixMyCityModel.ViewModel
{
    // Row-shape for the Users / Officers table on the admin screens.
    // Denormalised (city / ward / role / department names) so the front-end
    // never has to do a second lookup per row.
    public class AdminUserRow
    {
        public int ConsumerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public DateTime? DOB { get; set; }

        public int RoleId { get; set; }
        public string RoleName { get; set; }

        public int? CityId { get; set; }
        public string CityName { get; set; }

        public int? WardId { get; set; }
        public string WardName { get; set; }

        public int? DeptId { get; set; }
        public string DepartmentName { get; set; }

        public string Designation { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Filter + sort DTO shared by Users and Officers screens; only relevant
    // fields are populated per screen. Matches the AJAX POST payload that
    // admin-users.js / admin-officers.js sends.
    public class AdminUserListFilterViewModel
    {
        public string Name { get; set; }
        public string Designation { get; set; } // Officers-only
        public int? CityId { get; set; }
        public int? WardId { get; set; }

        // Values: "Name", "ConsumerId", "DOB"
        public string SortBy { get; set; } = "ConsumerId";
        public string SortDir { get; set; } = "DESC";

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // For distinguishing Users list vs Officers list at the SP layer.
        // 0 = all, otherwise RoleId filter.
        public int RoleId { get; set; }
    }

    public class AdminUserListViewModel
    {
        public List<AdminUserRow> Rows { get; set; } = new List<AdminUserRow>();
        public int TotalCount { get; set; }
        public AdminUserListFilterViewModel Filter { get; set; } = new AdminUserListFilterViewModel();

        // Dropdown sources for the filter card + edit modal.
        public List<City> Cities { get; set; } = new List<City>();
        public List<Ward> Wards { get; set; } = new List<Ward>();
        public List<Role> Roles { get; set; } = new List<Role>();
        public List<Department> Departments { get; set; } = new List<Department>();
    }
}
