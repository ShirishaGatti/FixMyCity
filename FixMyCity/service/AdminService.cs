using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;

namespace FixMyCity.service
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repo;

        public AdminService() : this(new AdminRepository()) { }
        public AdminService(IAdminRepository repo) { _repo = repo; }

        public AdminDashboardViewModel GetDashboard() => _repo.GetDashboardStats();

        // ============================================================
        // Users
        // ============================================================
        private static readonly HashSet<string> AllowedUserSort = new HashSet<string> { "ConsumerId", "Name", "DOB" };

        public AdminUserListViewModel ListUsers(AdminUserListFilterViewModel filter)
        {
            filter = filter ?? new AdminUserListFilterViewModel();

            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;

            if (!AllowedUserSort.Contains(filter.SortBy ?? string.Empty))
                filter.SortBy = "ConsumerId";

            filter.SortDir = string.Equals(filter.SortDir, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

            var vm = _repo.ListUsers(filter);

            vm.Cities = _repo.GetCitiesFull();
            vm.Wards = _repo.GetWardsFull();
            vm.Roles = _repo.GetRoles();
            vm.Departments = _repo.GetDepartments();

            return vm;
        }

        public AdminUserEditViewModel GetUserById(int consumerId)
        {
            if (consumerId <= 0) throw new BusinessException("Invalid user.", "INVALID_USER");
            var vm = _repo.GetUserById(consumerId);
            if (vm == null) throw new BusinessException("User not found.", "NOT_FOUND");

            vm.Cities = _repo.GetCitiesFull();
            vm.Wards = _repo.GetWardsFull();
            vm.Roles = _repo.GetRoles();
            vm.Departments = _repo.GetDepartments();
            return vm;
        }

        public void UpdateUser(int consumerId, int newRoleId, int? deptId, int actorId)
        {
            if (consumerId <= 0) throw new BusinessException("Invalid user.", "INVALID_USER");
            if (newRoleId <= 0) throw new BusinessException("Invalid role.", "INVALID_ROLE");

            if (newRoleId == RoleIds.SupportExecutive && (!deptId.HasValue || deptId.Value <= 0))
                throw new BusinessException("Assigning Officer role requires a Department.", "DEPT_REQUIRED");

            _repo.UpdateUser(consumerId, newRoleId, deptId, actorId);
        }

        public void UpdateUserStatus(int consumerId, bool isActive, int actorId)
        {
            if (consumerId <= 0) throw new BusinessException("Invalid user.", "INVALID_USER");
            _repo.UpdateUserStatus(consumerId, isActive, actorId);
        }

        public void DeleteUser(int consumerId, int actorId)
        {
            if (consumerId <= 0) throw new BusinessException("Invalid user.", "INVALID_USER");
            _repo.DeleteUser(consumerId, actorId);
        }

        // ============================================================
        // Complaints
        // ============================================================
        private static readonly HashSet<string> AllowedComplaintSort =
            new HashSet<string> { "ComplaintId", "CreatedAt", "CategoryName", "StatusName", "PriorityName" };

        public AdminComplaintListViewModel ListComplaints(AdminComplaintListFilterViewModel filter)
        {
            filter = filter ?? new AdminComplaintListFilterViewModel();
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;

            if (!AllowedComplaintSort.Contains(filter.SortBy ?? string.Empty))
                filter.SortBy = "ComplaintId";

            filter.SortDir = string.Equals(filter.SortDir, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

            var vm = _repo.ListComplaints(filter);
            vm.Filter = filter;
            vm.Categories = _repo.GetCategories();
            vm.Priorities = _repo.GetPriorities();
            vm.Statuses = _repo.GetStatuses();
            vm.Cities = _repo.GetCitiesFull();
            vm.Wards = _repo.GetWardsFull();
            return vm;
        }

        public AdminComplaintEditViewModel GetComplaintById(int complaintId)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            var vm = _repo.GetComplaintById(complaintId);
            if (vm == null) throw new BusinessException("Complaint not found.", "NOT_FOUND");

            vm.Categories = _repo.GetCategories();
            vm.Priorities = _repo.GetPriorities();
            vm.Statuses = _repo.GetStatuses();
            vm.Officers = _repo.ListUsers(new AdminUserListFilterViewModel { RoleId = 3, PageSize = 1000 }).Rows;
            return vm;
        }

        public void UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId, int roleId)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            if (categoryId <= 0 || priorityId <= 0 || statusId <= 0)
                throw new BusinessException("Category, priority and status are required.", "INVALID_INPUT");
            _repo.UpdateComplaint(complaintId, categoryId, priorityId, statusId, assignedTo, actorId, roleId);
        }

        public void DeleteComplaint(int complaintId, int actorId)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            _repo.DeleteComplaint(complaintId, actorId);
        }

        // ============================================================
        // Master data
        // ============================================================
        public List<MasterEntityViewModel> GetMasterList(string entityType, int? parentId, bool includeInactive)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new BusinessException("Entity type is required.", "ENTITY_REQUIRED");
            return _repo.GetMasterList(entityType.Trim().ToLowerInvariant(), parentId, includeInactive);
        }

        public int SaveMaster(MasterEntitySaveViewModel vm, int actorId)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.Name))
                throw new BusinessException("Name is required.", "NAME_REQUIRED");
            if (vm.Name.Trim().Length < 2)
                throw new BusinessException("Name must be at least 2 characters.", "NAME_TOO_SHORT");

            string name = vm.Name.Trim();
            switch ((vm.EntityType ?? "").ToLowerInvariant())
            {
                case "state":
                    return _repo.SaveState(vm.Id, name, vm.IsActive, actorId);
                case "district":
                    if (!vm.ParentId.HasValue || vm.ParentId.Value <= 0)
                        throw new BusinessException("Please select a State.", "PARENT_REQUIRED");
                    return _repo.SaveDistrict(vm.Id, name, vm.ParentId.Value, vm.IsActive, actorId);
                case "city":
                    return _repo.SaveCity(vm.Id, name, vm.ParentId, vm.IsActive, actorId);
                case "ward":
                    if (!vm.ParentId.HasValue || vm.ParentId.Value <= 0)
                        throw new BusinessException("Please select a City.", "PARENT_REQUIRED");
                    if (string.IsNullOrWhiteSpace(vm.WardNo))
                        throw new BusinessException("Ward Number is required.", "WARD_NO_REQUIRED");
                    return _repo.SaveWard(vm.Id, name, vm.ParentId.Value, vm.IsActive, vm.WardNo.Trim(), actorId);
                case "category":
                    if (vm.DepartmentId == null || vm.DepartmentId <= 0)
                        throw new BusinessException("Department is required.", "DepartmentId_REQUIRED");
                    return _repo.SaveCategory(vm.Id, name, vm.IsActive, actorId, vm.DepartmentId);
                case "department":
                    return _repo.SaveDepartment(vm.Id, name, vm.IsActive, actorId);
                case "role":
                    return _repo.SaveRole(vm.Id, name, vm.IsActive, actorId);
                default:
                    throw new BusinessException("Unknown entity type.", "INVALID_ENTITY");
            }
        }

        public List<District> GetDistricts(int? stateId) => _repo.GetDistricts(stateId);

        public List<Ward> GetWardsByCity(int cityId)
        {
            var all = _repo.GetWardsFull();
            return all.FindAll(w => w.CityId == cityId);
        }
       /* public void UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId, int actorId)
        {
            if (consumerId <= 0) throw new BusinessException("Invalid officer.", "INVALID_USER");
            if (!deptId.HasValue || deptId.Value <= 0)
                throw new BusinessException("Department is required.", "DEPT_REQUIRED");
            _repo.UpdateOfficer(consumerId, designation, wardId, deptId, actorId);
        }  */
       public MasterDataViewModel GetMasterData()
   {
       return new MasterDataViewModel
       {
           States = _repo.GetStates(),
           Districts = _repo.GetDistricts(null),
           Cities = _repo.GetCitiesFull(),
           Wards = _repo.GetWardsFull(),
           Categories = _repo.GetCategories(),
           Departments = _repo.GetDepartments()
       };
   }
       /* public AdminUserListViewModel GetOfficers(AdminUserListFilterViewModel filter)
        {
            filter = filter ?? new AdminUserListFilterViewModel();

            // Officer Role Id
            filter.RoleId = 3;   // Replace with your actual Officer RoleId

            return _adminRepository.GetUsers(filter);
        }

        public void UpdateOfficer(
            int consumerId,
            string designation,
            int? wardId,
            int? deptId,
            int actorId)
        {
            if (consumerId <= 0)
                throw new BusinessException("Invalid officer.");

            _adminRepository.UpdateOfficer(
                consumerId,
                designation,
                wardId,
                deptId,
                actorId);
        }*/

        public List<Department> GetDepartments() => _repo.GetDepartments();
        public List<Role> GetRoles() => _repo.GetRoles();
    }
}