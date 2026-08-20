/*using FixMyCity.Exceptions;
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

            // Enforce 10-file cap on chat attachments for this complaint (separate from the
            // complaint-form 10-file cap — each bucket has its own independent limit).
            var complaintRepo = new ComplaintRepository();
            int chatCount = complaintRepo.GetComplaintChatAttachmentCount(complaintId);
            if (chatCount + 1 > 10)
                throw new BusinessException($"A maximum of 10 documents can be uploaded via chat per complaint. You already have {chatCount}.", "TOO_MANY_FILES");

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
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
                _chatRepo.DeactivateAttachment(attachmentId);   // don't leave an orphaned metadata row either
                throw;
            }

            return BuildAttachmentViewModel(chatMessageId, senderId, senderRoleId, attachmentId,
            originalName, ext, file.ContentLength, DateTime.UtcNow, isMine: true);
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
            if (m.IsAttachment)
            {
                string ext = Path.GetExtension(m.FileName ?? string.Empty).ToLowerInvariant();
                return BuildAttachmentViewModel(m.ChatMessageId, m.SenderId, m.SenderRoleId,
                    m.AttachmentId ?? 0, m.FileName, ext, m.FileSizeBytes ?? 0, m.CreatedAt,
                    isMine: m.SenderId == viewerId);
            }

            return new ChatMessageViewModel
            {
                ChatMessageId = m.ChatMessageId,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderRoleId = m.SenderRoleId,
                MessageText = m.MessageText,
                IsAttachment = false,
                CreatedAt = m.CreatedAt,
                IsMine = m.SenderId == viewerId
            };
        }
        private ChatMessageViewModel BuildAttachmentViewModel(int chatMessageId, int senderId, int senderRoleId,
        int attachmentId, string fileName, string ext, long fileSizeBytes, DateTime createdAt, bool isMine)
        {
            return new ChatMessageViewModel
            {
                ChatMessageId = chatMessageId,
                SenderId = senderId,
                SenderRoleId = senderRoleId,
                IsAttachment = true,
                AttachmentId = attachmentId,
                FileName = fileName,
                IsImage = ImageExtensions.Contains(ext),
                FileSizeDisplay = FormatFileSize(fileSizeBytes),
                IconClass = IconClassFor(ext),
                CreatedAt = createdAt,
                IsMine = isMine
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
*/
using FixMyCity.Exceptions;
using FixMyCity.Repository;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;
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

        // How many files can be sent in one attach/drop action. Each file
        // still becomes its own chat message (one bubble per attachment,
        // same as today) — this just bounds how many bubbles one action
        // can create at once.
        private static readonly int MaxAttachmentsPerMessage =
            Convert.ToInt32(ConfigurationManager.AppSettings["ChatMaxAttachmentsPerMessage"] ?? "10");

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

            ComplaintThreadResult result = _chatRepo.GetThread(complaintId, requesterId, requesterRoleId, sinceMessageId);

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

        // One chat message per file, same as before — this just lets the
        // composer send several attachments in a single attach/drop action
        // instead of one round trip per file.
        public List<ChatMessageViewModel> SendAttachments(int complaintId, int senderId, int senderRoleId, IEnumerable<HttpPostedFileBase> files)
        {
            var validFiles = (files ?? Enumerable.Empty<HttpPostedFileBase>())
                .Where(f => f != null && f.ContentLength > 0)
                .ToList();

            if (validFiles.Count == 0)
                throw new BusinessException("No file was provided.", "EMPTY_FILE");

            // Reject the whole batch up front — same reasoning as the
            // complaint-form limit: don't send 10 and silently drop the 11th.
            if (validFiles.Count > MaxAttachmentsPerMessage)
                throw new BusinessException(
                    $"You can attach up to {MaxAttachmentsPerMessage} files at a time. You selected {validFiles.Count}.",
                    "TOO_MANY_FILES");

            // Enforce the 10-file cap on chat attachments for this complaint.
            var complaintRepo = new ComplaintRepository();
            int chatCount = complaintRepo.GetComplaintChatAttachmentCount(complaintId);
            if (chatCount + validFiles.Count > 10)
                throw new BusinessException($"A maximum of 10 documents can be uploaded via chat per complaint. You already have {chatCount}.", "TOO_MANY_FILES");

            // Validate every file before creating any DB rows or writing
            // anything to disk, so a bad file in the batch doesn't leave a
            // partial set of bubbles behind.
            foreach (var file in validFiles)
            {
                string name = Path.GetFileName(file.FileName);
                string ext = Path.GetExtension(name).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext))
                    throw new BusinessException($"'{name}' is not an allowed file type. Allowed: {string.Join(", ", AllowedExtensions)}.", "INVALID_FILE_TYPE");

                if (file.ContentLength > MaxAttachmentSizeBytes)
                    throw new BusinessException($"'{name}' exceeds the {MaxAttachmentSizeMB}MB limit.", "FILE_TOO_LARGE");
            }

            var results = new List<ChatMessageViewModel>();
            foreach (var file in validFiles)
                results.Add(SendSingleAttachment(complaintId, senderId, senderRoleId, file));

            return results;
        }

        private ChatMessageViewModel SendSingleAttachment(int complaintId, int senderId, int senderRoleId, HttpPostedFileBase file)
        {
            string originalName = Path.GetFileName(file.FileName);
            string ext = Path.GetExtension(originalName).ToLowerInvariant();

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
                _chatRepo.DeactivateAttachment(attachmentId);
                throw new BusinessException($"Failed to save '{originalName}' to disk. Please try again.", "UPLOAD_FAILED");
            }

            int chatMessageId;
            try
            {
                chatMessageId = _chatRepo.InsertAttachmentMessage(complaintId, senderId, senderRoleId, attachmentId);
            }
            catch (Exception)
            {
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
                _chatRepo.DeactivateAttachment(attachmentId);   // don't leave an orphaned metadata row either
                throw;
            }

            return BuildAttachmentViewModel(chatMessageId, senderId, senderRoleId, attachmentId,
            originalName, ext, file.ContentLength, DateTime.UtcNow, isMine: true);
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
            if (m.IsAttachment)
            {
                string ext = Path.GetExtension(m.FileName ?? string.Empty).ToLowerInvariant();
                return BuildAttachmentViewModel(m.ChatMessageId, m.SenderId, m.SenderRoleId,
                    m.AttachmentId ?? 0, m.FileName, ext, m.FileSizeBytes ?? 0, m.CreatedAt,
                    isMine: m.SenderId == viewerId);
            }

            return new ChatMessageViewModel
            {
                ChatMessageId = m.ChatMessageId,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderRoleId = m.SenderRoleId,
                MessageText = m.MessageText,
                IsAttachment = false,
                CreatedAt = m.CreatedAt,
                IsMine = m.SenderId == viewerId
            };
        }
        private ChatMessageViewModel BuildAttachmentViewModel(int chatMessageId, int senderId, int senderRoleId,
        int attachmentId, string fileName, string ext, long fileSizeBytes, DateTime createdAt, bool isMine)
        {
            return new ChatMessageViewModel
            {
                ChatMessageId = chatMessageId,
                SenderId = senderId,
                SenderRoleId = senderRoleId,
                IsAttachment = true,
                AttachmentId = attachmentId,
                FileName = fileName,
                IsImage = ImageExtensions.Contains(ext),
                FileSizeDisplay = FormatFileSize(fileSizeBytes),
                IconClass = IconClassFor(ext),
                CreatedAt = createdAt,
                IsMine = isMine
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