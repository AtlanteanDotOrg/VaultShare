using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

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

    // Helper method to get GoogleId from session and set it in ViewData
    private bool SetGoogleIdInViewData()
    {
        var googleId = HttpContext.Session.GetString("GoogleId");
        if (string.IsNullOrEmpty(googleId))
        {
            Console.WriteLine("Google ID is null in session, redirecting to login.");
            return false;
        }

        Console.WriteLine("Google ID retrieved from session: " + googleId);
        ViewData["GoogleId"] = googleId;
        return true;
    }

    // Action to initiate Google login
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

    // Dashboard view after successful Google login
public IActionResult Dashboard()
{
    if (!SetGoogleIdInViewData())
    {
        return RedirectToAction("Login");
    }

    var googleId = HttpContext.Session.GetString("GoogleId");
    var user = _userService.GetUserByGoogleIdAsync(googleId).Result;

    if (user != null)
    {
        ViewData["Username"] = user.Name;
        ViewData["Vaults"] = user.Vaults;
    }
    else
    {
        ViewData["Username"] = "User"; 
        ViewData["Vaults"] = new List<Vault>(); // Fallback
    }

    return View("dashboard");
}



    public IActionResult DefaultSend()
    {
        Console.WriteLine("DefaultSend action triggered.");
        if (!SetGoogleIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    public IActionResult VaultSendAsync()
    {
        if (!SetGoogleIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View("vaultSend");
    }

public IActionResult Vault(string vaultId)
{
    if (!SetGoogleIdInViewData())
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
        if (!SetGoogleIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

public IActionResult Settings()
{
    Console.WriteLine("Settings action triggered.");

    var googleId = HttpContext.Session.GetString("GoogleId");
    if (string.IsNullOrEmpty(googleId))
    {
        Console.WriteLine("Google ID is null in session, redirecting to login.");
        return RedirectToAction("Login");
    }

    var user = _userService.GetUserByGoogleIdAsync(googleId).Result;

    if (user != null)
    {
        ViewData["Username"] = user.Name;
        ViewData["Bio"] = user.Bio;
        ViewData["CardNumber"] = user.CardNumber;
        ViewData["CardExpiry"] = user.CardExpiry;
        ViewData["CardCvc"] = user.CardCvc;
        ViewData["CardNickname"] = user.CardNickname;
    }

    return View("settings");
}


    public IActionResult Transactions()
    {
        if (!SetGoogleIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View("transactions"); // Corresponds to transactions.cshtml
    }

    public IActionResult Privacy()
    {
        Console.WriteLine("Privacy action triggered.");
        if (!SetGoogleIdInViewData())
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Console.WriteLine("Error action triggered.");
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
