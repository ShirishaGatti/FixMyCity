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
        AdminUserListViewModel ListUsers(AdminUserListFilterViewModel filter);
        bool UpdateUser(int consumerId, int newRoleId, int? deptId, int actorId);
        //bool UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId, int actorId);
        bool DeleteUser(int consumerId, int actorId);

        // Complaints
        AdminComplaintListViewModel ListComplaints(AdminComplaintListFilterViewModel filter);
        bool UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId,int roleId);
        bool DeleteComplaint(int complaintId, int actorId);

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
        List<MasterEntityViewModel> GetMasterList(string entityType, int? parentId, bool includeInactive);
        int SaveRole(int id, string name, bool isActive, int actorId);
        int SaveState(int id, string name, bool isActive, int actorId);
        int SaveDistrict(int id, string name, int stateId, bool isActive, int actorId);
        int SaveCity(int id, string name, int? districtId, bool isActive, int actorId);
        int SaveWard(int id, string name, int cityId, bool isActive, string wardNo, int actorId);
        int SaveCategory(int id, string name, bool isActive, int actorId,int DepartmentId);
        int SaveDepartment(int id, string name, bool isActive, int actorId);
        AdminUserEditViewModel GetUserById(int consumerId);

        bool UpdateUserStatus(int consumerId, bool isActive, int actorId);

        AdminComplaintEditViewModel GetComplaintById(int complaintId);

    }
}
