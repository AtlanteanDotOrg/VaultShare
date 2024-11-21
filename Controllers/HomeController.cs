using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Google.Authenticator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Security.Cryptography;

namespace VaultShare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserService _userService;
    static byte [] entropy = {5, 6, 1, 3, 9, 8, 4};

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
        HttpContext.Session.SetString("LoginSuccess", "true");
        return Challenge(new AuthenticationProperties { RedirectUri = "/Home/Dashboard" }, GoogleDefaults.AuthenticationScheme);
    }

    // Login page view
    public IActionResult Login()
    {
        Console.WriteLine("Login action triggered.");
        HttpContext.Session.SetString("UserName", "NA");
        HttpContext.Session.SetString("IsValidTwoFactorAuthentication", "invalid");
        return View("login"); // Corresponds to login.cshtml
    }

    [HttpPost] // Action to initiate No Oauth login
    public async Task<IActionResult> NoOauthLogin(string email, string password)
    {
        Console.WriteLine("NoOauthLogin action triggered.");
        bool status = false;
        var usernameCheck = HttpContext.Session.GetString("UserName");
        var authCheck = HttpContext.Session.GetString("IsValidTwoFactorAuthentication");

        // Fetch the user from the database
        var user = await _userService.GetUserByEmailAsync(email);

        // Check if the user exists and if the password matches
        if(usernameCheck.Equals("NA") || authCheck.Equals("invalid"))
        {
            //generate authentication key
            string UserUniqueKey = (user.Username.ToString() + "2nD07gL1");

            // Check if the user exists and if the password matches
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                HttpContext.Session.SetString("UserName", user.Username.ToString());

                //Two Factor Authentication Setup
                TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
                var setupInfo = TwoFacAuth.GenerateSetupCode("VaultShare", user.Username, ConvertSecretToBytes(UserUniqueKey), 300);
                HttpContext.Session.SetString("UserUniqueKey", UserUniqueKey);
                ViewBag.BarcodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                ViewBag.SetupCode = setupInfo.ManualEntryKey;
                status = true;

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
            }
        }
        else
        {
            return RedirectToAction("Dashboard");
        }

        // If login failed, show an error message
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        ViewBag.Status = status;
        
        // If login failed, show an error message
        TempData["LoginError"] = "Invalid email or password. Please try again.";
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

        // Check for login success
        if (HttpContext.Session.GetString("LoginSuccess") == "true")
        {
            ViewData["ShowSuccessAlert"] = true;
            HttpContext.Session.Remove("LoginSuccess"); // Clear after use
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
        var googleId = HttpContext.Session.GetString("GoogleId");
        ViewData["GoogleId"] = googleId;
            var userId = HttpContext.Session.GetString("Id");
    ViewData["Id"] = googleId;
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

    // Fetch contributions for the selected vault
    var contributions = new List<Contribution>
    {
        new Contribution { Description = "Money for Electric", Amount = 40.00 },
        new Contribution { Description = "Deposit for Rent", Amount = 750.00 },
        new Contribution { Description = "Deposit for Rent", Amount = 300.00 },
        new Contribution { Description = "Money for Groceries", Amount = 70.00},
        new Contribution { Description = "Deposit for Internet Bill", Amount = 50.00},
        new Contribution { Description = "Deposit for Rent", Amount = 600.00},
    };

    var transactions = new List<Transaction>
    {
        new Transaction {Description = "Electric Bill", Amount = -30.00},
        new Transaction {Description = "Internet Bill", Amount = -75.00},
        new Transaction {Description = "Groceries", Amount = -90.00},
        new Transaction {Description = "Paper Towels and Hand Soap", Amount = -15.00}
    };

    ViewData["SelectedVault"] = selectedVault;
    ViewData["Contributions"] = contributions;
    ViewData["Transaction"] = transactions;

    return View("vault");
}

    //     if (string.IsNullOrEmpty(vaultId))
    //     {
    //         _logger.LogWarning("Vault ID is null or empty.");
    //         return RedirectToAction("Dashboard");
    //     }

    //     if (!SetUserIdInViewData())
    //     {
    //         return RedirectToAction("Login");
    //     }

    //     var googleId = ViewData["GoogleId"]?.ToString();
    //     var id = ViewData["Id"]?.ToString();

    //     if (!string.IsNullOrEmpty(googleId))
    //     {
    //         var user = _userService.GetUserByGoogleIdAsync(googleId).Result;

    //         if (user == null)
    //         {
    //             _logger.LogWarning($"User with GoogleId: {googleId} not found.");
    //             return RedirectToAction("Dashboard");
    //         }

    //         var userVaults = user.Vaults;
    //         if (userVaults == null || !userVaults.Any())
    //         {
    //             _logger.LogWarning($"No vaults found for user with GoogleId: {googleId}");
    //             return RedirectToAction("Dashboard");
    //         }

    //         var selectedVault = userVaults.FirstOrDefault(v => v.VaultId == vaultId);

    //         if (selectedVault == null)
    //         {
    //             _logger.LogWarning($"Vault with Id: {vaultId} not found for user with GoogleId: {googleId}");
    //             return RedirectToAction("Dashboard");
    //         }

    //         ViewData["SelectedVault"] = selectedVault;
    //         return View("vault");
    //     }
    //     else
    //     {
    //         if (string.IsNullOrEmpty(id))
    //         {
    //             _logger.LogWarning("User ID is null or empty in ViewData.");
    //             return RedirectToAction("Dashboard");
    //         }

    //         var user = _userService.GetUserByIdAsync(id).Result;

    //         if (user == null)
    //         {
    //             _logger.LogWarning($"User with Id: {id} not found.");
    //             return RedirectToAction("Dashboard");
    //         }

    //         var userVaults = user.Vaults;
    //         if (userVaults == null || !userVaults.Any())
    //         {
    //             _logger.LogWarning($"No vaults found for user with Id: {id}");
    //             return RedirectToAction("Dashboard");
    //         }

    //         var selectedVault = userVaults.FirstOrDefault(v => v.VaultId == vaultId);

    //         if (selectedVault == null)
    //         {
    //             _logger.LogWarning($"Vault with Id: {vaultId} not found for user with Id: {id}");
    //             return RedirectToAction("Dashboard");
    //         }

    //         ViewData["SelectedVault"] = selectedVault;
    //         return View("vault");
    //     }
    // }



    public IActionResult Friends()
    {
        var googleId = HttpContext.Session.GetString("GoogleId");
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
    // log out function 
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(); 
        HttpContext.Session.SetString("UserName", "NA");
        HttpContext.Session.SetString("IsValidTwoFactorAuthentication", "invalid");
        TempData["LogoutMessage"] = "Successfully logged out!";

        return RedirectToAction("Login"); 
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Console.WriteLine("Error action triggered.");
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    private static byte[] ConvertSecretToBytes(string secret)
    {
        return Encoding.UTF8.GetBytes(secret);
    }

    public ActionResult TwoFactorAuthenticate()
    {
     var token = HttpContext.Request.Form["CodeDigit"];
     TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
     string UserUniqueKey = HttpContext.Session.GetString("UserUniqueKey");
     bool isValid = TwoFacAuth.ValidateTwoFactorPIN(UserUniqueKey, token, false);
     if (isValid)
     {
         string UserCode = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(UserUniqueKey), entropy, DataProtectionScope.CurrentUser));

         HttpContext.Session.SetString("IsValidTwoFactorAuthentication", "valid");
         return RedirectToAction("Dashboard");
      }

      ViewBag.Message = "Google Two Factor PIN is expired or wrong";
      return RedirectToAction("Login");
    }
}
