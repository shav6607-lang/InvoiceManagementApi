using Microsoft.AspNetCore.Mvc;
using InvoiceManagementApi.Models;
using InvoiceManagementApi.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace InvoiceManagementApi.Controllers
{
    using Microsoft.AspNetCore.Authorization;

    [Route("api/[controller]")]
    public class LoginController : BaseController
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        public LoginController(IAuthRepository authRepository, ILogger<LoginController> logger, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _logger = logger;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] AuthenticateModel model)
        {
            if (model == null ||string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(CreateAuthResponse("Username and password are required."));
            }

            var result = await _authRepository.AuthenticateAsync(model.Username,model.Password);

            if (result?.IsSuccess != true || result.User == null)
            {
                var message = string.IsNullOrWhiteSpace(result?.Message)? "Invalid username or password.": result.Message;
                _logger.LogWarning("Authentication failed for user {Username}: {Message}",model.Username,message);

               return Unauthorized(CreateAuthResponse(message));
            }

            SetUserClaims(result.User);

            var defaultCompany = result.Companies?.FirstOrDefault();

            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                _logger.LogWarning("Jwt:Key is not configured.");

                return Ok(new
                {
                    Message = result.Message ?? "Success",
                    result.User.UserId,
                    result.User.UserName,
                    result.User.RoleId,
                    result.User.RoleName,
                    result.User.DisplayName,
                    Company = defaultCompany == null ? null : new
                    {
                        defaultCompany.CompanyId,
                        defaultCompany.CompanyName,
                        defaultCompany.GSTNO,
                        defaultCompany.State,
                        defaultCompany.StateCode,
                        defaultCompany.EMail,
                        defaultCompany.Phone
                    }
                });
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                new(ClaimTypes.Name, result.User.UserName ?? string.Empty),
                new("RoleId", result.User.RoleId.ToString()),
                new(ClaimTypes.Role, result.User.RoleName ?? string.Empty),
                new("DisplayName", result.User.DisplayName ?? string.Empty)
            };

            if (defaultCompany != null)
            {
                claims.Add(new Claim("CompanyId",defaultCompany.CompanyId.ToString()));
                claims.Add(new Claim("CompanyName",defaultCompany.CompanyName ?? string.Empty));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),SecurityAlgorithms.HmacSha512)
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(securityToken);

            return Ok(new
            {
                Message = result.Message ?? "Success",
                access_token = token,
                token_type = "Bearer",
                expires_in = tokenDescriptor.Expires,
                status_message = "Ok",
                userDetails = result.User,
                menus = Array.Empty<object>()
            });
        }

        private static object CreateAuthResponse(string message)
        {
            return new
            {
                Message = message,
                access_token = string.Empty,
                token_type = string.Empty,
                expires_in = string.Empty,
                status_message = string.Empty,
                userDetails = Array.Empty<object>(),
                menus = Array.Empty<object>()
            };
        }
    }
}