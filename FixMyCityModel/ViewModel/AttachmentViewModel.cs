using System;
using System.IO;

namespace FixMyCityModel.ViewModel
{
    public class AttachmentViewModel
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsImage => ContentType != null && ContentType.StartsWith("image/");

        public string FileSizeDisplay
        {
            get
            {
                if (FileSizeBytes < 1024) return FileSizeBytes + " B";
                if (FileSizeBytes < 1024 * 1024) return Math.Round(FileSizeBytes / 1024.0, 1) + " KB";
                return Math.Round(FileSizeBytes / (1024.0 * 1024.0), 2) + " MB";
            }
        }

        public string IconClass
        {
            get
            {
                switch (Path.GetExtension(FileName ?? "").ToLowerInvariant())
                {
                    case ".pdf": return "bi-file-earmark-pdf-fill text-danger";
                    case ".doc":
                    case ".docx": return "bi-file-earmark-word-fill text-primary";
                    default: return "bi-file-earmark-fill";
                }
            }
        }
    }
}