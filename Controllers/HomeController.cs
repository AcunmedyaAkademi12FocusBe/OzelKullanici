using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OzelKullanici.Models.Entities;

namespace OzelKullanici.Controllers;

[ApiController]
public class HomeController (UserManager<AppUser> userManager): ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Ok("Merhaba");
        }
        
        var user = await userManager.FindByIdAsync(userId);
        return Ok($"merhaba {user.FirstName} {user.LastName}");
    }
}