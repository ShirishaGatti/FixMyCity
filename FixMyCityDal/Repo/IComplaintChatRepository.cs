using FixMyCityModel.Model;
using System.Collections.Generic;

namespace FixMyCity.Repository
{
    public interface IComplaintChatRepository
    {
        /// <summary>
        /// Returns messages newer than sinceMessageId (0 = full thread) plus
        /// whether the thread is currently open for writing.
        /// Throws BusinessException (via service) if requester isn't a participant —
        /// the SP itself throws a SQL error which the repo translates.
        /// </summary>
        ComplaintThreadResult GetThread(int complaintId, int requesterId, int requesterRoleId, int sinceMessageId);

        int InsertTextMessage(int complaintId, int senderId, int senderRoleId, string messageText);

        int InsertAttachmentMessage(int complaintId, int senderId, int senderRoleId, int attachmentId);

        int CreateAttachment(int complaintId, string fileName, string contentType, long fileSizeBytes, int uploadedBy);

        ChatAttachmentRecord GetAttachmentById(int chatAttachmentId, int requesterId, int requesterRoleId);
    }

    /// <summary>Thin row shape for a chat attachment lookup (download flow).</summary>
    public class ChatAttachmentRecord
    {
        public int ChatAttachmentId { get; set; }
        public int ComplaintId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public int UploadedBy { get; set; }
    }
}
