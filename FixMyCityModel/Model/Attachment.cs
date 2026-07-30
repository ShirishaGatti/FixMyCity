using System;

namespace FixMyCityModel.Model
{
    public class Attachment
    {
        public int AttachmentId { get; set; }
        public int ComplaintId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public int UploadedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Only populated by ComplaintAttachment_GetById — used to enforce
        // the "can only delete while complaint is Open" rule in the service.
        public string ComplaintStatusName { get; set; }
    }
}