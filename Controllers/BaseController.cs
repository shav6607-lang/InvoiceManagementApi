using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using InvoiceManagementApi.Models;

namespace InvoiceManagementApi.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class BaseController : ControllerBase
    {
        protected void SetUserClaims(UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim(ClaimTypes.Role, user.RoleName ?? string.Empty),
                new Claim("DisplayName", user.DisplayName ?? string.Empty),
                new Claim("CompanyName", user.CompanyName ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, "Custom");
            var principal = new ClaimsPrincipal(identity);

            HttpContext.User = principal;
        }

        protected int? CurrentUserId
        {
            get
            {
                if (User?.Identity?.IsAuthenticated != true) return null;
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out var id)) return id;
                return null;
            }
        }

        protected string GetRoutePath()
        {
            var path = HttpContext?.Request?.Path.Value ?? string.Empty;
            return path.TrimStart('/');
        }
    }
}
