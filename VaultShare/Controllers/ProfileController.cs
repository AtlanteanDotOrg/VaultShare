using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;

[Controller]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly UserService _userService;

    public ProfileController(UserService userService)
    {
        _userService = userService;
    }

    // Register a new user without Google
    [HttpPost("createaccount")]
    public async Task<ActionResult> CreateAccount([FromForm] User user)
    {
        if (user == null || string.IsNullOrEmpty(user.Email) ||
            string.IsNullOrEmpty(user.Password) ||
            string.IsNullOrEmpty(user.Username)) // Check for Username here
        {
            return BadRequest("Invalid user data.");
        }

        // Hash the password before storing
        user.Password = HashPassword(user.Password);

        var existingUser = await _userService.GetUserByEmailAsync(user.Email);
        if (existingUser != null)
        {
            return Conflict("User already exists.");
        }

        await _userService.CreateUserAsync(user); // Use UserService to create user

        // Redirect to the Login action in the HomeController after successful account creation
        return RedirectToAction("Login", "Home");
    }

    // Hash the password using BCrypt
    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private string GetGoogleIdFromSession()
    {
        var googleId = HttpContext.Session.GetString("GoogleId");
        
        // Fallback if Google ID is missing in session
        if (string.IsNullOrEmpty(googleId))
        {
            Console.WriteLine("Google ID is missing from session. Using fallback Google ID.");
            googleId = "test_google_id"; // Replace with a default or test Google ID for debugging
        }

        return googleId;
    }

    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] ProfileUpdateRequest request)
    {
        var googleId = GetGoogleIdFromSession();
        var user = await _userService.GetUserByGoogleIdAsync(googleId);
        
        if (user == null)
            return BadRequest("User not found.");

        user.Name = request.Value;
        await _userService.UpdateUserAsync(user);
        return Ok();
    }

    [HttpPut("bio")]
    public async Task<IActionResult> UpdateBio([FromBody] ProfileUpdateRequest request)
    {
        var googleId = GetGoogleIdFromSession();
        var user = await _userService.GetUserByGoogleIdAsync(googleId);
        
        if (user == null)
            return BadRequest("User not found.");

        user.Bio = request.Value;
        await _userService.UpdateUserAsync(user);
        return Ok();
    }

    [HttpPost("push-notifications")]
    public async Task<IActionResult> UpdatePushNotifications([FromBody] NotificationRequest request)
    {
        var googleId = GetGoogleIdFromSession();
        var user = await _userService.GetUserByGoogleIdAsync(googleId);

        if (user == null)
            return BadRequest("User not found.");

        user.NotificationsEnabled = request.Enable;
        await _userService.UpdateUserAsync(user);
        return Ok();
    }

    [HttpPost("payment-method")]
    public async Task<IActionResult> UpdatePaymentMethod([FromBody] PaymentMethodRequest request)
    {
        try
        {
            var googleId = GetGoogleIdFromSession();
            var user = await _userService.GetUserByGoogleIdAsync(googleId);

            if (user == null)
                return BadRequest("User not found.");

            user.CardNumber = request.CardNumber;
            user.CardExpiry = request.Expiry;
            user.CardCvc = request.Cvv;
            user.CardNickname = request.Name;
            user.RoutingNumber = request.RoutingNumber;
            user.AccountNumber = request.AccountNumber;
            user.MerchantID = request.MerchantID;

            await _userService.UpdateUserAsync(user);
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating payment method: {ex.Message}");
            return StatusCode(500, "Failed to update payment method.");
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var googleId = GetGoogleIdFromSession();
        var user = await _userService.GetUserByGoogleIdAsync(googleId);

        if (user == null)
            return BadRequest("User not found.");

        user.Password = request.NewPassword;
        await _userService.UpdateUserAsync(user);
        return Ok();
    }
    
}


public class ProfileUpdateRequest { public string Value { get; set; } }
public class NotificationRequest { public bool Enable { get; set; } }
public class PaymentMethodRequest 
{ 
    public string Name { get; set; }
    public string Expiry { get; set; }
    public string Cvv { get; set; }
    public string CardNumber { get; set; }
    public string AccountNumber { get; set; }  // New field
    public string RoutingNumber { get; set; }  // New field
    public string MerchantID { get; set; }     // New field
}
public class ResetPasswordRequest { public string NewPassword { get; set; } }
