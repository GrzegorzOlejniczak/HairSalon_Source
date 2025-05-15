using Microsoft.AspNetCore.Identity;

namespace HairSalonApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string ProfilePicturePath { get; set; }
    }
}
