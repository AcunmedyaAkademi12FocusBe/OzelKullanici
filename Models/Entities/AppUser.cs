using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace OzelKullanici.Models.Entities;

public class AppUser : IdentityUser
{
    [MaxLength(100)]
    public string FirstName { get; set; }
    [MaxLength(100)]
    public string LastName { get; set; }
}