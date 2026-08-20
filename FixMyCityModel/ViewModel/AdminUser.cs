using FixMyCityModel.Model;
using System;
using System.Collections.Generic;

namespace FixMyCityModel.ViewModel
{
    public abstract class AdminLookupViewModel
    {
        public List<City> Cities { get; set; } = new List<City>();
        public List<Ward> Wards { get; set; } = new List<Ward>();
        public List<Role> Roles { get; set; } = new List<Role>();
        public List<Department> Departments { get; set; } = new List<Department>();
    }
    public class AdminUserListFilterViewModel
    {
        public string Name { get; set; }
        public string Designation { get; set; }
        public int? CityId { get; set; }
        public int? WardId { get; set; }

        // Was a non-nullable int before, which silently broke filtering:
        // AddInParameter always sent 0 instead of DBNull, so the SP's
        // "@RoleId IS NULL OR c.RoleId = @RoleId" effectively became
        // "c.RoleId = 0" and matched nothing. Must be nullable.
        public int? RoleId { get; set; }

        public string SortBy { get; set; } = "ConsumerId";
        public string SortDir { get; set; } = "DESC";

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

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

    public class AdminUserListViewModel: AdminLookupViewModel
    {
        public List<AdminUserRow> Rows { get; set; } = new List<AdminUserRow>();
        public int TotalCount { get; set; }
        public AdminUserListFilterViewModel Filter { get; set; } = new AdminUserListFilterViewModel();

        // Dropdown sources for the filter card + edit modal.
        public int TotalPages => Filter.PageSize > 0
            ? (int)Math.Ceiling(TotalCount / (double)Filter.PageSize)
            : 0;
    }
    public class AdminComplaintEditViewModel : AdminLookupViewModel
    {
        public int ComplaintId { get; set; }

        public string ComplaintNumber { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int CategoryId { get; set; }

        public int PriorityId { get; set; }

        public string PriorityName { get; set; }

        public int StatusId { get; set; }

        public int? AssignedTo { get; set; }

        // Citizen who raised the complaint — used to notify them by email.
        public int RaisedByConsumerId { get; set; }
        public string RaisedByName { get; set; }
        public string RaisedByEmail { get; set; }

        public int CityId { get; set; }

        public int WardId { get; set; }

        public List<ComplaintCategory> Categories { get; set; }
            = new List<ComplaintCategory>();

        public List<ComplaintPriority> Priorities { get; set; }
            = new List<ComplaintPriority>();

        public List<ComplaintStatus> Statuses { get; set; }
            = new List<ComplaintStatus>();

        public List<AdminUserRow> Officers { get; set; }
            = new List<AdminUserRow>();
    }
    public class AdminUserEditViewModel : AdminLookupViewModel
    {
        public int ConsumerId { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }

        public DateTime? DOB { get; set; }

        public int RoleId { get; set; }

        public int? CityId { get; set; }
        public int? WardId { get; set; }
        public int? DeptId { get; set; }

        public string Designation { get; set; }

        public bool IsActive { get; set; }
    }
}
