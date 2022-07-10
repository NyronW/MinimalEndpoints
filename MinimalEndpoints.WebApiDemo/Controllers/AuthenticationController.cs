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
    [HttpPost, Route("login")]
    public IActionResult Login(LoginModel loginDTO)
    {
        try
        {
            if (string.IsNullOrEmpty(loginDTO.UserName) ||
            string.IsNullOrEmpty(loginDTO.Password))
                return BadRequest("Username and/or Password not specified");

            if (loginDTO.UserName.Equals("demo") &&
                    loginDTO.Password.Equals("secret123"))
            {
                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("thisisasecretkey@123"));
                var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
                var jwtSecurityToken = new JwtSecurityToken(
                    issuer: "ABCXYZ",
                    audience: "http://localhost:7024",
                    claims: new List<Claim>(),
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: signinCredentials
                );

                return Ok(new JwtSecurityTokenHandler()
                       .WriteToken(jwtSecurityToken));
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
