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
    public class ComplaintChatRepository : IComplaintChatRepository
    {
        private readonly Database db;

        public ComplaintChatRepository()
        {
            db = DatabaseFactory.CreateDatabase();
        }

        public ComplaintThreadResult GetThread( int complaintId,int requesterId,int requesterRoleId,int sinceMessageId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintChat_GetByComplaintId");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "RequesterId", DbType.Int32, requesterId);
                db.AddInParameter(com, "RequesterRoleId", DbType.Int32, requesterRoleId);
                db.AddInParameter(com, "SinceMessageId", DbType.Int32, sinceMessageId);

                DataSet ds = db.ExecuteDataSet(com);

                var list = new List<ComplaintChatMessage>();
                bool isChatOpen = false;

                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                        list.Add(MapMessage(row));
                }
                if (ds != null && ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    isChatOpen = Convert.ToBoolean(ds.Tables[1].Rows[0]["IsChatOpen"]);
                }

                ComplaintThreadResult result = new ComplaintThreadResult();
                result.Messages = list;
                result.IsChatOpen = isChatOpen;

                return result;
            }
            catch (SqlException ex) when (ex.Number == 52000)
            {
                // THROW 52000 in the SP = permission denial (not a participant).
                // Translate to the same BusinessException type used elsewhere so the
                // service/controller layers don't need to know about SQL error codes.
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve chat thread.", "ComplaintChat_GetByComplaintId", ex);
            }
        }

        public int InsertTextMessage(int complaintId, int senderId, int senderRoleId, string messageText)
        {
            return Insert(complaintId, senderId, senderRoleId, messageText, null);
        }

        public int InsertAttachmentMessage(int complaintId, int senderId, int senderRoleId, int attachmentId)
        {
            return Insert(complaintId, senderId, senderRoleId, null, attachmentId);
        }

        private int Insert(int complaintId, int senderId, int senderRoleId, string messageText, int? attachmentId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintChat_Insert");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "SenderId", DbType.Int32, senderId);
                db.AddInParameter(com, "SenderRoleId", DbType.Int32, senderRoleId);
                db.AddInParameter(com, "MessageText", DbType.String, (object)messageText ?? DBNull.Value);
                db.AddInParameter(com, "AttachmentId", DbType.Int32, (object)attachmentId ?? DBNull.Value);
                db.AddOutParameter(com, "NewChatMessageId", DbType.Int32, 4);

                db.ExecuteNonQuery(com);
                return Convert.ToInt32(db.GetParameterValue(com, "NewChatMessageId"));
            }
            catch (SqlException ex) when (ex.Number == 52000 || ex.Number == 52001 || ex.Number == 52002)
            {
                // Custom THROW errors from the SP: 52000 permission, 52001 malformed
                // content, 52002 chat closed. All are user-facing business rules, not
                // infrastructure failures, so they map to BusinessException.
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to send chat message.", "ComplaintChat_Insert", ex);
            }
        }

        public int CreateAttachment(int complaintId, string fileName, string contentType, long fileSizeBytes, int uploadedBy)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintChatAttachment_Create");
                db.AddInParameter(com, "ComplaintId", DbType.Int32, complaintId);
                db.AddInParameter(com, "FileName", DbType.String, fileName);
                db.AddInParameter(com, "ContentType", DbType.String, (object)contentType ?? DBNull.Value);
                db.AddInParameter(com, "FileSizeBytes", DbType.Int64, fileSizeBytes);
                db.AddInParameter(com, "UploadedBy", DbType.Int32, uploadedBy);
                db.AddOutParameter(com, "NewAttachmentId", DbType.Int32, 4);

                db.ExecuteNonQuery(com);
                return Convert.ToInt32(db.GetParameterValue(com, "NewAttachmentId"));
            }
            catch (SqlException ex) when (ex.Number == 52002)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to save chat attachment.", "ComplaintChatAttachment_Create", ex);
            }
        }

        public ChatAttachmentRecord GetAttachmentById(int chatAttachmentId, int requesterId, int requesterRoleId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintChatAttachment_GetById");
                db.AddInParameter(com, "ChatAttachmentId", DbType.Int32, chatAttachmentId);
                db.AddInParameter(com, "RequesterId", DbType.Int32, requesterId);
                db.AddInParameter(com, "RequesterRoleId", DbType.Int32, requesterRoleId);

                DataSet ds = db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];
                    return new ChatAttachmentRecord
                    {
                        ChatAttachmentId = Convert.ToInt32(row["ChatAttachmentId"]),
                        ComplaintId = Convert.ToInt32(row["ComplaintId"]),
                        FileName = Convert.ToString(row["FileName"]),
                        ContentType = row["ContentType"] is DBNull ? null : Convert.ToString(row["ContentType"]),
                        FileSizeBytes = Convert.ToInt64(row["FileSizeBytes"]),
                        UploadedBy = Convert.ToInt32(row["UploadedBy"])
                    };
                }
                return null;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve chat attachment.", "ComplaintChatAttachment_GetById", ex);
            }
        }

        private static ComplaintChatMessage MapMessage(DataRow row)
        {
            return new ComplaintChatMessage
            {
                ChatMessageId = Convert.ToInt32(row["ChatMessageId"]),
                ComplaintId = Convert.ToInt32(row["ComplaintId"]),
                SenderId = Convert.ToInt32(row["SenderId"]),
                SenderName = Convert.ToString(row["SenderName"]),
                SenderRoleId = Convert.ToInt32(row["SenderRoleId"]),
                MessageText = row["MessageText"] is DBNull ? null : Convert.ToString(row["MessageText"]),
                AttachmentId = row["AttachmentId"] is DBNull ? (int?)null : Convert.ToInt32(row["AttachmentId"]),
                FileName = row["FileName"] is DBNull ? null : Convert.ToString(row["FileName"]),
                ContentType = row["ContentType"] is DBNull ? null : Convert.ToString(row["ContentType"]),
                FileSizeBytes = row["FileSizeBytes"] is DBNull ? (long?)null : Convert.ToInt64(row["FileSizeBytes"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
        }
    }
}
