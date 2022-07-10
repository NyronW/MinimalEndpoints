using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MinimalEndpoints.WebApiDemo.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MinimalEndpoints.WebApiDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;

    Dictionary<string, User> Users = new Dictionary<string, User>
    {
        { "admin", new User("admin","secret123", new[] { new Claim("todo:read-write","true") }) },
        { "demo", new User("demo","secret123", new[] { new Claim("todo:read","true") }) }
    };


    public AuthenticationController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost, Route("")]
    public IActionResult Login(LoginModel loginDTO)
    {
        try
        {
            if (string.IsNullOrEmpty(loginDTO.UserName) ||
                     string.IsNullOrEmpty(loginDTO.Password))
                return BadRequest("Username and/or Password not specified");

            var user = Users[loginDTO.UserName.ToLower()];

            if (user.Password.Equals(loginDTO.Password))
            {
                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AuthZ:SecretKey"]));
                var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
                var jwtSecurityToken = new JwtSecurityToken(
                    issuer: _configuration["AuthZ:Issuer"],
                    audience: _configuration["AuthZ:Audience"],
                    claims: user.Claims,
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: signinCredentials
                );

                var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

                return Ok(token);
            }
        }
        catch
        {
            return BadRequest
            ("An error occurred in generating the token");
        }

        return Unauthorized();
    }
}
