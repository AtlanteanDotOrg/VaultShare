using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;

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
}
public class ResetPasswordRequest { public string NewPassword { get; set; } }
