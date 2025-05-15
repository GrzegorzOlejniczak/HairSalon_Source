using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HairSalonApp.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HairSalonApp.Controllers
{

    public class AboutController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AboutController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var hairdressers = await _userManager.GetUsersInRoleAsync("Hairdresser");

            var hairdressersList = hairdressers.Cast<ApplicationUser>().ToList();

            return View(hairdressersList);
        }
    }
}