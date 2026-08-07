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
}