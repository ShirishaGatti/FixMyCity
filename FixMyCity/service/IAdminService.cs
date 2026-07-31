using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System.Collections.Generic;

namespace FixMyCity.Service
{
    public interface IAdminService
    {
        AdminDashboardViewModel GetDashboard();

        AdminUserListViewModel ListUsers(AdminUserListFilterViewModel filter);
        void UpdateUserRole(int consumerId, int newRoleId, int? deptId, int? wardId, string designation, int actorId);
        void UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId, int actorId);
        void DeleteUser(int consumerId);

        AdminComplaintListViewModel ListComplaints(AdminComplaintListFilterViewModel filter);
        void UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId);
        void DeleteComplaint(int complaintId);

        MasterDataViewModel GetMasterData();
        int SaveMaster(MasterEntitySaveViewModel vm, int actorId);
        List<MasterEntityViewModel> GetMasterList(string entityType, int? parentId, bool includeInactive);
        // Handy lookup accessors used by controllers/JSON endpoints.
        List<District> GetDistricts(int? stateId);
        List<Ward> GetWardsByCity(int cityId);
        List<Department> GetDepartments();
        List<Role> GetRoles();
    }
}
