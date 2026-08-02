using System;
using System.Collections.Generic;
using System.Web;

namespace FixMyCityModel.ViewModel
{
    /// <summary>
    /// Single message rendered in the chat thread. Kept separate from the
    /// domain Model so the view never depends on data-layer shapes directly
    /// (same separation you use elsewhere, e.g. AttachmentViewModel vs Attachment).
    /// </summary>
    public class ChatMessageViewModel
    {
        public int ChatMessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public int SenderRoleId { get; set; }
        public string SenderRoleLabel => SenderRoleId == 3 ? "Officer" : "Citizen";

        public string MessageText { get; set; }

        public bool IsAttachment { get; set; }
        public int? AttachmentId { get; set; }
        public string FileName { get; set; }
        public bool IsImage { get; set; }
        public string FileSizeDisplay { get; set; }
        public string IconClass { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Set by the controller/view relative to the current viewer, not stored.</summary>
        public bool IsMine { get; set; }
    }

    /// <summary>
    /// Full thread payload — used both for the initial partial-view render
    /// and as the JSON shape returned by the polling endpoint.
    /// </summary>
    public class ComplaintChatViewModel
    {
        public int ComplaintId { get; set; }
        public bool IsChatOpen { get; set; }
        public List<ChatMessageViewModel> Messages { get; set; } = new List<ChatMessageViewModel>();

        public string AllowedExtensionsCsv { get; set; }
        public int MaxAttachmentSizeMB { get; set; }
    }

    /// <summary>Input for posting a text message.</summary>
    public class SendChatMessageViewModel
    {
        public int ComplaintId { get; set; }
        public string MessageText { get; set; }
    }

    /// <summary>Input for posting a single file attachment as a message.</summary>
    public class SendChatAttachmentViewModel
    {
        public int ComplaintId { get; set; }
        public HttpPostedFileBase File { get; set; }
    }
}
