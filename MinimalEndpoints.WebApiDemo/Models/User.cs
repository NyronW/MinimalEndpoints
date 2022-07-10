using System.Security.Claims;

namespace MinimalEndpoints.WebApiDemo.Models;

public record User(string UserName,string Password, Claim[] Claims);