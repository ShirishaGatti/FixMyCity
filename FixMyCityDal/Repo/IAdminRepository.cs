using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System.Collections.Generic;

namespace FixMyCity.Repository
{
    public interface IAdminRepository
    {
        // Dashboard
        AdminDashboardViewModel GetDashboardStats();

        // Users / Officers
        AdminUserListViewModel ListUsers(AdminUserListFilterViewModel filter, out int totalCount);
        bool UpdateUserRole(int consumerId, int newRoleId, int? deptId, int? wardId, string designation, int actorId);
        bool UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId, int actorId);
        bool DeleteUser(int consumerId);

        // Complaints
        List<AdminComplaintRow> ListComplaints(AdminComplaintListFilterViewModel filter, out int totalCount);
        bool UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId);
        bool DeleteComplaint(int complaintId);

        // Master data
        List<State> GetStates();
        List<District> GetDistricts(int? stateId);
        List<City> GetCitiesFull();
        List<Ward> GetWardsFull();
        List<ComplaintCategory> GetCategories();
        List<Department> GetDepartments();
        List<ComplaintPriority> GetPriorities();
        List<ComplaintStatus> GetStatuses();
        List<Role> GetRoles();

        int SaveState(int id, string name, bool isActive, int actorId);
        int SaveDistrict(int id, string name, int stateId, bool isActive, int actorId);
        int SaveCity(int id, string name, int? districtId, bool isActive, int actorId);
        int SaveWard(int id, string name, int cityId, bool isActive, int actorId);
        int SaveCategory(int id, string name, bool isActive, int actorId);
        int SaveDepartment(int id, string name, bool isActive, int actorId);
    }
}
