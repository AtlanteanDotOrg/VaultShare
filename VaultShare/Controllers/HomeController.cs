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

    // Dashboard view after successful Google login
    public IActionResult Dashboard()
    {
        Console.WriteLine("Dashboard action triggered.");
        return View("dashboard"); // Corresponds to dashboard.cshtml
    }

    public IActionResult DefaultSend()
    {
        Console.WriteLine("DefaultSend action triggered.");
        return View();
    }

public IActionResult VaultSendAsync()
{
    var googleId = HttpContext.Session.GetString("GoogleId");
    Console.WriteLine("VaultSendAsync - Google ID from session: " + googleId);

    if (string.IsNullOrEmpty(googleId))
    {
        Console.WriteLine("Google ID is null in session, redirecting to login.");
        return RedirectToAction("Login");
    }

    ViewData["GoogleId"] = googleId;
    return View("vaultSend");
}

    public IActionResult Vault()
    {
        Console.WriteLine("Vault action triggered.");
        return View("vault"); // Corresponds to vault.cshtml
    }

public IActionResult Friends()
{
    var googleId = HttpContext.Session.GetString("GoogleId");
    Console.WriteLine("Friends action triggered - Google ID from session: " + googleId);

    if (string.IsNullOrEmpty(googleId))
    {
        Console.WriteLine("Google ID is null in session, redirecting to login.");
        return RedirectToAction("Login");
    }

    ViewData["GoogleId"] = googleId;
    return View();
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
