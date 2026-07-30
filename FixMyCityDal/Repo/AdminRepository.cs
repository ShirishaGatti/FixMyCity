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
    // Data-access class for all Admin screens.
    // Same Enterprise Library Data Application Block pattern used across
    // the rest of the codebase (see AuthRepository / ComplaintRepository) —
    // GetStoredProcCommand + AddInParameter, ExecuteDataSet / ExecuteNonQuery,
    // SqlException → DataAccessException translation. Nothing here decides
    // "is this allowed" — that's AdminService's job.
    public class AdminRepository : IAdminRepository
    {
        private readonly Database db;

        public AdminRepository()
        {
            db = DatabaseFactory.CreateDatabase();
        }

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
        // Users / Officers list
        // ============================================================
        public AdminUserListViewModel ListUsers(AdminUserListFilterViewModel filter, out int totalCount)
        {
            totalCount = 0;
            var vm = new AdminUserListViewModel { Filter = filter };
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_ListUsers");
                db.AddInParameter(com, "Name", DbType.String, (object)filter.Name ?? DBNull.Value);
                db.AddInParameter(com, "Designation", DbType.String, (object)filter.Designation ?? DBNull.Value);
                db.AddInParameter(com, "CityId", DbType.Int32, filter.CityId.HasValue ? (object)filter.CityId.Value : DBNull.Value);
                db.AddInParameter(com, "WardId", DbType.Int32, filter.WardId.HasValue ? (object)filter.WardId.Value : DBNull.Value);
                db.AddInParameter(com, "RoleId", DbType.Int32, filter.RoleId);
                db.AddInParameter(com, "SortBy", DbType.String, filter.SortBy ?? "ConsumerId");
                db.AddInParameter(com, "SortDir", DbType.String, filter.SortDir ?? "DESC");
                db.AddInParameter(com, "PageNumber", DbType.Int32, filter.PageNumber);
                db.AddInParameter(com, "PageSize", DbType.Int32, filter.PageSize);
                db.AddOutParameter(com, "TotalCount", DbType.Int32, 4);

                DataSet ds = db.ExecuteDataSet(com);
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
                            RoleName = row["RoleName"].ToString(),
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
                    }
                }
                totalCount = Convert.ToInt32(db.GetParameterValue(com, "TotalCount"));
                vm.TotalCount = totalCount;
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load users list.", "Admin_ListUsers", ex); }
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

        public bool UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId, int actorId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_UpdateOfficer");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "Designation", DbType.String, (object)designation ?? DBNull.Value);
                db.AddInParameter(com, "WardId", DbType.Int32, wardId.HasValue ? (object)wardId.Value : DBNull.Value);
                db.AddInParameter(com, "DeptId", DbType.Int32, deptId.HasValue ? (object)deptId.Value : DBNull.Value);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to update officer.", "Admin_UpdateOfficer", ex); }
            return true;
        }

        public bool DeleteUser(int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_DeleteUser");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to delete user.", "Admin_DeleteUser", ex); }
            return true;
        }

        // ============================================================
        // Complaints
        // ============================================================
        public List<AdminComplaintRow> ListComplaints(AdminComplaintListFilterViewModel filter, out int totalCount)
        {
            totalCount = 0;
            var rows = new List<AdminComplaintRow>();
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
                db.AddOutParameter(com, "TotalCount", DbType.Int32, 4);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        rows.Add(new AdminComplaintRow
                        {
                            ComplaintId = ToInt(row["ComplaintId"]),
                            ComplaintNumber = row["ComplaintNumber"].ToString(),
                            Title = row["Title"].ToString(),
                            CategoryId = ToInt(row["CategoryId"]),
                            CategoryName = row["CategoryName"].ToString(),
                            PriorityId = ToInt(row["PriorityId"]),
                            PriorityName = row["PriorityName"].ToString(),
                            StatusId = ToInt(row["StatusId"]),
                            StatusName = row["StatusName"].ToString(),
                            RaisedByName = row["RaisedByName"] is DBNull ? null : row["RaisedByName"].ToString(),
                            AssigneeName = row["AssigneeName"] is DBNull ? null : row["AssigneeName"].ToString(),
                            AssignedTo = row["AssignedTo"] is DBNull ? (int?)null : ToInt(row["AssignedTo"]),
                            CityId = ToInt(row["CityId"]),
                            CityName = row["CityName"] is DBNull ? null : row["CityName"].ToString(),
                            WardId = ToInt(row["WardId"]),
                            WardName = row["WardName"] is DBNull ? null : row["WardName"].ToString(),
                            CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                        });
                    }
                }
                totalCount = Convert.ToInt32(db.GetParameterValue(com, "TotalCount"));
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load complaints list.", "Admin_ListComplaints", ex); }
            return rows;
        }

        public bool UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_UpdateComplaint");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "CategoryId", DbType.Int32, categoryId);
                db.AddInParameter(com, "PriorityId", DbType.Int32, priorityId);
                db.AddInParameter(com, "StatusId", DbType.Int32, statusId);
                db.AddInParameter(com, "AssignedTo", DbType.Int32, assignedTo.HasValue ? (object)assignedTo.Value : DBNull.Value);
                db.AddInParameter(com, "ActorId", DbType.Int32, actorId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to update complaint.", "Admin_UpdateComplaint", ex); }
            return true;
        }

        public bool DeleteComplaint(int complaintId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Admin_DeleteComplaint");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to delete complaint.", "Admin_DeleteComplaint", ex); }
            return true;
        }

        // ============================================================
        // Master lookups
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
                        list.Add(new State
                        {
                            StateId = ToInt(r["StateId"]),
                            StateName = r["StateName"].ToString(),
                            IsActive = Convert.ToBoolean(r["IsActive"])
                        });
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
                db.AddInParameter(com, "StateId", DbType.Int32, stateId.HasValue ? (object)stateId.Value : DBNull.Value);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new District
                        {
                            DistrictId = ToInt(r["DistrictId"]),
                            DistrictName = r["DistrictName"].ToString(),
                            StateId = ToInt(r["StateId"]),
                            StateName = r.Table.Columns.Contains("StateName") && !(r["StateName"] is DBNull) ? r["StateName"].ToString() : null,
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
                DbCommand com = db.GetStoredProcCommand("FixMyCity.City_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new City { CityId = ToInt(r["CityId"]), CityName = r["CityName"].ToString() });
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
                        list.Add(new Ward
                        {
                            WardId = ToInt(r["WardId"]),
                            WardName = r["WardName"].ToString(),
                            CityId = ToInt(r["CityId"])
                        });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load wards.", "Ward_GetAll", ex); }
            return list;
        }

        public List<ComplaintCategory> GetCategories()
        {
            var list = new List<ComplaintCategory>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Category_GetAll");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0)
                    foreach (DataRow r in ds.Tables[0].Rows)
                        list.Add(new ComplaintCategory { CategoryId = ToInt(r["CategoryId"]), CategoryName = r["CategoryName"].ToString() });
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
                        list.Add(new Department
                        {
                            DepartmentId = ToInt(r["DepartmentId"]),
                            DepartmentName = r["DepartmentName"].ToString(),
                            IsActive = Convert.ToBoolean(r["IsActive"])
                        });
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
                        list.Add(new Role
                        {
                            RoleId = ToInt(r["RoleId"]),
                            RoleName = r["RoleName"].ToString(),
                            IsActive = Convert.ToBoolean(r["IsActive"])
                        });
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to load roles.", "Role_GetAll", ex); }
            return list;
        }

        // ============================================================
        // Master saves — one SP per entity keeps the schema-specific
        // details (columns / audit columns / uniqueness) inside SQL.
        // ============================================================
        public int SaveState(int id, string name, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.State_Save", id, name, null, isActive, actorId);

        public int SaveDistrict(int id, string name, int stateId, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.District_Save", id, name, stateId, isActive, actorId);

        public int SaveCity(int id, string name, int? districtId, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.City_Save", id, name, districtId, isActive, actorId);

        public int SaveWard(int id, string name, int cityId, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.Ward_Save", id, name, cityId, isActive, actorId);

        public int SaveCategory(int id, string name, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.Category_Save", id, name, null, isActive, actorId);

        public int SaveDepartment(int id, string name, bool isActive, int actorId) =>
            SaveSimpleMaster("FixMyCity.Department_Save", id, name, null, isActive, actorId);

        // Common shape for all master saves: Id (0=insert / >0=update),
        // Name, optional ParentId (StateId/DistrictId/CityId), IsActive,
        // ActorId. Each SP internally maps ParentId to the correct FK
        // column, so this repo doesn't have to know the schema per entity.
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

        private static int ToInt(object o) => o is DBNull ? 0 : Convert.ToInt32(o);
    }
}
