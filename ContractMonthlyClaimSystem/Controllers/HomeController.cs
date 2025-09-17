using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Diagnostics;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Services;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;

        public HomeController(IClaimService claimService, IUserService userService)
        {
            _claimService = claimService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await GetCurrentUserAsync();
            var dashboard = await _claimService.GetDashboardDataAsync(currentUser.UserId, currentUser.Role);

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _userService.GetUserByIdAsync(userId);
        }
    }

}
