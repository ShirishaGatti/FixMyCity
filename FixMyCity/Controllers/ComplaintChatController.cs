using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.Service;
using FixMyCityModel.ViewModel;
using System.Linq;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    /// <summary>
    /// Shared endpoint for the Officer&lt;-&gt;Citizen chat thread on a complaint.
    /// One controller (not duplicated per-role) because permission is already
    /// resolved by role+RaisedBy/AssignedTo inside the service/SP layer — the
    /// controller only needs to know who's asking, not which UI they came from.
    /// [RoleAuthorize] with no roles restricts to "any authenticated consumer";
    /// the actual Citizen-vs-Officer participant check happens deeper.
    /// </summary>
    [RoleAuthorize]
    public class ComplaintChatController : Controller
    {
        private readonly IComplaintChatService _chatService;
        private readonly ISessionContext _session;

        public ComplaintChatController()
        {
            _chatService = new ComplaintChatService();
            _session = new JwtSessionContext();
        }

        private int CurrentActorId => _session.ConsumerId;
        private int CurrentRoleId => _session.RoleId;

        /// <summary>
        /// Full thread on first load, or GET ?sinceMessageId=N for polling.
        /// Returns JSON so the same endpoint serves both the initial partial-view
        /// render (via an ajax call from the drawer/officer page) and periodic polls.
        /// </summary>
        [HttpGet]
        public JsonResult GetThread(int complaintId, int sinceMessageId = 0)
        {
            try
            {
                var vm = _chatService.GetThread(complaintId, CurrentActorId, CurrentRoleId, sinceMessageId);

                // Each message rendered through the same _ChatMessageBubble partial
                // used for the initial full-thread render — one rendering path,
                // whether it's page load, poll, or an immediately-sent message.
                var items = vm.Messages
                    .Select(m => new { chatMessageId = m.ChatMessageId, html = RenderPartialToString("_ChatMessageBubble", m) })
                    .ToList();

                return Json(new
                {
                    success = true,
                    isChatOpen = vm.IsChatOpen,
                    messages = items
                }, JsonRequestBehavior.AllowGet);
            }
            catch (BusinessException ex)
            {
                // Covers both "complaint not found" and "not a participant" —
                // the SP deliberately returns the same generic denial for both,
                // so an unauthorized caller can't use the error message to
                // distinguish a nonexistent complaint from one they can't see.
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // These two return a small JSON envelope with server-rendered HTML for
        // the new bubble (via _ChatMessageBubble), rather than raw JSON fields.
        // Rationale: there must be exactly one place that knows how to render a
        // bubble (icons, image vs. file, size formatting, date format). Returning
        // JSON-only would force the JS layer to duplicate that formatting logic,
        // and the two renderers would inevitably drift out of sync over time.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendText(SendChatMessageViewModel vm)
        {
            try
            {
                var message = _chatService.SendText(vm.ComplaintId, CurrentActorId, CurrentRoleId, vm.MessageText);
                string html = RenderPartialToString("_ChatMessageBubble", message);
                return Json(new { success = true, chatMessageId = message.ChatMessageId, html });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendAttachment(int complaintId, System.Web.HttpPostedFileBase file)
        {
            try
            {
                var message = _chatService.SendAttachment(complaintId, CurrentActorId, CurrentRoleId, file);
                string html = RenderPartialToString("_ChatMessageBubble", message);
                return Json(new { success = true, chatMessageId = message.ChatMessageId, html });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Renders a partial view to an HTML string, for use in JSON responses.</summary>
        private string RenderPartialToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new System.IO.StringWriter())
            {
                var viewResult = System.Web.Mvc.ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.ToString();
            }
        }

        [HttpGet]
        public ActionResult DownloadAttachment(int id)
        {
            string fileName, contentType;
            string physicalPath;
            try
            {
                physicalPath = _chatService.GetAttachmentPhysicalPath(id, CurrentActorId, CurrentRoleId, out fileName, out contentType);
            }
            catch (NotFoundException)
            {
                return HttpNotFound();
            }

            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound();
            return File(physicalPath, contentType, fileName);
        }
    }
}
