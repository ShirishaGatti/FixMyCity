using FixMyCity.Exceptions;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace FixMyCity.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly Database db;

        public AdminRepository()
        {
            db = DatabaseFactory.CreateDatabase();
        }

        private static int ToInt(object value) => value is DBNull || value == null ? 0 : Convert.ToInt32(value);

        // ============================================================
        // Dashboard
        // ============================================================
        public AdminDashboardViewModel GetDashboardStats()
        {
            var vm = new AdminDashboardViewModel();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_GetDashboardStats");
                DataSet ds = db.ExecuteDataSet(com);

                if (ds.Tables.Count >= 1 && ds.Tables[0].Rows.Count > 0)
                {
                    var r = ds.Tables[0].Rows[0];
                    vm.TotalUsers = ToInt(r["TotalUsers"]);
                    vm.TotalCitizens = ToInt(r["TotalCitizens"]);
                    vm.TotalOfficers = ToInt(r["TotalOfficers"]);
                    vm.TotalAdmins = ToInt(r["TotalAdmins"]);
                    vm.TotalComplaints = ToInt(r["TotalComplaints"]);
                    vm.OpenComplaints = ToInt(r["OpenComplaints"]);
                    vm.InProgressComplaints = ToInt(r["InProgressComplaints"]);
                    vm.ResolvedComplaints = ToInt(r["ResolvedComplaints"]);
                    vm.ClosedComplaints = ToInt(r["ClosedComplaints"]);
                    vm.TotalCities = ToInt(r["TotalCities"]);
                    vm.TotalWards = ToInt(r["TotalWards"]);
                    vm.TotalDepartments = ToInt(r["TotalDepartments"]);
                    vm.TotalCategories = ToInt(r["TotalCategories"]);
                }
                if (ds.Tables.Count >= 2)
                    foreach (DataRow row in ds.Tables[1].Rows)
                        vm.TopCategories.Add(new CategoryCount { CategoryName = row["CategoryName"].ToString(), Count = ToInt(row["Cnt"]) });

                if (ds.Tables.Count >= 3)
                    foreach (DataRow row in ds.Tables[2].Rows)
                        vm.RecentComplaints.Add(new Complaint
                        {
                            ComplaintId = ToInt(row["ComplaintId"]),
                            ComplaintNumber = row["ComplaintNumber"].ToString(),
                            Title = row["Title"].ToString(),
                            StatusName = row["StatusName"].ToString(),
                            CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                        });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load dashboard stats.", "Admin_GetDashboardStats", ex); }
            return vm;
        }

        // ============================================================
        // Users
        // ============================================================
        public AdminUserListViewModel ListUsers(AdminUserListFilterViewModel filter)
        {
            var vm = new AdminUserListViewModel { Filter = filter };

            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_ListUsers");

                db.AddInParameter(com, "Name", DbType.String,
                    string.IsNullOrWhiteSpace(filter.Name) ? (object)DBNull.Value : filter.Name.Trim());

                db.AddInParameter(com, "Designation", DbType.String,
                    string.IsNullOrWhiteSpace(filter.Designation) ? (object)DBNull.Value : filter.Designation.Trim());

                db.AddInParameter(com, "CityId", DbType.Int32,
                    filter.CityId.HasValue ? (object)filter.CityId.Value : DBNull.Value);

                db.AddInParameter(com, "WardId", DbType.Int32,
                    filter.WardId.HasValue ? (object)filter.WardId.Value : DBNull.Value);

                // FIX: was "filter.RoleId>0?filter.RoleId : 2" — that silently forced every
                // request with no role picked into RoleId=2 (Citizen), so Admins/Officers
                // could never show up unless the user explicitly picked their own role.
                // RoleId is nullable end-to-end now: no selection => no filter => everyone.
                db.AddInParameter(com, "RoleId", DbType.Int32,
                    filter.RoleId.HasValue && filter.RoleId.Value > 0 ? (object)filter.RoleId.Value : DBNull.Value);

                db.AddInParameter(com, "SortBy", DbType.String, filter.SortBy ?? "ConsumerId");
                db.AddInParameter(com, "SortDir", DbType.String, filter.SortDir ?? "DESC");
                db.AddInParameter(com, "PageNumber", DbType.Int32, filter.PageNumber);
                db.AddInParameter(com, "PageSize", DbType.Int32, filter.PageSize);

                DataSet ds = db.ExecuteDataSet(com);

                int total = 0;

                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        vm.Rows.Add(new AdminUserRow
                        {
                            ConsumerId = ToInt(row["ConsumerId"]),
                            Name = row["Name"].ToString(),
                            Email = row["Email"].ToString(),
                            Contact = row["Contact"] is DBNull ? null : row["Contact"].ToString(),
                            DOB = row["DOB"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["DOB"]),
                            RoleId = ToInt(row["RoleId"]),
                            RoleName = row["RoleName"] is DBNull ? null : row["RoleName"].ToString(),
                            CityId = row["CityId"] is DBNull ? (int?)null : ToInt(row["CityId"]),
                            CityName = row["CityName"] is DBNull ? null : row["CityName"].ToString(),
                            WardId = row["WardId"] is DBNull ? (int?)null : ToInt(row["WardId"]),
                            WardName = row["WardName"] is DBNull ? null : row["WardName"].ToString(),
                            DeptId = row["DeptId"] is DBNull ? (int?)null : ToInt(row["DeptId"]),
                            DepartmentName = row["DepartmentName"] is DBNull ? null : row["DepartmentName"].ToString(),
                            Designation = row["Designation"] is DBNull ? null : row["Designation"].ToString(),
                            IsActive = Convert.ToBoolean(row["IsActive"]),
                            CreatedDate = row["CreatedDate"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(row["CreatedDate"])
                        });

                        if (total == 0 && ds.Tables[0].Columns.Contains("TotalCount"))
                            total = ToInt(row["TotalCount"]);
                    }
                }

                vm.TotalCount = total;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to load users list.", "Admin_ListUsers", ex);
            }

            return vm;
        }

        public AdminUserEditViewModel GetUserById(int consumerId)
        {
            AdminUserEditViewModel vm = null;
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_GetUserById");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                DataSet ds = db.ExecuteDataSet(com);

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var r = ds.Tables[0].Rows[0];
                    vm = new AdminUserEditViewModel
                    {
                        ConsumerId = ToInt(r["ConsumerId"]),
                        Name = r["Name"].ToString(),
                        Email = r["Email"].ToString(),
                        Contact = r["Contact"] is DBNull ? null : r["Contact"].ToString(),
                        DOB = r["DOB"] is DBNull ? (DateTime?)null : Convert.ToDateTime(r["DOB"]),
                        RoleId = ToInt(r["RoleId"]),
                        CityId = r["CityId"] is DBNull ? (int?)null : ToInt(r["CityId"]),
                        WardId = r["WardId"] is DBNull ? (int?)null : ToInt(r["WardId"]),
                        DeptId = r["DeptId"] is DBNull ? (int?)null : ToInt(r["DeptId"]),
                        Designation = r["Designation"] is DBNull ? null : r["Designation"].ToString(),
                        IsActive = Convert.ToBoolean(r["IsActive"])
                    };
                }
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load user.", "Admin_GetUserById", ex); }
            return vm;
        }

        public bool UpdateUserRole(int consumerId, int newRoleId, int? deptId, int? wardId, string designation, int actorId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_UpdateUserRole");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "NewRoleId", DbType.Int32, newRoleId);
                db.AddInParameter(com, "DeptId", DbType.Int32, deptId.HasValue ? (object)deptId.Value : DBNull.Value);
                db.AddInParameter(com, "WardId", DbType.Int32, wardId.HasValue ? (object)wardId.Value : DBNull.Value);
                db.AddInParameter(com, "Designation", DbType.String, (object)designation ?? DBNull.Value);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to update user role.", "Admin_UpdateUserRole", ex); }
            return true;
        }

        public bool UpdateUserStatus(int consumerId, bool isActive, int actorId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_UpdateUserStatus");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "IsActive", DbType.Boolean, isActive);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to update user status.", "Admin_UpdateUserStatus", ex); }
            return true;
        }

        public bool DeleteUser(int consumerId, int actorId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_DeleteUser");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to delete user.", "Admin_DeleteUser", ex); }
            return true;
        }

        // ============================================================
        // Complaints
        // ============================================================
        public AdminComplaintListViewModel ListComplaints(AdminComplaintListFilterViewModel filter)
        {
            var vm = new AdminComplaintListViewModel { Filter = filter, Rows = new List<AdminComplaintRow>() };

            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_ListComplaints");
                db.AddInParameter(com, "CategoryId", DbType.Int32, filter.CategoryId.HasValue ? (object)filter.CategoryId.Value : DBNull.Value);
                db.AddInParameter(com, "CityId", DbType.Int32, filter.CityId.HasValue ? (object)filter.CityId.Value : DBNull.Value);
                db.AddInParameter(com, "WardId", DbType.Int32, filter.WardId.HasValue ? (object)filter.WardId.Value : DBNull.Value);
                db.AddInParameter(com, "SortBy", DbType.String, filter.SortBy ?? "ComplaintId");
                db.AddInParameter(com, "SortDir", DbType.String, filter.SortDir ?? "DESC");
                db.AddInParameter(com, "PageNumber", DbType.Int32, filter.PageNumber);
                db.AddInParameter(com, "PageSize", DbType.Int32, filter.PageSize);

                DataSet ds = db.ExecuteDataSet(com);
                int total = 0;

                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        vm.Rows.Add(new AdminComplaintRow
                        {
                            ComplaintId = ToInt(row["ComplaintId"]),
                            ComplaintNumber = row["ComplaintNumber"].ToString(),
                            Title = row["Title"].ToString(),
                            CategoryId = ToInt(row["CategoryId"]),
                            CategoryName = row["CategoryName"] is DBNull ? null : row["CategoryName"].ToString(),
                            PriorityId = ToInt(row["PriorityId"]),
                            PriorityName = row["PriorityName"] is DBNull ? null : row["PriorityName"].ToString(),
                            StatusId = ToInt(row["StatusId"]),
                            StatusName = row["StatusName"] is DBNull ? null : row["StatusName"].ToString(),
                            AssignedTo = row["AssignedTo"] is DBNull ? (int?)null : ToInt(row["AssignedTo"]),
                            CityId = row["CityId"] is DBNull ? 0 : ToInt(row["CityId"]),
                            CityName = row["CityName"] is DBNull ? null : row["CityName"].ToString(),
                            WardId = row["WardId"] is DBNull ? 0 : ToInt(row["WardId"]),
                            WardName = row["WardName"] is DBNull ? null : row["WardName"].ToString(),
                            CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                        });

                        if (total == 0 && ds.Tables[0].Columns.Contains("TotalCount"))
                            total = ToInt(row["TotalCount"]);
                    }
                }

                vm.TotalCount = total;
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load complaints list.", "Admin_ListComplaints", ex); }
            return vm;
        }

        public AdminComplaintEditViewModel GetComplaintById(int complaintId)
        {
            AdminComplaintEditViewModel vm = null;
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_GetComplaintById");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                DataSet ds = db.ExecuteDataSet(com);

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var r = ds.Tables[0].Rows[0];
                    vm = new AdminComplaintEditViewModel
                    {
                        ComplaintId = ToInt(r["ComplaintId"]),
                        Title = r["Title"].ToString(),
                        Description = r["Description"] is DBNull ? null : r["Description"].ToString(),
                        CategoryId = ToInt(r["CategoryId"]),
                        PriorityId = ToInt(r["PriorityId"]),
                        StatusId = ToInt(r["StatusId"]),
                        AssignedTo = r["AssignedTo"] is DBNull ? (int?)null : ToInt(r["AssignedTo"]),
                        CityId = ToInt(r["CityId"]),
                        WardId = ToInt(r["WardId"])
                    };
                }
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load complaint.", "Admin_GetComplaintById", ex); }
            return vm;
        }

        public bool UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId,int roleId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_Save");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "CategoryId", DbType.Int32, categoryId);
                db.AddInParameter(com, "PriorityId", DbType.Int32, priorityId);
                db.AddInParameter(com, "Status", DbType.Int32, statusId);
                db.AddInParameter(com, "AssignedTo", DbType.Int32, assignedTo.HasValue ? (object)assignedTo.Value : DBNull.Value);
                //db.AddInParameter(com, "RaisedBy", DbType.Int32, actorId);
                db.AddInParameter(com, "RoleId", DbType.Int32, roleId); // Admin role
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to update complaint.", "Admin_UpdateComplaint", ex); }
            return true;
        }

        public bool DeleteComplaint(int complaintId, int actorId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintDelete");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to delete complaint.", "Admin_DeleteComplaint", ex); }
            return true;
        }

        // ============================================================
        // Master lookups (unchanged from before)
        // ============================================================
        public List<State> GetStates()
        {
            var list = new List<State>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.State_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new State { StateId = ToInt(r["Id"]), StateName = r["Name"].ToString(), IsActive = Convert.ToBoolean(r["IsActive"]) });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load states.", "State_GetAll", ex); }
            return list;
        }

        public List<District> GetDistricts(int? stateId)
        {
            var list = new List<District>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.District_GetAll");
                db.AddInParameter(com, "ParentId", DbType.Int32, stateId.HasValue ? (object)stateId.Value : DBNull.Value);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new District
                        {
                            DistrictId = ToInt(r["Id"]),
                            DistrictName = r["Name"].ToString(),
                            StateId = ToInt(r["ParentId"]),
                            StateName = r.Table.Columns.Contains("ParentName") && !(r["ParentName"] is DBNull) ? r["ParentName"].ToString() : null,
                            IsActive = Convert.ToBoolean(r["IsActive"])
                        });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load districts.", "District_GetAll", ex); }
            return list;
        }

        public List<City> GetCitiesFull()
        {
            var list = new List<City>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.City_GetByDistrict");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new City { CityId = ToInt(r["Id"]), CityName = r["Name"].ToString() });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load cities.", "City_GetAll", ex); }
            return list;
        }

        public List<Ward> GetWardsFull()
        {
            var list = new List<Ward>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Ward_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new Ward { WardId = ToInt(r["Id"]), WardName = r["Name"].ToString(), WardNo = ToInt(r["WardNo"]), CityId = ToInt(r["ParentId"]) });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load wards.", "Ward_GetAll", ex); }
            return list;
        }

        public List<ComplaintCategory> GetCategories()
        {
            var list = new List<ComplaintCategory>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.GetCategory");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new ComplaintCategory
                        {
                            CategoryId = ToInt(r["Id"]),
                            CategoryName = r["Name"].ToString(),
                            DepartmentName = r["DepartmentName"] is DBNull ? null : r["DepartmentName"].ToString(),
                            DepartmentId = ToInt(r["DepartmentId"])
                        });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load categories.", "Category_GetAll", ex); }
            return list;
        }

        public List<Department> GetDepartments()
        {
            var list = new List<Department>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Department_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new Department { DepartmentId = ToInt(r["Id"]), DepartmentName = r["Name"].ToString(), IsActive = Convert.ToBoolean(r["IsActive"]) });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load departments.", "Department_GetAll", ex); }
            return list;
        }

        public List<ComplaintPriority> GetPriorities()
        {
            var list = new List<ComplaintPriority>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Priority_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new ComplaintPriority { PriorityId = ToInt(r["PriorityId"]), PriorityName = r["PriorityName"].ToString() });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load priorities.", "Priority_GetAll", ex); }
            return list;
        }

        public List<ComplaintStatus> GetStatuses()
        {
            var list = new List<ComplaintStatus>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Status_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new ComplaintStatus { StatusId = ToInt(r["StatusId"]), StatusName = r["StatusName"].ToString() });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load statuses.", "Status_GetAll", ex); }
            return list;
        }

        public List<Role> GetRoles()
        {
            var list = new List<Role>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Role_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new Role { RoleId = ToInt(r["Id"]), RoleName = r["Name"].ToString(), IsActive = Convert.ToBoolean(r["IsActive"]) });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load roles.", "Role_GetAll", ex); }
            return list;
        }

        // ============================================================
        // Generic master list/save (unchanged)
        // ============================================================
        private static readonly Dictionary<string, string> MasterListProcs = new Dictionary<string, string>
        {
            { "state",      "FixMyCity.State_GetAll" },
            { "district",   "FixMyCity.District_GetAll" },
            { "city",       "FixMyCity.City_GetByDistrict" },
            { "ward",       "FixMyCity.Ward_GetAll" },
            { "category",   "FixMyCity.GetCategory" },
            { "department", "FixMyCity.Department_GetAll" },
            { "role",       "FixMyCity.Role_GetAll" },
        };

        private static readonly HashSet<string> EntitiesWithParent = new HashSet<string> { "district", "city", "ward" };

        public List<MasterEntityViewModel> GetMasterList(string entityType, int? parentId, bool includeInactive)
        {
            string sproc;
            if (!MasterListProcs.TryGetValue(entityType, out sproc))
                throw new BusinessException("Unknown entity type.", "INVALID_ENTITY");

            var list = new List<MasterEntityViewModel>();
            try
            {
                DbCommand com = db.GetStoredProcCommand(sproc);
                if (EntitiesWithParent.Contains(entityType))
                    db.AddInParameter(com, "ParentId", DbType.Int32, parentId.HasValue ? (object)parentId.Value : DBNull.Value);
                db.AddInParameter(com, "IncludeInactive", DbType.Boolean, includeInactive);

                using (IDataReader reader = db.ExecuteReader(com))
                {
                    while (reader.Read())
                    {
                        list.Add(new MasterEntityViewModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            ParentId = reader["ParentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ParentId"]),
                            ParentName = reader["ParentName"] == DBNull.Value ? null : reader["ParentName"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            WardNo = HasColumn(reader, "WardNo") && reader["WardNo"] != DBNull.Value ? reader["WardNo"].ToString() : null
                        });
                    }
                }
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load master data.", sproc, ex); }
            return list;
        }

        public int SaveState(int id, string name, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.State_Save", id, name, null, isActive, actorId);

        public int SaveDistrict(int id, string name, int stateId, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.District_Save", id, name, stateId, isActive, actorId);

        public int SaveCity(int id, string name, int? districtId, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.City_Save", id, name, districtId, isActive, actorId);

        public int SaveWard(int id, string name, int cityId, bool isActive, string wardNo, int actorId)
        {
            int newId = id;
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Ward_Save");
                db.AddInParameter(com, "Id", DbType.Int32, id);
                db.AddInParameter(com, "Name", DbType.String, name);
                db.AddInParameter(com, "ParentId", DbType.Int32, cityId);
                db.AddInParameter(com, "IsActive", DbType.Boolean, isActive);
                db.AddInParameter(com, "WardNo", DbType.String, string.IsNullOrWhiteSpace(wardNo) ? DBNull.Value : (object)wardNo);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.AddOutParameter(com, "NewId", DbType.Int32, 4);
                db.ExecuteNonQuery(com);
                newId = Convert.ToInt32(db.GetParameterValue(com, "NewId"));
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000 || ex.Number == 2627 || ex.Number == 2601)
                    throw new DataAccessException(ex.Message, "Ward_Save", ex);
                throw new DataAccessException("Failed to save ward.", "Ward_Save", ex);
            }
            return newId;
        }

        public int SaveCategory(int id, string name, bool isActive, int actorId, int departmentId)
        {
            int newId = id;
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Category_Save");
                db.AddInParameter(com, "Id", DbType.Int32, id);
                db.AddInParameter(com, "Name", DbType.String, name);
                db.AddInParameter(com, "IsActive", DbType.Boolean, isActive);
                db.AddInParameter(com, "DepartmentId", DbType.Int32, departmentId);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.AddOutParameter(com, "NewId", DbType.Int32, 4);
                db.ExecuteNonQuery(com);
                newId = Convert.ToInt32(db.GetParameterValue(com, "NewId"));
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000 || ex.Number == 2627 || ex.Number == 2601)
                    throw new DataAccessException(ex.Message, "Category_Save", ex);
                throw new DataAccessException("Failed to save category.", "Category_Save", ex);
            }
            return newId;
        }

        public int SaveDepartment(int id, string name, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.Department_Save", id, name, null, isActive, actorId);

        public int SaveRole(int id, string name, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.Role_Save", id, name, null, isActive, actorId);

        private int SaveSimpleMaster(string sproc, int id, string name, int? parentId, bool isActive, int actorId)
        {
            int newId = id;
            try
            {
                DbCommand com = db.GetStoredProcCommand(sproc);
                db.AddInParameter(com, "Id", DbType.Int32, id);
                db.AddInParameter(com, "Name", DbType.String, name);
                db.AddInParameter(com, "ParentId", DbType.Int32, parentId.HasValue ? (object)parentId.Value : DBNull.Value);
                db.AddInParameter(com, "IsActive", DbType.Boolean, isActive);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.AddOutParameter(com, "NewId", DbType.Int32, 4);
                db.ExecuteNonQuery(com);
                newId = Convert.ToInt32(db.GetParameterValue(com, "NewId"));
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    throw new DataAccessException("A record with that name already exists.", sproc, ex);
                throw new DataAccessException("Failed to save master data.", sproc, ex);
            }
            return newId;
        }
        public void UpdateOfficer(
    int consumerId,
    string designation,
    int? wardId,
    int? deptId,
    int actorId)
        {
            using (DbCommand cmd = db.GetStoredProcCommand("Admin_UpdateOfficer"))
            {
                db.AddInParameter(cmd, "@ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(cmd, "@Designation", DbType.String, designation);
                db.AddInParameter(cmd, "@WardId", DbType.Int32, wardId);
                db.AddInParameter(cmd, "@DeptId", DbType.Int32, deptId);
                db.AddInParameter(cmd, "@ActorId", DbType.Int32, actorId);

                db.ExecuteNonQuery(cmd);
            }
        }
        private static bool HasColumn(IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}