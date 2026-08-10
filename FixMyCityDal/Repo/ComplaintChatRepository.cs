/*using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
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
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied ||
                ex.Number == ChatSqlErrorCodes.MalformedContent ||
                ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
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
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied ||
                ex.Number == ChatSqlErrorCodes.MalformedContent ||
                ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to save chat attachment.", "ComplaintChatAttachment_Create", ex);
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
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied ||
                ex.Number == ChatSqlErrorCodes.MalformedContent ||
                ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.ChatClosed)
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
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied ||
                ex.Number == ChatSqlErrorCodes.MalformedContent ||
                ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
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
        public void DeactivateAttachment(int attachmentId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintChatAttachment_Deactivate");
                db.AddInParameter(com, "ChatAttachmentId", DbType.Int32, attachmentId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                // Best-effort cleanup — don't let a cleanup failure mask the original exception below.
                throw new DataAccessException("ComplaintChatAttachment_Deactivate", "ComplaintChatAttachment_Deactivate", ex);


            }
        }
    }
}
*/
using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
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

        public ComplaintThreadResult GetThread(int complaintId, int requesterId, int requesterRoleId, int sinceMessageId)
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
            // GetThread is read-only, so the only SP-raised error code that can
            // realistically apply is PermissionDenied (52000) — MalformedContent
            // and ChatClosed only fire from the write-side Insert SP. Still,
            // this endpoint is hit by the 5s/20s poll loop from every open chat
            // panel, so any *other* SqlException (deadlock, timeout, connection
            // drop) needs a fallback too, or a single transient DB hiccup
            // becomes an unhandled 500 on every open tab's next poll tick.
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to load chat thread.", "ComplaintChat_GetByComplaintId", ex);
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
            // These three SP error codes (52000/52001/52002) are all
            // business-rule rejections the caller should see as a friendly
            // message, not a 500 — so they're translated to BusinessException.
            // Everything else is an unexpected DB failure and falls through
            // to the generic catch below as a DataAccessException.
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.MalformedContent ||
                ex.Number == ChatSqlErrorCodes.ChatClosed)
            {
                throw new BusinessException(ex.Message, "CHAT_WRITE_REJECTED");
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to save chat message.", "ComplaintChat_Insert", ex);
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
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
            }
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.MalformedContent ||
                ex.Number == ChatSqlErrorCodes.ChatClosed)
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
            catch (SqlException ex) when (ex.Number == ChatSqlErrorCodes.PermissionDenied)
            {
                throw new BusinessException(ex.Message, "CHAT_ACCESS_DENIED");
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
        public void DeactivateAttachment(int attachmentId)
        {
            try
            {
                DbCommand com = db.GetStoredProcCommand("FixMyCity.ComplaintChatAttachment_Deactivate");
                db.AddInParameter(com, "ChatAttachmentId", DbType.Int32, attachmentId);
                db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                // Best-effort cleanup — don't let a cleanup failure mask the original exception below.
                throw new DataAccessException("ComplaintChatAttachment_Deactivate", "ComplaintChatAttachment_Deactivate", ex);
            }
        }
    }
}