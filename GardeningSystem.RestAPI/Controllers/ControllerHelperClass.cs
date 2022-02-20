using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common;
using Microsoft.AspNetCore.Http;

namespace GardeningSystem.RestAPI.Controllers {

    /// <summary>
    /// Obsolete class. Was used to get the id of a user from the http context.
    /// </summary>
    public static class ControllerHelperClass {

        public static string GetUserId(HttpContext httpContext) {
            return httpContext.User.Identities.FirstOrDefault()?.Claims?.Where(c => c.Type == JwtClaimTypes.UserID).FirstOrDefault()?.Value;
        }
    }
}
