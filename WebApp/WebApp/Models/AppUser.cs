using Microsoft.AspNetCore.Identity;

namespace WebApp.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
