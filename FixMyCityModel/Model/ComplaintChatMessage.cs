using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace FixMyCityModel.Model
{
    /// <summary>
    /// A single message in a complaint's Officer&lt;-&gt;Citizen chat thread.
    /// Exactly one of MessageText / Attachment* is populated — enforced by
    /// CK_ChatMessage_Content at the DB level and mirrored here for clarity.
    /// </summary>
    ///  
    public class ComplaintThreadResult
    {
        public List<ComplaintChatMessage> Messages { get; set; }

    public bool IsChatOpen { get; set; }

    public ComplaintThreadResult()
    {
        Messages = new List<ComplaintChatMessage>();
    }
}
public class ComplaintChatMessage
    {
        public int ChatMessageId { get; set; }
        public int ComplaintId { get; set; }

        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public int SenderRoleId { get; set; }

        public string MessageText { get; set; }

        public int? AttachmentId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long? FileSizeBytes { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsAttachment => AttachmentId.HasValue;
    }
}
