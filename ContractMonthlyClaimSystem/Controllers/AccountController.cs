using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login - FIXED: Removed naming conflict
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && user.PasswordHash == HashPassword(model.Password))
                {
                    // FIXED: Using System.Security.Claims.Claim directly
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new System.Security.Claims.Claim(ClaimTypes.Name, user.Name),
                        new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                        new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(60)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid username or password.");
            }

            return View(model);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username already exists
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                    if (existingUser != null)
                    {
                        if (existingUser.Username == model.Username)
                        {
                            ModelState.AddModelError("Username", "Username already exists.");
                        }
                        if (existingUser.Email == model.Email)
                        {
                            ModelState.AddModelError("Email", "Email already exists.");
                        }
                        return View(model);
                    }

                    // Create new user with all required fields
                    var user = new User
                    {
                        Username = model.Username,
                        Name = model.Name,
                        Email = model.Email,
                        PasswordHash = HashPassword(model.Password),
                        Role = UserRole.Lecturer,
                        Department = model.Department ?? "General",
                        CreatedDate = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Registration failed: {ex.Message}");
                }
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}