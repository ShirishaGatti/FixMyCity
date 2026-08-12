using FixMyCityModel.ViewModel;
using System;
using System.Linq;

namespace FixMyCity.Service
{
    public static class ComplaintPdfBuilder
    {
        private const string BrandBlue = "#1574e0";
        private const string BrandDark = "#212529";
        private const string BorderGray = "#D9D9D9";
        private const string RowAlt = "#F2F8FF";

        public static byte[] Build(ComplaintExportViewModel export)
        {
            var b = new PdfBuilder();
            int page = b.CurrentPageIndex;
            double y = b.PageTop;
            const double left = 40, right = 555;

            // Header banner
            b.AddFilledRect(page, left, y - 10, right - left, 46, BrandBlue);
            b.AddText(page, left + 12, y + 16, "FixMyCity", 20, bold: true, colorHex: "#FFFFFF");
            b.AddText(page, left + 12, y, "Municipal Complaint Report", 10, colorHex: "#FFFFFF");
            b.AddTextRightAligned(page, right - 12, y + 16, export.Complaint.ComplaintNumber, 13, bold: true, colorHex: "#FFFFFF");
            b.AddTextRightAligned(page, right - 12, y, export.Complaint.CreatedAt.ToString("dd-MMM-yyyy"), 10, colorHex: "#FFFFFF");
            y -= 66;

            // Complaint details box
            b.AddRect(page, left, y - 90, right - left, 90, colorHex: BorderGray);
            b.AddText(page, left + 10, y - 14, "Complaint Details", 11, bold: true, colorHex: BrandDark);
            b.AddText(page, left + 10, y - 30, $"Title: {export.Complaint.Title}", 9);
            b.AddText(page, left + 10, y - 44, $"Category: {export.Complaint.CategoryName}   |   Priority: {export.Complaint.PriorityName}", 9);
            b.AddText(page, left + 10, y - 58, $"Status: {export.Complaint.StatusName}   |   Filed: {export.Complaint.CreatedAt:dd-MMM-yyyy hh:mm tt}", 9);
            b.AddText(page, left + 10, y - 72, $"Location: {export.Complaint.AddressLine}, {export.Complaint.WardName}, {export.Complaint.CityName}", 9);
            y -= 106;

            // Citizen info box
            b.AddRect(page, left, y - 50, right - left, 50, colorHex: BorderGray);
            b.AddText(page, left + 10, y - 14, "Citizen Details", 11, bold: true, colorHex: BrandDark);
            b.AddText(page, left + 10, y - 30, $"Name: {export.Citizen.Name}", 9);
            b.AddText(page, left + 10, y - 44, $"Email: {export.Citizen.Email}   |   Phone: {export.Citizen.Contact}", 9);
            y -= 66;

            // Description
            b.AddText(page, left + 2, y, "Description", 11, bold: true, colorHex: BrandDark);
            y -= 16;
            foreach (var line in WrapText(export.Complaint.Description, 95))
            {
                b.AddText(page, left + 10, y, line, 9);
                y -= 14;
            }
            y -= 10;

            // Attachments table
            b.AddText(page, left + 2, y, "Attachments", 11, bold: true, colorHex: BrandDark);
            y -= 18;

            if (export.Attachments.Count == 0)
            {
                b.AddText(page, left + 10, y, "No attachments.", 9);
                y -= 16;
            }
            else
            {
                b.AddFilledRect(page, left, y - 4, right - left, 18, RowAlt);
                b.AddText(page, left + 10, y, "File Name", 9, bold: true);
                b.AddTextRightAligned(page, right - 10, y, "Size", 9, bold: true);
                y -= 18;

                foreach (var a in export.Attachments)
                {
                    if (y < b.PageBottom + 20) { page = b.AddPage(); y = b.PageTop; }
                    b.AddText(page, left + 10, y, a.FileName, 9);
                    b.AddTextRightAligned(page, right - 10, y, a.FileSizeDisplay, 9);
                    y -= 16;
                }
            }

            // Footer
            b.AddLine(page, left, b.PageBottom + 14, right, b.PageBottom + 14, colorHex: BorderGray);
            b.AddText(page, left, b.PageBottom, $"Generated on {DateTime.Now:dd-MMM-yyyy hh:mm tt} FixMyCity Municipal Complaint System", 7.5, colorHex: "#999999");

            return b.Build();
        }

        private static System.Collections.Generic.IEnumerable<string> WrapText(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            var words = text.Split(' ');
            var line = "";
            foreach (var word in words)
            {
                if ((line + " " + word).Trim().Length > maxCharsPerLine)
                {
                    yield return line.Trim();
                    line = word;
                }
                else line = (line + " " + word).Trim();
            }
            if (!string.IsNullOrEmpty(line)) yield return line;
        }
    }
}