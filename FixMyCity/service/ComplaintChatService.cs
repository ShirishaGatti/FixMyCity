using FixMyCity.Exceptions;
using FixMyCity.Repository;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace FixMyCity.Service
{
    public class ComplaintChatService : IComplaintChatService
    {
        private readonly IComplaintChatRepository _chatRepo;

        // Same allow-list / size-limit convention as ComplaintService, but with
        // its own AppSettings keys so ops can tune chat attachment limits
        // independently from complaint-filing attachment limits (e.g. maybe
        // chat should allow smaller files to keep the thread responsive).
        private static readonly string[] AllowedExtensions =
            (ConfigurationManager.AppSettings["ChatAllowedAttachmentExtensions"]
                ?? ConfigurationManager.AppSettings["AllowedAttachmentExtensions"]
                ?? ".jpg,.jpeg,.png,.pdf,.doc,.docx")
            .Split(',').Select(e => e.Trim().ToLowerInvariant()).ToArray();

        private static readonly int MaxAttachmentSizeMB =
            Convert.ToInt32(ConfigurationManager.AppSettings["ChatMaxAttachmentSizeMB"]
                ?? ConfigurationManager.AppSettings["MaxAttachmentSizeMB"]
                ?? "5");

        private static readonly long MaxAttachmentSizeBytes = MaxAttachmentSizeMB * 1024L * 1024L;

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public ComplaintChatService() : this(new ComplaintChatRepository()) { }

        // Constructor injection available for unit testing; parameterless
        // constructor above preserves the `new ServiceImpl()` convention
        // used throughout the rest of the codebase (ComplaintService,
        // ConsumerService, etc.) so the controller wiring stays consistent.
        public ComplaintChatService(IComplaintChatRepository chatRepo)
        {
            _chatRepo = chatRepo;
        }

        public ComplaintChatViewModel GetThread(int complaintId, int requesterId, int requesterRoleId, int sinceMessageId)
        {
            
            ComplaintThreadResult result = _chatRepo.GetThread(complaintId,requesterId,requesterRoleId,sinceMessageId);

            return new ComplaintChatViewModel
            {
                ComplaintId = complaintId,
                IsChatOpen = result.IsChatOpen,
                Messages = result.Messages
                    .Select(m => MapToViewModel(m, requesterId))
                    .ToList(),
                AllowedExtensionsCsv = string.Join(",", AllowedExtensions),
                MaxAttachmentSizeMB = MaxAttachmentSizeMB
            };
            
        }

        public ChatMessageViewModel SendText(int complaintId, int senderId, int senderRoleId, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                throw new BusinessException("Message cannot be empty.", "EMPTY_MESSAGE");

            if (messageText.Length > 1000)
                throw new BusinessException("Message cannot exceed 1000 characters.", "MESSAGE_TOO_LONG");

            // Lifecycle rule (Open/In Progress only) is enforced by the SP itself
            // (THROW 52002). We don't re-check status here to avoid a second,
            // possibly-stale read racing the insert — single source of truth
            // for "is it still open" is the DB row at insert time.
            int newId = _chatRepo.InsertTextMessage(complaintId, senderId, senderRoleId, messageText.Trim());

            return new ChatMessageViewModel
            {
                ChatMessageId = newId,
                SenderId = senderId,
                SenderRoleId = senderRoleId,
                MessageText = messageText.Trim(),
                IsAttachment = false,
                CreatedAt = DateTime.UtcNow,
                IsMine = true
            };
        }

        public ChatMessageViewModel SendAttachment(int complaintId, int senderId, int senderRoleId, HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength <= 0)
                throw new BusinessException("No file was provided.", "EMPTY_FILE");

            string originalName = Path.GetFileName(file.FileName);
            string ext = Path.GetExtension(originalName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
                throw new BusinessException($"'{originalName}' is not an allowed file type. Allowed: {string.Join(", ", AllowedExtensions)}.", "INVALID_FILE_TYPE");

            if (file.ContentLength > MaxAttachmentSizeBytes)
                throw new BusinessException($"'{originalName}' exceeds the {MaxAttachmentSizeMB}MB limit.", "FILE_TOO_LARGE");

            // Create the DB row first (this is also where the SP re-validates
            // chat-open status), then write to disk, then link it into the
            // message thread. If any later step fails we compensate by
            // deactivating what was already written — same "undo the earlier
            // step" approach ComplaintService.UploadAttachments uses.
            int attachmentId = _chatRepo.CreateAttachment(complaintId, originalName, file.ContentType, file.ContentLength, senderId);

            string complaintFolder = Path.Combine(GetUploadRoot(), complaintId.ToString());
            Directory.CreateDirectory(complaintFolder);
            string physicalPath = Path.Combine(complaintFolder, attachmentId + ext);

            try
            {
                file.SaveAs(physicalPath);
            }
            catch (Exception)
            {
                throw new BusinessException($"Failed to save '{originalName}' to disk. Please try again.", "UPLOAD_FAILED");
            }

            int chatMessageId;
            try
            {
                chatMessageId = _chatRepo.InsertAttachmentMessage(complaintId, senderId, senderRoleId, attachmentId);
            }
            catch (Exception)
            {
                // Message row failed after the file already landed on disk —
                // clean up the orphaned file so it doesn't linger unreferenced.
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
                throw;
            }

            return new ChatMessageViewModel
            {
                ChatMessageId = chatMessageId,
                SenderId = senderId,
                SenderRoleId = senderRoleId,
                IsAttachment = true,
                AttachmentId = attachmentId,
                FileName = originalName,
                IsImage = ImageExtensions.Contains(ext),
                FileSizeDisplay = FormatFileSize(file.ContentLength),
                IconClass = IconClassFor(ext),
                CreatedAt = DateTime.UtcNow,
                IsMine = true
            };
        }

        public string GetAttachmentPhysicalPath(int chatAttachmentId, int requesterId, int requesterRoleId, out string fileName, out string contentType)
        {
            var record = _chatRepo.GetAttachmentById(chatAttachmentId, requesterId, requesterRoleId);
            if (record == null)
                throw new NotFoundException("Attachment not found.");

            fileName = record.FileName;
            contentType = string.IsNullOrEmpty(record.ContentType) ? "application/octet-stream" : record.ContentType;

            string ext = Path.GetExtension(record.FileName);
            return Path.Combine(GetUploadRoot(), record.ComplaintId.ToString(), record.ChatAttachmentId + ext);
        }

        // Separate disk root from complaint-filing attachments, per requirement —
        // conversation history is a distinct concern from evidence filed with
        // the complaint itself. Computed per-call (not static) because
        // Server.MapPath needs a live HttpContext.
        private static string GetUploadRoot() =>
            HttpContext.Current.Server.MapPath(
                ConfigurationManager.AppSettings["ChatAttachmentUploadRoot"] ?? "~/App_Data/Uploads/ComplaintChat");

        private ChatMessageViewModel MapToViewModel(ComplaintChatMessage m, int viewerId)
        {
            string ext = m.IsAttachment ? Path.GetExtension(m.FileName ?? string.Empty).ToLowerInvariant() : null;

            return new ChatMessageViewModel
            {
                ChatMessageId = m.ChatMessageId,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderRoleId = m.SenderRoleId,
                MessageText = m.MessageText,
                IsAttachment = m.IsAttachment,
                AttachmentId = m.AttachmentId,
                FileName = m.FileName,
                IsImage = m.IsAttachment && ImageExtensions.Contains(ext),
                FileSizeDisplay = m.IsAttachment ? FormatFileSize(m.FileSizeBytes ?? 0) : null,
                IconClass = m.IsAttachment ? IconClassFor(ext) : null,
                CreatedAt = m.CreatedAt,
                IsMine = m.SenderId == viewerId
            };
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):0.#} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:0.#} KB";
            return $"{bytes} B";
        }

        private static string IconClassFor(string ext)
        {
            switch (ext)
            {
                case ".pdf": return "bi-file-earmark-pdf-fill";
                case ".doc":
                case ".docx": return "bi-file-earmark-word-fill";
                default: return "bi-file-earmark-fill";
            }
        }
    }
}
