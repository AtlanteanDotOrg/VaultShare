using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;

namespace VaultShare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Login()
    {
        return View("login"); // Corresponds to login.cshtml
    }

    public IActionResult Dashboard()
    {
        return View("dashboard"); // Corresponds to dashboard.cshtml
    }

    public IActionResult DefaultSend()
    {
        return View();
    }

    public IActionResult VaultSend()
    {
        return View("vaultSend"); // Corresponds to vaultSend.cshtml
    }

    public IActionResult Vault()
    {
        return View("vault"); // Corresponds to vault.cshtml
    }

    public IActionResult Friends()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View("settings"); // Corresponds to settings.cshtml
    }

    public IActionResult Transactions()
    {
        return View("transactions"); // Corresponds to transactions.cshtml
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
