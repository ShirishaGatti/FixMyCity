using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace FixMyCity.service
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly IConsumerService _consumerService;

        public ComplaintService()
        {
            _complaintRepo = new ComplaintRepository();
            _consumerService = new ConsumerService();
        }

        //public ComplaintService(IComplaintRepository complaintRepo,
        //                        IConsumerService consumerService)
        //{
        //    _complaintRepo = complaintRepo;
        //    _consumerService = consumerService;
        //}

        public MyComplaintsViewModel GetComplaints(int consumerId, int assignedTo, int roleId)
        {
            return new MyComplaintsViewModel
            {
                Complaints = _complaintRepo.GetComplaints(consumerId, assignedTo, roleId),
                Statuses = _complaintRepo.GetStatuses(),
                Categories = _complaintRepo.GetCategories(),
                Priorities = _complaintRepo.GetPriorities(),
                Cities = _consumerService.GetCities()
            };
        }

        public int FileComplaint(FileComplaintViewModel vm, int consumerId)
        {
            if (string.IsNullOrWhiteSpace(vm.Title))
                throw new BusinessException("Title is required.");

            if (string.IsNullOrWhiteSpace(vm.Description))
                throw new BusinessException("Description is required.");

            Complaint complaint = new Complaint
            {
                Title = vm.Title,
                Description = vm.Description,
                CategoryId = vm.CategoryId,
                PriorityId = vm.PriorityId,
                RaisedBy = consumerId,
                AddressLine = vm.AddressLine,
                Landmark = vm.Landmark,
                WardId = vm.WardId,
                CityId = vm.CityId
            };

            return _complaintRepo.Create(complaint);
        }
        private static readonly string[] AllowedExtensions =
    (ConfigurationManager.AppSettings["AllowedAttachmentExtensions"] ?? ".jpg,.jpeg,.png,.pdf,.doc,.docx")
        .Split(',').Select(e => e.Trim().ToLowerInvariant()).ToArray();

        private static readonly int MaxAttachmentSizeMB =
            Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttachmentSizeMB"] ?? "5");

        private static readonly long MaxAttachmentSizeBytes = MaxAttachmentSizeMB * 1024L * 1024L;

        // Computed per-call, not cached statically — Server.MapPath needs a live
        // HttpContext, which isn't guaranteed to exist at static-field-init time.
        private static string GetUploadRoot() =>
            HttpContext.Current.Server.MapPath(ConfigurationManager.AppSettings["AttachmentUploadRoot"] ?? "~/App_Data/Uploads/Complaints");

        public List<AttachmentViewModel> UploadAttachments(int complaintId, int consumerId, IEnumerable<HttpPostedFileBase> files)
        {
            var result = new List<AttachmentViewModel>();
            if (files == null) return result;

            var validFiles = files.Where(f => f != null && f.ContentLength > 0).ToList();
            if (validFiles.Count == 0) return result;

            var existingNames = new HashSet<string>(
                _complaintRepo.GetAttachmentsByComplaintId(complaintId, consumerId).Select(a => a.FileName.ToLowerInvariant()));

            string complaintFolder = Path.Combine(GetUploadRoot(), complaintId.ToString());
            Directory.CreateDirectory(complaintFolder);

            foreach (var file in validFiles)
            {
                string originalName = Path.GetFileName(file.FileName);
                string ext = Path.GetExtension(originalName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext))
                    throw new BusinessException($"'{originalName}' is not an allowed file type. Allowed: jpg, jpeg, png, pdf, doc, docx.", "INVALID_FILE_TYPE");

                if (file.ContentLength > MaxAttachmentSizeBytes)
                    throw new BusinessException($"'{originalName}' exceeds the {MaxAttachmentSizeMB}MB limit.", "FILE_TOO_LARGE");

                if (existingNames.Contains(originalName.ToLowerInvariant()))
                    throw new BusinessException($"'{originalName}' has already been uploaded to this complaint.", "DUPLICATE_FILE");

                int attachmentId = _complaintRepo.CreateAttachment(complaintId, originalName, file.ContentType, file.ContentLength, consumerId);

                try
                {
                    file.SaveAs(Path.Combine(complaintFolder, attachmentId + ext));
                }
                catch (Exception)
                {
                    // Compensate: don't leave a DB row pointing at a file that never
                    // landed on disk. Same "undo the earlier step" idea as the SP's
                    // ROLLBACK — just done in C# because the disk write can't be
                    // part of the SQL transaction.
                    _complaintRepo.DeleteAttachment(attachmentId, consumerId);
                    throw new BusinessException($"Failed to save '{originalName}' to disk. Please try again.", "UPLOAD_FAILED");
                }

                existingNames.Add(originalName.ToLowerInvariant());
                result.Add(new AttachmentViewModel
                {
                    AttachmentId = attachmentId,
                    FileName = originalName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.ContentLength,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return result;
        }

        public List<AttachmentViewModel> GetAttachments(int complaintId, int consumerId) =>
            _complaintRepo.GetAttachmentsByComplaintId(complaintId, consumerId)
                .Select(a => new AttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                    CreatedAt = a.CreatedAt
                }).ToList();

        public Complaint GetComplaintDetails(int complaintId, int consumerId)
        {
            var complaint = _complaintRepo.GetById(complaintId, consumerId);
            if (complaint == null) throw new NotFoundException("Complaint not found.");
            return complaint;
        }

        public Attachment GetAttachmentForDownload(int attachmentId, int consumerId)
        {
            var attachment = _complaintRepo.GetAttachmentById(attachmentId, consumerId);
            if (attachment == null) throw new NotFoundException("Attachment not found.");
            return attachment;
        }

        public string GetPhysicalPath(Attachment attachment) =>
            Path.Combine(GetUploadRoot(), attachment.ComplaintId.ToString(), attachment.AttachmentId + Path.GetExtension(attachment.FileName));

        public void DeleteAttachment(int attachmentId, int consumerId)
        {
            var attachment = _complaintRepo.GetAttachmentById(attachmentId, consumerId);
            if (attachment == null) throw new NotFoundException("Attachment not found.");

            if (!string.Equals(attachment.ComplaintStatusName, "Open", StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("Attachments can only be removed while the complaint is still Open.", "COMPLAINT_NOT_OPEN");

            _complaintRepo.DeleteAttachment(attachmentId, consumerId);

            string physicalPath = GetPhysicalPath(attachment);
            if (File.Exists(physicalPath)) File.Delete(physicalPath);
        }
        public void UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId, int roleId)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            if (categoryId <= 0 || priorityId <= 0 || statusId <= 0)
                throw new BusinessException("Category, priority and status are required.", "INVALID_INPUT");
            _complaintRepo.UpdateComplaint(complaintId, categoryId, priorityId, statusId, assignedTo, actorId, roleId);
        }

        public void ResolveComplaint(int complaintId, int officerId)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            _complaintRepo.ResolveComplaint(complaintId, officerId);
        }

        public void ConfirmResolution(int complaintId, int consumerId)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            _complaintRepo.ConfirmResolution(complaintId, consumerId);
        }

        public void RejectResolution(int complaintId, int consumerId, string reason)
        {
            if (complaintId <= 0) throw new BusinessException("Invalid complaint.", "INVALID_COMPLAINT");
            if (!string.IsNullOrWhiteSpace(reason) && reason.Length > 500)
                throw new BusinessException("Reason must be 500 characters or fewer.", "REASON_TOO_LONG");
            _complaintRepo.RejectResolution(complaintId, consumerId, reason);
        }
        public int SaveComplaint(FileComplaintViewModel vm, int consumerId, int roleId)
        {
            if (string.IsNullOrWhiteSpace(vm.Title)) throw new BusinessException("Title is required.");
            if (string.IsNullOrWhiteSpace(vm.Description)) throw new BusinessException("Description is required.");

            var complaint = new Complaint
            {
                ComplaintId = vm.ComplaintId ?? 0,
                Title = vm.Title,
                Description = vm.Description,
                CategoryId = vm.CategoryId,
                PriorityId = vm.PriorityId,
                RaisedBy = consumerId,
                AddressLine = vm.AddressLine,
                Landmark = vm.Landmark,
                WardId = vm.WardId,
                CityId = vm.CityId
            };
            return _complaintRepo.SaveComplaint(complaint, roleId, consumerId);
        }

        public OfficerDashboardViewModel GetOfficerDashboard(int officerId, int roleId)
        {
            _complaintRepo.ExpireOverdueResolutions();
            var complaints = _complaintRepo.GetComplaints(null, officerId, roleId);
            var now = DateTime.Now;

            return new OfficerDashboardViewModel
            {
                TotalAssigned = complaints.Count,
                OpenCount = complaints.Count(c => string.Equals(c.StatusName, "Open", StringComparison.OrdinalIgnoreCase)),
                InProgressCount = complaints.Count(c => string.Equals(c.StatusName, "In Progress", StringComparison.OrdinalIgnoreCase)),
                ResolvedCount = complaints.Count(c => string.Equals(c.StatusName, "Resolved", StringComparison.OrdinalIgnoreCase)),
                ClosedCount = complaints.Count(c => string.Equals(c.StatusName, "Closed", StringComparison.OrdinalIgnoreCase)),
                TodayCount = complaints.Count(c => c.CreatedAt.Date == now.Date),
                WeeklyCount = complaints.Count(c => c.CreatedAt.Date >= now.Date.AddDays(-7)),
                MonthlyCount = complaints.Count(c => c.CreatedAt.Date >= now.Date.AddMonths(-1)),
                PriorityBreakdown = complaints
                    .GroupBy(c => c.PriorityName)
                    .Select(g => new ValueCount { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList(),
                RecentComplaints = complaints
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList()
            };
        }

        public MyComplaintsViewModel GetOfficerComplaints(int officerId, OfficerComplaintsQuery query)
        {
            if (query == null)
                query = new OfficerComplaintsQuery();

            _complaintRepo.ExpireOverdueResolutions();

            var statuses = _complaintRepo.GetStatuses();
            var categories = _complaintRepo.GetCategories();
            var priorities = _complaintRepo.GetPriorities();
            var complaints = _complaintRepo.GetAssignedByOfficerId(officerId);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchLower = query.SearchTerm.Trim().ToLowerInvariant();
                complaints = complaints.Where(c =>
                    (!string.IsNullOrEmpty(c.ComplaintNumber) && c.ComplaintNumber.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.Title) && c.Title.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.CategoryName) && c.CategoryName.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.PriorityName) && c.PriorityName.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(c.StatusName) && c.StatusName.ToLowerInvariant().Contains(searchLower)))
                    .ToList();
            }

            if (query.StatusId.HasValue)
                complaints = complaints.Where(c => c.StatusId == query.StatusId.Value).ToList();

            if (query.PriorityId.HasValue)
                complaints = complaints.Where(c => c.PriorityId == query.PriorityId.Value).ToList();

            if (query.CategoryId.HasValue)
                complaints = complaints.Where(c => c.CategoryId == query.CategoryId.Value).ToList();

            complaints = SortRegisteredComplaints(complaints, query.SortColumn, query.SortDirection);

            var totalRecords = complaints.Count;
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var pagedComplaints = complaints.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new MyComplaintsViewModel
            {
                SearchTerm = query.SearchTerm,
                StatusId = query.StatusId,
                PriorityId = query.PriorityId,
                CategoryId = query.CategoryId,
                SortColumn = query.SortColumn,
                SortDirection = query.SortDirection,
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                Complaints = pagedComplaints,
                Statuses = statuses,
                Categories = categories,
                Priorities = priorities
            };
        }
        public Complaint GetAssignedComplaint(int officerId, int complaintId)
        {
            return _complaintRepo.GetAssignedComplaintById(complaintId, officerId);
        }
        public void DeleteComplaint(int complaintId, int consumerId)
        {
            bool deleted = _complaintRepo.DeleteComplaint(complaintId, consumerId);
            if (!deleted)
                throw new BusinessException("Complaint could not be deleted it may not be Open anymore.", "COMPLAINT_NOT_DELETABLE");
        }

        public MyComplaintsViewModel Search(int consumerId, ComplaintListFilterViewModel filter)
        {
            _complaintRepo.ExpireOverdueResolutions();
            ComplaintSearchResult vm = _complaintRepo.Search(consumerId, filter);
            return new MyComplaintsViewModel
            {
                Complaints = vm.Complaints,
                Categories = _complaintRepo.GetCategories(),
                Priorities = _complaintRepo.GetPriorities(),
                Statuses = _complaintRepo.GetStatuses(),
                Cities = _consumerService.GetCities(),
                Filter = filter,
                TotalCount = vm.TotalCount,
                TotalPages = (int)Math.Ceiling(vm.TotalCount / (double)filter.PageSize)
            };
        }
        private static List<Complaint> SortRegisteredComplaints(List<Complaint> complaints, string sortColumn, string sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortColumn?.ToLowerInvariant())
            {
                case "complaintnumber":
                    return descending
                        ? complaints.OrderByDescending(c => c.ComplaintNumber).ToList()
                        : complaints.OrderBy(c => c.ComplaintNumber).ToList();
                case "title":
                    return descending
                        ? complaints.OrderByDescending(c => c.Title).ToList()
                        : complaints.OrderBy(c => c.Title).ToList();
                case "category":
                    return descending
                        ? complaints.OrderByDescending(c => c.CategoryName).ToList()
                        : complaints.OrderBy(c => c.CategoryName).ToList();
                case "priority":
                    return descending
                        ? complaints.OrderByDescending(c => c.PriorityName).ToList()
                        : complaints.OrderBy(c => c.PriorityName).ToList();
                case "status":
                    return descending
                        ? complaints.OrderByDescending(c => c.StatusName).ToList()
                        : complaints.OrderBy(c => c.StatusName).ToList();
                case "createdat":
                default:
                    return descending
                        ? complaints.OrderByDescending(c => c.CreatedAt).ToList()
                        : complaints.OrderBy(c => c.CreatedAt).ToList();
            }
        }

        public ComplaintExportViewModel GetComplaintForExport(int complaintId, int consumerId)
        {
            var complaint = GetComplaintDetails(complaintId, consumerId);
            var citizen = _consumerService.GetProfile(consumerId);
            var attachments = GetAttachments(complaintId, consumerId);
            return new ComplaintExportViewModel { Complaint = complaint, Citizen = citizen, Attachments = attachments };
        }
    }
}