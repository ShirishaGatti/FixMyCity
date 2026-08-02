using FixMyCityModel.ViewModel;
using System.Web;

namespace FixMyCity.Service
{
    public interface IComplaintChatService
    {
        /// <summary>
        /// Full thread if sinceMessageId is 0, or only newer messages for polling.
        /// Throws NotFoundException if the complaint doesn't exist, BusinessException
        /// if the requester isn't a participant (raised by the SP).
        /// </summary>
        ComplaintChatViewModel GetThread(int complaintId, int requesterId, int requesterRoleId, int sinceMessageId);

        /// <summary>Posts a plain text message. Throws BusinessException if chat is closed or text is empty.</summary>
        ChatMessageViewModel SendText(int complaintId, int senderId, int senderRoleId, string messageText);

        /// <summary>
        /// Validates, stores to disk, and posts a single file as its own message.
        /// Throws BusinessException for invalid type/size or if chat is closed.
        /// </summary>
        ChatMessageViewModel SendAttachment(int complaintId, int senderId, int senderRoleId, HttpPostedFileBase file);

        /// <summary>Resolves the physical path to a chat attachment for download, verifying permission first.</summary>
        string GetAttachmentPhysicalPath(int chatAttachmentId, int requesterId, int requesterRoleId, out string fileName, out string contentType);
    }
}
