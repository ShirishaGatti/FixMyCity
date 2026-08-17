using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Caching;

namespace FixMyCity.Repository
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly Database db;
        private static readonly MemoryCache _cache = MemoryCache.Default;

        public ComplaintRepository()
        {
            db = DatabaseFactory.CreateDatabase();
        }

        public List<Complaint> GetComplaints(int? consumerId, int? assignedTo, int roleId)
        {
            var list = new List<Complaint>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintGetById");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "AssignedTo", DbType.Int32, assignedTo);
                db.AddInParameter(com, "RoleId", DbType.Int32, roleId);
                DataSet ds = db.ExecuteDataSet(com);

                if (ds != null && ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(MapComplaint(row));
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve complaints.", "Complaint_GetByConsumerId", ex);
            }
            return list;
        }

        public Complaint GetById(int complaintId, int? consumerId = null, int? officerId = null)
        {
            try
            {
                DbCommand com;
                if (officerId.HasValue)
                {
                    com = db.GetStoredProcCommand("FixMyCity.Complaint_GetAssignedById");
                    db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                    db.AddInParameter(com, "OfficerId", DbType.Int32, officerId.Value);
                }
                else
                {
                    com = db.GetStoredProcCommand("FixMyCity.Complaint_GetById");
                    db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                    db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId ?? 0);
                }

                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return MapComplaint(ds.Tables[0].Rows[0]);
                return null;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve complaint.", "Complaint_GetById", ex);
            }
        }
     

        public List<ComplaintCategory> GetCategories()
        {
            var list = new List<ComplaintCategory>();
            try
            {
                DataSet ds = db.ExecuteDataSet(db.GetStoredProcCommand("FixMyCity.Category_GetAll"));
                if (ds != null && ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(new ComplaintCategory { CategoryId = Convert.ToInt32(row["CategoryId"]), CategoryName = Convert.ToString(row["CategoryName"]) });
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve categories.", "Category_GetAll", ex);
            }
            return list;
        }

        public List<ComplaintPriority> GetPriorities()
        {
            var list = new List<ComplaintPriority>();
            try
            {
                DataSet ds = db.ExecuteDataSet(db.GetStoredProcCommand("FixMyCity.Priority_GetAll"));
                if (ds != null && ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(new ComplaintPriority { PriorityId = Convert.ToInt32(row["PriorityId"]), PriorityName = Convert.ToString(row["PriorityName"]) });
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve priorities.", "Priority_GetAll", ex);
            }
            return list;
        }
        public int CreateAttachment(int complaintId, string fileName, string contentType, long fileSizeBytes, int uploadedBy)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintAttachment_Create");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "FileName", DbType.String, fileName);
                db.AddInParameter(com, "ContentType", DbType.String, contentType);
                db.AddInParameter(com, "FileSizeBytes", DbType.Int64, fileSizeBytes);
                db.AddInParameter(com, "UploadedBy", DbType.Int32, uploadedBy);
                db.AddOutParameter(com, "NewAttachmentId", DbType.Int32, 4);
                db.ExecuteNonQuery(com);
                ClearComplaintCache(uploadedBy);
                return Convert.ToInt32(db.GetParameterValue(com, "NewAttachmentId"));
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to save attachment record.", "ComplaintAttachment_Create", ex);
            }
        }

        public List<Attachment> GetAttachmentsByComplaintId(int complaintId, int consumerId)
        {
            var list = new List<Attachment>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintAttachment_GetByComplaintId");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(MapAttachment(row));
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve attachments.", "ComplaintAttachment_GetByComplaintId", ex);
            }
            return list;
        }

        public Attachment GetAttachmentById(int attachmentId, int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintAttachment_GetById");
                db.AddInParameter(com, "AttachmentId", DbType.Int32, attachmentId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return MapAttachment(ds.Tables[0].Rows[0]);
                return null;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve attachment.", "ComplaintAttachment_GetById", ex);
            }
        }

        public void DeleteAttachment(int attachmentId, int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintAttachment_Delete");
                db.AddInParameter(com, "AttachmentId", DbType.Int32, attachmentId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.ExecuteNonQuery(com);
                ClearComplaintCache(consumerId);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to delete attachment.", "ComplaintAttachment_Delete", ex);
            }
        }
        // ComplaintRepository.cs — cache field, same shape as ItemRepository
        public int SaveComplaint(Complaint c, int actorId, int roleId, int? statusId = null, int? assignedTo = null)
{
    try
    {
        DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_Save");

        db.AddInParameter(com, "ComplaintId", DbType.Int32, c.ComplaintId == 0 ? (object)DBNull.Value : c.ComplaintId);
        db.AddInParameter(com, "Title", DbType.String, (object)c.Title ?? DBNull.Value);
        db.AddInParameter(com, "Description", DbType.String, (object)c.Description ?? DBNull.Value);
        db.AddInParameter(com, "CategoryId", DbType.Int32, c.CategoryId == 0 ? (object)DBNull.Value : c.CategoryId);
        db.AddInParameter(com, "PriorityId", DbType.Int32, c.PriorityId == 0 ? (object)DBNull.Value : c.PriorityId);
        db.AddInParameter(com, "AddressLine", DbType.String, (object)c.AddressLine ?? DBNull.Value);
        db.AddInParameter(com, "Landmark", DbType.String, (object)c.Landmark ?? DBNull.Value);
        db.AddInParameter(com, "WardId", DbType.Int32, c.WardId == 0 ? (object)DBNull.Value : c.WardId);
        db.AddInParameter(com, "CityId", DbType.Int32, c.CityId == 0 ? (object)DBNull.Value : c.CityId);
        db.AddInParameter(com, "Status", DbType.Int32, statusId.HasValue ? (object)statusId.Value : DBNull.Value);
        db.AddInParameter(com, "AssignedTo", DbType.Int32, assignedTo.HasValue ? (object)assignedTo.Value : DBNull.Value);
        db.AddInParameter(com, "RaisedBy", DbType.Int32, actorId);
        db.AddInParameter(com, "RoleId", DbType.Int32, roleId);
        db.AddOutParameter(com, "SavedComplaintId", DbType.Int32, 4);

        db.ExecuteNonQuery(com);
        int savedId = Convert.ToInt32(db.GetParameterValue(com, "SavedComplaintId"));
        ClearComplaintCache(actorId);
        return savedId;
    }
    catch (SqlException ex)
    {
        throw new DataAccessException("Failed to save complaint.", "Complaint_Save", ex);
    }
}
     /*   public int SaveComplaint(Complaint c, int roleId, int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_Save");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, c.ComplaintId == 0 ? (object)DBNull.Value : c.ComplaintId);
                db.AddInParameter(com, "Title", DbType.String, c.Title);
                db.AddInParameter(com, "Description", DbType.String, c.Description);
                db.AddInParameter(com, "CategoryId", DbType.Int32, c.CategoryId);
                db.AddInParameter(com, "PriorityId", DbType.Int32, c.PriorityId);
                db.AddInParameter(com, "RaisedBy", DbType.Int32, c.RaisedBy);
                db.AddInParameter(com, "AddressLine", DbType.String, c.AddressLine);
                db.AddInParameter(com, "Landmark", DbType.String, (object)c.Landmark ?? DBNull.Value);
                db.AddInParameter(com, "WardId", DbType.Int32, c.WardId);
                db.AddInParameter(com, "CityId", DbType.Int32, c.CityId);
                db.AddInParameter(com, "RoleId", DbType.Int32, roleId);
                db.AddOutParameter(com, "SavedComplaintId", DbType.Int32, 4);

                db.ExecuteNonQuery(com);
                int savedId = Convert.ToInt32(db.GetParameterValue(com, "SavedComplaintId"));
                ClearComplaintCache(c.RaisedBy);
                return savedId;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to save complaint.", "Complaint_Save", ex);
            }
        }
        public bool UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId, int roleId)
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
                db.AddInParameter(com, "RaisedBy", DbType.Int32, actorId);
                db.AddInParameter(com, "RoleId", DbType.Int32, roleId); // Admin role
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex) { throw new DataAccessException("Failed to update complaint.", "Admin_UpdateComplaint", ex); }
            return true;
        }*/

        public bool ResolveComplaint(int complaintId, int officerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_Resolve");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "OfficerId", DbType.Int32, officerId);
                db.AddOutParameter(com, "RaisedBy", DbType.Int32, 4);
                db.ExecuteNonQuery(com);
                // Bust the citizen's cache (not the officer's — officer lists aren't cached),
                // so their MyComplaints view reflects "Awaiting Confirmation" immediately.
                object raisedByValue = db.GetParameterValue(com, "RaisedBy");
                if (raisedByValue != null && raisedByValue != DBNull.Value)
                    ClearComplaintCache(Convert.ToInt32(raisedByValue));
                return true;
            }
            catch (SqlException ex) when (ex.Number == ComplaintWorkflowSqlErrorCodes.ResolveNotFound ||
                                           ex.Number == ComplaintWorkflowSqlErrorCodes.ResolveInvalidState ||
                                           ex.Number == ComplaintWorkflowSqlErrorCodes.ResolveStatusMissing)
            {
                throw new BusinessException(ex.Message, "COMPLAINT_RESOLVE_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to mark complaint as resolved.", "Complaint_Resolve", ex);
            }
        }

        public bool ConfirmResolution(int complaintId, int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_ConfirmResolution");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.ExecuteNonQuery(com);
                ClearComplaintCache(consumerId);
                return true;
            }
            catch (SqlException ex) when (ex.Number == ComplaintWorkflowSqlErrorCodes.ConfirmNotFound ||
                                           ex.Number == ComplaintWorkflowSqlErrorCodes.ConfirmInvalidState ||
                                           ex.Number == ComplaintWorkflowSqlErrorCodes.ConfirmStatusMissing)
            {
                throw new BusinessException(ex.Message, "COMPLAINT_CONFIRM_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to confirm the resolution.", "Complaint_ConfirmResolution", ex);
            }
        }

        public bool RejectResolution(int complaintId, int consumerId, string reason)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_RejectResolution");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "Reason", DbType.String, string.IsNullOrWhiteSpace(reason) ? (object)DBNull.Value : reason.Trim());
                db.ExecuteNonQuery(com);
                ClearComplaintCache(consumerId);
                return true;
            }
            catch (SqlException ex) when (ex.Number == ComplaintWorkflowSqlErrorCodes.RejectNotFound ||
                                           ex.Number == ComplaintWorkflowSqlErrorCodes.RejectInvalidState ||
                                           ex.Number == ComplaintWorkflowSqlErrorCodes.RejectStatusMissing)
            {
                throw new BusinessException(ex.Message, "COMPLAINT_REJECT_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to reject the resolution.", "Complaint_RejectResolution", ex);
            }
        }

        // Lazy trigger for the 7-day auto-close: called opportunistically before
        // Officer/Citizen/Admin complaint lists load (see ComplaintService /
        // AdminService). Complaint_AutoExpireResolutions is also safe to run
        // standalone (e.g. from a SQL Agent job) if that's wired up later.
        public List<int> ExpireOverdueResolutions()
        {
            var affectedConsumerIds = new List<int>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_AutoExpireResolutions");
                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int raisedBy = Convert.ToInt32(row["RaisedBy"]);
                        affectedConsumerIds.Add(raisedBy);
                        ClearComplaintCache(raisedBy);
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == ComplaintWorkflowSqlErrorCodes.AutoExpireStatusMissing)
            {
                // Statuses not seeded yet — nothing to expire, don't blow up list pages over it.
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to auto-expire overdue resolutions.", "Complaint_AutoExpireResolutions", ex);
            }
            return affectedConsumerIds;
        }

        public List<Complaint> GetAssignedByOfficerId(int officerId)
        {
            var list = new List<Complaint>();
            try
            {

                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_GetAssignedByOfficer");
                db.AddInParameter(com, "OfficerId", DbType.Int32, officerId);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(MapComplaint(row));
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve assigned complaints.", "Complaint_GetAssignedByOfficerId", ex);
            }
            return list;
        }
     /*   public Complaint GetAssignedComplaintById(int complaintId, int officerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_GetAssignedById");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "OfficerId", DbType.Int32, officerId);
                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return MapComplaint(ds.Tables[0].Rows[0]);
                return null;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve assigned complaint.", "Complaint_GetAssignedComplaintById", ex);
            }
        }*/

        public bool DeleteComplaint(int complaintId, int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintDelete");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                int rows = Convert.ToInt32(db.ExecuteScalar(com));
                ClearComplaintCache(consumerId);
                return rows > 0;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to delete complaint.", "ComplaintDelete", ex);
            }
        }

        public ComplaintSearchResult Search(int consumerId, ComplaintListFilterViewModel filter)
        {
            string cacheKey = GenerateSearchCacheKey(consumerId, filter);

            if (_cache.Contains(cacheKey))
                return (ComplaintSearchResult)_cache.Get(cacheKey);
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_Search");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                db.AddInParameter(com, "Title", DbType.String, string.IsNullOrWhiteSpace(filter.Title) ? (object)DBNull.Value : filter.Title);
                db.AddInParameter(com, "CategoryId", DbType.Int32, (object)filter.CategoryId ?? DBNull.Value);
                db.AddInParameter(com, "StatusId", DbType.Int32, (object)filter.StatusId ?? DBNull.Value);
                db.AddInParameter(com, "DateFrom", DbType.Date, (object)filter.DateFrom ?? DBNull.Value);
                db.AddInParameter(com, "DateTo", DbType.Date, (object)filter.DateTo ?? DBNull.Value);
                db.AddInParameter(com, "SortField", DbType.String, filter.SortField);
                db.AddInParameter(com, "SortDirection", DbType.String, filter.SortDirection);
                db.AddInParameter(com, "PageNumber", DbType.Int32, filter.PageNumber);
                db.AddInParameter(com, "PageSize", DbType.Int32, filter.PageSize);

                DataSet ds = db.ExecuteDataSet(com);
                var list = new List<Complaint>();
                int totalCount = 0;
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(MapComplaint(row));
                    if (ds.Tables[0].Rows.Count > 0)
                        totalCount = Convert.ToInt32(ds.Tables[0].Rows[0]["TotalCount"]);
                }

                var result = new ComplaintSearchResult
                {
                    Complaints = list,
                    TotalCount = totalCount
                };

                _cache.Add(cacheKey, result,
                    new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10)
                    });

                return result;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to search complaints.", "Complaint_Search", ex);
            }
        }

        public List<ComplaintStatus> GetStatuses()
        {
            var list = new List<ComplaintStatus>();
            try
            {
                DataSet ds = db.ExecuteDataSet(db.GetStoredProcCommand("FixMyCity.Status_GetAll"));
                if (ds != null && ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(new ComplaintStatus { StatusId = Convert.ToInt32(row["StatusId"]), StatusName = Convert.ToString(row["StatusName"]) });
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve statuses.", "Status_GetAll", ex);
            }
            return list;
        }

        private static string GenerateSearchCacheKey(int consumerId, ComplaintListFilterViewModel f) =>
            $"ComplaintSearch_{consumerId}_{f.Title}_{f.CategoryId}_{f.StatusId}_{f.DateFrom:yyyyMMdd}_{f.DateTo:yyyyMMdd}_{f.SortField}_{f.SortDirection}_{f.PageNumber}_{f.PageSize}";

        // Called after any write (save/delete/attachment upload/delete) for this
        // consumer — a cached search result is now stale the moment their data
        // changes, so sweep every cache entry keyed to them.
        private static void ClearComplaintCache(int consumerId)
        {
            string prefix = $"ComplaintSearch_{consumerId}_";
            var staleKeys = _cache.Where(kv => kv.Key.StartsWith(prefix)).Select(kv => kv.Key).ToList();
            foreach (var key in staleKeys)
                _cache.Remove(key);
        }
        private static Attachment MapAttachment(DataRow row)
        {
            var a = new Attachment
            {
                AttachmentId = Convert.ToInt32(row["AttachmentId"]),
                ComplaintId = Convert.ToInt32(row["ComplaintId"]),
                FileName = Convert.ToString(row["FileName"]),
                ContentType = row["ContentType"] is DBNull ? null : Convert.ToString(row["ContentType"]),
                FileSizeBytes = row["FileSizeBytes"] is DBNull ? 0 : Convert.ToInt64(row["FileSizeBytes"]),
                UploadedBy = Convert.ToInt32(row["UploadedBy"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
            if (row.Table.Columns.Contains("StatusName"))
                a.ComplaintStatusName = Convert.ToString(row["StatusName"]);
            return a;
        }
        private static Complaint MapComplaint(DataRow row)
        {
            return new Complaint
            {
                ComplaintId = Convert.ToInt32(row["ComplaintId"]),
                ComplaintNumber = Convert.ToString(row["ComplaintNumber"]),
                Title = Convert.ToString(row["Title"]),
                Description = Convert.ToString(row["Description"]),
                CategoryId = Convert.ToInt32(row["CategoryId"]),
                CategoryName = Convert.ToString(row["CategoryName"]),
                PriorityId = Convert.ToInt32(row["PriorityId"]),
                PriorityName = Convert.ToString(row["PriorityName"]),
                StatusId = Convert.ToInt32(row["StatusId"]),
                StatusName = Convert.ToString(row["StatusName"]),
                RaisedBy = Convert.ToInt32(row["RaisedBy"]),
                AssignedTo = row["AssignedTo"] is DBNull ? (int?)null : Convert.ToInt32(row["AssignedTo"]),
                // AssignedName = row.Table.Columns.Contains("AssignedName") && row["AssignedName"] != DBNull.Value ? Convert.ToString(row["AssignedName"]) : null,
                AssignedName = row.Table.Columns.Contains("AssignedName") && row["AssignedName"] != DBNull.Value
    ? Convert.ToString(row["AssignedName"]) : null,
                AddressLine = Convert.ToString(row["AddressLine"]),
                Landmark = row["Landmark"] is DBNull ? null : Convert.ToString(row["Landmark"]),
                WardId = Convert.ToInt32(row["WardId"]),
                WardName = Convert.ToString(row["WardName"]),
                CityId = Convert.ToInt32(row["CityId"]),
                CityName = Convert.ToString(row["CityName"]),
                ResolvedDate = row["ResolvedDate"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["ResolvedDate"]),
                ClosedDate = row["ClosedDate"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["ClosedDate"]),
                ReopenCount = Convert.ToInt32(row["ReopenCount"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
        }

    }
}
