using JwtAuth.Entity;
using JwtAuth.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuth.Controllers
{
    public class BaseController : ControllerBase
    {
        protected Guid GetValidUserId()
        {
            var userId = UserClaimHelper.GetUserId(User);

            if (userId == null)
            {
                throw new UnauthorizedAccessException("User ID not found or invalid in token.");
            }

            return userId.Value;
        }
        protected string GetValidUserRole()
        {
            var userRole = UserClaimHelper.GetUserRole(User);
            if (userRole == null) 
            {
                throw new UnauthorizedAccessException("User Role not found");
            }
            return userRole;
        }
    }
}
