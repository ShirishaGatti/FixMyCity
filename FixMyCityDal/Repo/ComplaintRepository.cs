using FixMyCity.Exceptions;
using FixMyCityModel.Model;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace FixMyCity.Repository
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly Database db;

        public ComplaintRepository()
        {
            db = DatabaseFactory.CreateDatabase();
        }

        public List<Complaint> GetByConsumerId(int consumerId)
        {
            var list = new List<Complaint>();
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_GetByConsumerId");
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
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

        public Complaint GetById(int complaintId, int consumerId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_GetById");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
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

        public int Create(Complaint c)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.Complaint_Create");
                db.AddInParameter(com, "Title", DbType.String, c.Title);
                db.AddInParameter(com, "Description", DbType.String, c.Description);
                db.AddInParameter(com, "CategoryId", DbType.Int32, c.CategoryId);
                db.AddInParameter(com, "PriorityId", DbType.Int32, c.PriorityId);
                db.AddInParameter(com, "RaisedBy", DbType.Int32, c.RaisedBy);
                db.AddInParameter(com, "AddressLine", DbType.String, c.AddressLine);
                db.AddInParameter(com, "Landmark", DbType.String, (object)c.Landmark ?? DBNull.Value);
                db.AddInParameter(com, "WardId", DbType.Int32, c.WardId);
                db.AddInParameter(com, "CityId", DbType.Int32, c.CityId);
                db.AddOutParameter(com, "NewComplaintId", DbType.Int32, 4);
                db.ExecuteNonQuery(com);
                return Convert.ToInt32(db.GetParameterValue(com, "NewComplaintId"));
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to file complaint.", "Complaint_Create", ex);
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
                AssigneeName = row["AssigneeName"] is DBNull ? null : Convert.ToString(row["AssigneeName"]),
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