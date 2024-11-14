
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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

    // Helper method to get GoogleId or User's Id from session and set it in ViewData
    private bool SetUserIdInViewData()
    {
        var googleId = HttpContext.Session.GetString("GoogleId");
        var id = HttpContext.Session.GetString("Id");

        if (!string.IsNullOrEmpty(googleId))
        {
            Console.WriteLine("Google ID retrieved from session: " + googleId);
            ViewData["GoogleId"] = googleId;
            return true;
        }
        else if (!string.IsNullOrEmpty(id))
        {
            Console.WriteLine("ID retrieved from session: " + id);
            ViewData["Id"] = id;
            return true;
        }

        Console.WriteLine("Both Google ID and ID are null in session, redirecting to login.");
        return false;
    }

    public IActionResult GoogleLogin()
    {
        Console.WriteLine("GoogleLogin action triggered.");
        return Challenge(new AuthenticationProperties { RedirectUri = "/Home/Dashboard" }, GoogleDefaults.AuthenticationScheme);
    }

    // Login page view
    public IActionResult Login()
    {
        Console.WriteLine("Login action triggered.");
        return View("login"); // Corresponds to login.cshtml
    }

    [HttpPost] // Action to initiate No Oauth login
    public async Task<IActionResult> NoOauthLogin(string email, string password)
    {
        Console.WriteLine("NoOauthLogin action triggered.");

        // Fetch the user from the database
        var user = await _userService.GetUserByEmailAsync(email);

        // Check if the user exists and if the password matches
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Id", user.Id.ToString()) // Optional: Custom claim for UserId
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user using cookies
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Set the user's ID in session
            HttpContext.Session.SetString("Id", user.Id.ToString());

            Console.WriteLine("User successfully logged in and ID stored in session.");
            return RedirectToAction("Dashboard");
        }

        // If login failed, show an error message
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View("login"); // Return to login view with error
    }


    public IActionResult Register()
    {
        return View();
    }

    public IActionResult ForgotPassword() {
        return View();
    }
    
    public IActionResult PasswordReset() {
        return View();
    }

    // Dashboard view after successful Google login
    public IActionResult Dashboard()
    {
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }

        // Retrieve GoogleId from session
        var googleId = HttpContext.Session.GetString("GoogleId");
        User user;

        if (!string.IsNullOrEmpty(googleId))
        {
            // If GoogleId exists, get the user by GoogleId
            user = _userService.GetUserByGoogleIdAsync(googleId).Result;
        }
        else
        {
            // If GoogleId is null, fall back to retrieving the user by regular Id
            var userId = HttpContext.Session.GetString("Id"); // Assuming UserId is stored in session
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login"); // Redirect if both IDs are missing
            }

            user = _userService.GetUserByIdAsync(userId).Result;
        }

        if (user != null)
        {
            ViewData["Username"] = user.Name;
            ViewData["Vaults"] = user.Vaults;
        }
        else
        {
            ViewData["Username"] = "User"; 
            ViewData["Vaults"] = new List<Vault>(); // Fallback if user not found
        }

        return View("dashboard");
    }

    public IActionResult DefaultSend()
    {
        Console.WriteLine("DefaultSend action triggered.");
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    public IActionResult VaultSendAsync()
    {
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View("vaultSend");
    }

    public IActionResult Vault(string vaultId)
    {
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }

        var googleId = ViewData["GoogleId"].ToString();
        var user = _userService.GetUserByGoogleIdAsync(googleId).Result;
        var userVaults = user?.Vaults;

        if (userVaults == null)
        {
            _logger.LogWarning($"No vaults found for user with GoogleId: {googleId}");
            return RedirectToAction("Dashboard");
        }

        var selectedVault = userVaults.FirstOrDefault(v => v.VaultId == vaultId);

        if (selectedVault == null)
        {
            _logger.LogWarning($"Vault with Id: {vaultId} not found for user with GoogleId: {googleId}");
            return RedirectToAction("Dashboard");
        }

        ViewData["SelectedVault"] = selectedVault;
        return View("vault");
    }

    public IActionResult Friends()
    {
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    public IActionResult Settings()
    {
        Console.WriteLine("Settings action triggered.");

        // Attempt to retrieve GoogleId or Id from session
        var googleId = HttpContext.Session.GetString("GoogleId");
        var id = HttpContext.Session.GetString("Id");

        User user = null;

        if (!string.IsNullOrEmpty(googleId))
        {
            // Try to fetch user by GoogleId
            Console.WriteLine("Google ID retrieved from session: " + googleId);
            user = _userService.GetUserByGoogleIdAsync(googleId).Result;
        }
        else if (!string.IsNullOrEmpty(id))
        {
            // Try to fetch user by regular Id
            Console.WriteLine("Regular ID retrieved from session: " + id);
            user = _userService.GetUserByIdAsync(id).Result;
        }

        if (user != null)
        {
            // Populate ViewData with user information if found
            ViewData["Username"] = user.Name;
            ViewData["Bio"] = user.Bio;
            ViewData["CardNumber"] = user.CardNumber;
            ViewData["CardExpiry"] = user.CardExpiry;
            ViewData["CardCvc"] = user.CardCvc;
            ViewData["CardNickname"] = user.CardNickname;
        }
        else
        {
            Console.WriteLine("No valid user found, redirecting to login.");
            return RedirectToAction("Login");
        }

        return View("settings");
    }



    public IActionResult Transactions()
    {
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View("transactions"); // Corresponds to transactions.cshtml
    }

    public IActionResult Privacy()
    {
        Console.WriteLine("Privacy action triggered.");
        if (!SetUserIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        // Sign the user out
        await HttpContext.SignOutAsync(); // Ensure this line is properly configured for sign-out
        return RedirectToAction("Login"); // Redirects to the homepage after logout
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Console.WriteLine("Error action triggered.");
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
