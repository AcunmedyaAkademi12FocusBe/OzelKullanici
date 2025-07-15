using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OzelKullanici.Models.DTOs.Register;
using OzelKullanici.Models.Entities;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace OzelKullanici.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration) : ControllerBase
{
    [HttpPost("[action]")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        var user = model.Adapt<AppUser>();
        user.UserName = model.Email;
        
        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        
        // burada token oluşturup
        // e-posta ile üyelik onay token'ı göndereceğim
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth", new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);
        
        Console.WriteLine($"\n onay linki: {confirmationLink} \n");
        
        return Ok("Kullanıcını oluşturdum");
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        
        return Ok("Onaylandı");
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized("bu kullanıcı yok");
        }
        
        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized("hatalı şifre");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim("uid", user.Id),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds
        );
        
        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiration = token.ValidTo
        });
    }
    
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetLoggedInUser()
    {
        return Ok($"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}");
    }
}

//  eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJuaWhhdGR5QGdtYWlsLmNvbSIsInVpZCI6IjJiYTA5NDg2LWIwMGItNDJlZi05MmJiLTg5ZDE0NjU0YTRmMSIsImZpcnN0bmFtZSI6Ik5paGF0IiwibGFzdG5hbWUiOiJEdXlzYWsiLCJleHAiOjE3NTI0ODkyMDAsImlzcyI6Ik96ZWxLdWxsYW5pY2kiLCJhdWQiOiJPemVsS3VsbGFuaWNpVXNlcnMifQ.dGdTx3tRYP2IgMO1qSzK8UUk7rhJISPz748CXS_mLqE
