//using Microsoft.AspNetCore.Mvc.Filters;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace WebAPI.Attributes
//{
//    public class RoleBasedAttribute : Attribute, IAuthorizationFilter
//    {
//        private readonly string[] _allowedRoles;

//        public RoleBasedAttribute(params string[] allowedRoles)
//        {
//            _allowedRoles = allowedRoles ?? new string[] { };
//        }

//        public void OnAuthorization(AuthorizationFilterContext context)
//        {
//            // Check if user is authenticated
//            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
//            {
//                context.Result = new UnauthorizedObjectResult("Bạn cần đăng nhập để truy cập tài nguyên này.");
//                return;
//            }

//            // Check if user has one of the allowed roles
//            var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role);
//            if (roleClaim == null || !_allowedRoles.Contains(roleClaim.Value))
//            {
//                context.Result = new ForbidResult("Bạn không có quyền truy cập tài nguyên này.");
//                return;
//            }
//        }
//    }
//}