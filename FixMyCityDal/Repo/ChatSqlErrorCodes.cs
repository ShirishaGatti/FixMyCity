using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixMyCity.Infrastructure
{
    internal static class ChatSqlErrorCodes
    {
        public const int PermissionDenied = 52000;   // caller is not a participant in this complaint's chat
        public const int MalformedContent = 52001;   // message text failed SP-side validation
        public const int ChatClosed = 52002;          // complaint is resolved/closed; chat is read-only
    }

    // Complaint_Resolve / Complaint_ConfirmResolution / Complaint_RejectResolution /
    // Complaint_AutoExpireResolutions — see complaint-resolution-workflow.sql
    internal static class ComplaintWorkflowSqlErrorCodes
    {
        public const int ResolveNotFound = 51010;        // not assigned to this officer / not found
        public const int ResolveInvalidState = 51011;     // not Open/In Progress/Reopened
        public const int ResolveStatusMissing = 51012;    // Awaiting Customer Confirmation status not seeded

        public const int ConfirmNotFound = 51020;         // doesn't belong to this citizen / not found
        public const int ConfirmInvalidState = 51021;      // not Awaiting Customer Confirmation
        public const int ConfirmStatusMissing = 51022;     // Closed status not seeded

        public const int RejectNotFound = 51030;
        public const int RejectInvalidState = 51031;
        public const int RejectStatusMissing = 51032;      // Reopened status not seeded

        public const int AutoExpireStatusMissing = 51040;
    }
}