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
            ViewData["Balance"] = user.Balance;

        }
        else
        {
            ViewData["Username"] = "User"; 
            ViewData["Vaults"] = new List<Vault>(); // Fallback if user not found
            ViewData["Balance"] = 0; // Default balance
        }

            var activities = new List<ActivityModel>
        {
            new ActivityModel { Description = "Payment for Groceries", IsPaid = true },
            new ActivityModel { Description = "Rent Payment to Landlord", IsPaid = true },
            new ActivityModel { Description = "Utility Bill Payment", IsPaid = false },
            new ActivityModel { Description = "Subscription for Streaming Service", IsPaid = true },
            new ActivityModel { Description = "Payment for Groceries", IsPaid = false }
        };

        // Pass the activities list to the View
        ViewData["Activities"] = activities;

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
    var transactions = new List<TransactionModel>
    {
        new TransactionModel { Description = "Groceries", Amount = -100, IsNegative = true, ImageUrl = "https://media.istockphoto.com/id/1399395748/photo/cheerful-business-woman-with-glasses-posing-with-her-hands-under-her-face-showing-her-smile.jpg?s=612x612&w=0&k=20&c=EbnuxLE-RJP9a08h2zjfgKUSFqmjGubk0p6zwJHnbrI="},
        new TransactionModel { Description = "Electric Bill", Amount = -30, IsNegative = true, ImageUrl = "https://media.istockphoto.com/id/1171319990/photo/beautiful-girl-taking-photos-with-retro-camera.jpg?s=612x612&w=0&k=20&c=vgt1M5BsYn5SEhBORdRASif1Yeq5yM6-0x55YRU4qrQ="},
        new TransactionModel { Description = "Money for Electric Bill", Amount = 40, IsNegative = false, ImageUrl = "https://media.istockphoto.com/id/1171319990/photo/beautiful-girl-taking-photos-with-retro-camera.jpg?s=612x612&w=0&k=20&c=vgt1M5BsYn5SEhBORdRASif1Yeq5yM6-0x55YRU4qrQ=" },
        new TransactionModel { Description = "Deposit for Rent", Amount = 750, IsNegative = false, ImageUrl = "https://static.vecteezy.com/system/resources/thumbnails/049/209/831/small_2x/young-woman-smiling-with-natural-beauty-against-a-plain-background-in-a-bright-and-cheerful-setting-png.png" },
        new TransactionModel { Description = "Internet Bill", Amount = -75, IsNegative = true, ImageUrl = "https://static.vecteezy.com/system/resources/thumbnails/049/209/831/small_2x/young-woman-smiling-with-natural-beauty-against-a-plain-background-in-a-bright-and-cheerful-setting-png.png" },
        new TransactionModel { Description = "Groceries", Amount = -90, IsNegative = true,ImageUrl = "https://media.istockphoto.com/id/1399395748/photo/cheerful-business-woman-with-glasses-posing-with-her-hands-under-her-face-showing-her-smile.jpg?s=612x612&w=0&k=20&c=EbnuxLE-RJP9a08h2zjfgKUSFqmjGubk0p6zwJHnbrI=" },
        new TransactionModel { Description = "Deposit for Rent", Amount = 300, IsNegative = false, ImageUrl = "https://media.istockphoto.com/id/1171319990/photo/beautiful-girl-taking-photos-with-retro-camera.jpg?s=612x612&w=0&k=20&c=vgt1M5BsYn5SEhBORdRASif1Yeq5yM6-0x55YRU4qrQ=" },
        new TransactionModel { Description = "Deposit for Groceries", Amount = 70, IsNegative = false, ImageUrl = "https://static.vecteezy.com/system/resources/thumbnails/049/209/831/small_2x/young-woman-smiling-with-natural-beauty-against-a-plain-background-in-a-bright-and-cheerful-setting-png.png" },
        new TransactionModel { Description = "Deposit for Internet Bill", Amount = 50, IsNegative = false, ImageUrl = "https://st.depositphotos.com/2024219/52075/i/450/depositphotos_520758138-stock-photo-african-american-handsome-man-isolated.jpg" },
        new TransactionModel { Description = "Paper Towels and Hand Soap", Amount = -15, IsNegative = true, ImageUrl = "https://media.istockphoto.com/id/1399395748/photo/cheerful-business-woman-with-glasses-posing-with-her-hands-under-her-face-showing-her-smile.jpg?s=612x612&w=0&k=20&c=EbnuxLE-RJP9a08h2zjfgKUSFqmjGubk0p6zwJHnbrI=" },
        new TransactionModel { Description = "Deposit for Rent", Amount = 600, IsNegative = false, ImageUrl = "https://st.depositphotos.com/2024219/52075/i/450/depositphotos_520758138-stock-photo-african-american-handsome-man-isolated.jpg" },
    };
    return View(transactions); 
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
        await HttpContext.SignOutAsync(); 
        return RedirectToAction("Login"); 
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Console.WriteLine("Error action triggered.");
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
