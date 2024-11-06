using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;
using System.Collections.Generic; // For IEnumerable
using System.Security.Claims; // For Claims
using System.Threading.Tasks;     // For Task
using BCrypt.Net; // Add this for BCrypt
using MongoDB.Bson;


namespace VaultShare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserService _userService;


    public HomeController(ILogger<HomeController> logger, UserService userService)
    {
        _logger = logger;
        _userService = userService;

        Console.WriteLine("HomeController instantiated.");
    }

    // Action to initiate Google login
    public IActionResult GoogleLogin()
    {
        Console.WriteLine("GoogleLogin action triggered.");
        // Trigger Google authentication and redirect to Dashboard after successful login
        return Challenge(new AuthenticationProperties { RedirectUri = "/Home/Dashboard" }, GoogleDefaults.AuthenticationScheme);
    }

    // Login page view
    public IActionResult Login()
    {
        Console.WriteLine("Login action triggered.");
        return View("login"); // Corresponds to login.cshtml
    }

    [HttpPost]  // logs
    public async Task<IActionResult> Login(string email, string password)
    {
        // Use _userService instead of _users
        var user = await _userService.GetUserByEmailAsync(email);

        // Check if user exists and if password matches
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            // Create the identity and sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Dashboard");
        }

        // If login failed
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View();
    }

    public IActionResult Register()
    {
        return View("register"); // Corresponds to register.cshtml
    }

    // Dashboard view after successful Google login
    public async Task<IActionResult> Dashboard()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login");
        }

        // Get the current user from the email
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        // Get all users to reference when looking for friends
        var allUsers = await _userService.GetAllUsersAsync();

        // Extract all the vault members' IDs from the user's vaults
        var vaultMembers = user.Vaults.SelectMany(v => v.Members).ToList();

        // Find all friends who are also members of any vaults
        var friendsInVaults = allUsers
            .Where(u => user.FriendIds.Contains(u.Id) && vaultMembers.Contains(u.Id))
            .ToList();

        // Create the ViewModel and populate it with data
        var viewModel = new DashboardViewModel
        {
            User = user,
            FriendsInVaults = friendsInVaults,
            Balance = user.Balance,
            Notifications = user.Notifications
        };

        return View(viewModel);
    }



    public IActionResult DefaultSend()
    {
        Console.WriteLine("DefaultSend action triggered.");
        return View();
    }

    public IActionResult VaultSendAsync()
    {
        // First, check if the user is logged in via OAuth (Google)
        var googleId = HttpContext.Session.GetString("GoogleId");
        Console.WriteLine("VaultSendAsync - Google ID from session: " + googleId);

        // If GoogleId is found in session, set it in ViewData and show the page
        if (!string.IsNullOrEmpty(googleId))
        {
            ViewData["GoogleId"] = googleId;
            return View("vaultSend"); // Google OAuth user
        }

        // If GoogleId is not found, check if the user is authenticated using cookies (regular sign-up)
        var userClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        if (userClaim != null)
        {
            // User is authenticated through regular sign-up
            ViewData["Username"] = userClaim.Value;
            return View("vaultSend"); // Regular user (not using OAuth)
        }

        // If no GoogleId or user is authenticated, redirect to the login page
        Console.WriteLine("User is not authenticated, redirecting to login.");
        return RedirectToAction("Login");
    }

    public IActionResult Vault()
    {
        Console.WriteLine("Vault action triggered.");
        return View("vault"); // Corresponds to vault.cshtml
    }

    public IActionResult Friends()
    {
        // First, check if the user is logged in via OAuth (Google)
        var googleId = HttpContext.Session.GetString("GoogleId");
        Console.WriteLine("Friends action triggered - Google ID from session: " + googleId);

        // If GoogleId is found in session, set it in ViewData and show the page
        if (!string.IsNullOrEmpty(googleId))
        {
            ViewData["GoogleId"] = googleId;
            return View(); // Regular Google OAuth user
        }

        // If GoogleId is not found, check if the user is authenticated using cookies (regular sign-up)
        var userClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        if (userClaim != null)
        {
            // User is authenticated through regular sign-up
            ViewData["Username"] = userClaim.Value;
            return View(); // Regular user (not using OAuth)
        }

        // If no GoogleId or user is authenticated, redirect to the login page
        Console.WriteLine("User is not authenticated, redirecting to login.");
        return RedirectToAction("Login");
    }

    public IActionResult Settings()
    {
        Console.WriteLine("Settings action triggered.");
        return View("settings"); // Corresponds to settings.cshtml
    }

    public IActionResult Transactions()
    {
        Console.WriteLine("Transactions action triggered.");
        return View("transactions"); // Corresponds to transactions.cshtml
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        // Sign out the current user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Redirect to the login page after logging out
        return RedirectToAction("Login");  // Assuming you have an AccountController with a Login action
    }

    public IActionResult Privacy()
    {
        Console.WriteLine("Privacy action triggered.");
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Console.WriteLine("Error action triggered.");
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}