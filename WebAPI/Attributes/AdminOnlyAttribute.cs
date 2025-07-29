//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Filters;
//using System.Security.Claims;

//namespace WebAPI.Attributes
//{
//    public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
//    {
//        public void OnAuthorization(AuthorizationFilterContext context)
//        {
//            // Check if user is authenticated
//            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
//            {
//                context.Result = new UnauthorizedObjectResult("Bạn cần đăng nhập để truy cập tài nguyên này.");
//                return;
//            }

//            // Check if user has admin role
//            var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role);
//            if (roleClaim?.Value != "A")
//            {
//                context.Result = new ForbidResult("Bạn không có quyền truy cập tài nguyên này.");
//                return;
//            }

//            // Check if account is active (optional additional check)
//            var accountIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
//            if (accountIdClaim == null)
//            {
//                context.Result = new UnauthorizedObjectResult("Thông tin tài khoản không hợp lệ.");
//                return;
//            }
//        }
//    }
//}