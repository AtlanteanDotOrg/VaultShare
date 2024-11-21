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

        // Redirect with a query parameter for the success message
        return RedirectToAction("Login", "Home", new { message = "Your account has been successfully created. Please log in." });
    }

    // Hash the password using BCrypt
    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private string GetIdFromSession()
    {
        var googleId = HttpContext.Session.GetString("GoogleId");
        var id = HttpContext.Session.GetString("Id");

        // Prefer GoogleId if available, otherwise fallback to Id
        if (!string.IsNullOrEmpty(googleId))
        {
            return googleId;
        }
        else if (!string.IsNullOrEmpty(id))
        {
            return id;
        }
        else
        {
            // Handle the case where both IDs are missing, depending on your application flow
            return null;
        }
    }

    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] ProfileUpdateRequest request)
    {
        // Get the user identifier from session (either GoogleId or Id)
        var userId = GetIdFromSession();

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User is not authenticated.");
        }

        // Attempt to get the user based on GoogleId or Id
        User user = null;
        if (userId == HttpContext.Session.GetString("GoogleId")) // If the user is logged in via Google
        {
            user = await _userService.GetUserByGoogleIdAsync(userId);
        }
        else // If the user is logged in via local login (Id)
        {
            user = await _userService.GetUserByIdAsync(userId);
        }

        // If no user is found, return a bad request
        if (user == null)
        {
            return BadRequest("User not found.");
        }

        // Update the username
        user.Name = request.Value;

        // Update the user in the database
        await _userService.UpdateUserAsync(user);

        return Ok("Username updated successfully.");
    }


    [HttpPut("bio")]
    public async Task<IActionResult> UpdateBio([FromBody] ProfileUpdateRequest request)
    {
        // Get the user identifier from session (either GoogleId or Id)
        var userId = GetIdFromSession();

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User is not authenticated.");
        }

        // Attempt to get the user based on GoogleId or Id
        User user = null;
        if (userId == HttpContext.Session.GetString("GoogleId")) // If the user is logged in via Google
        {
            user = await _userService.GetUserByGoogleIdAsync(userId);
        }
        else // If the user is logged in via local login (Id)
        {
            user = await _userService.GetUserByIdAsync(userId);
        }

        if (user == null)
            return BadRequest("User not found.");

        user.Bio = request.Value;
        await _userService.UpdateUserAsync(user);
        return Ok();
    }

    [HttpPost("push-notifications")]
    public async Task<IActionResult> UpdatePushNotifications([FromBody] NotificationRequest request)
    {
        var googleId = GetIdFromSession();
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
            var googleId = GetIdFromSession();
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
        try
        {
            // Get the user identifier (either GoogleId or Id) from the session
            var userId = GetIdFromSession();

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User is not authenticated.");
            }

            // Attempt to find the user
            User user = null;
            if (userId == HttpContext.Session.GetString("GoogleId")) // If it's a Google login
            {
                user = await _userService.GetUserByGoogleIdAsync(userId);
            }
            else // Non-Google user
            {
                user = await _userService.GetUserByIdAsync(userId);
            }

            if (user == null)
            {
                return BadRequest("No user found with the provided ID.");
            }

            // Hash the password before saving (important for security)
            user.Password = HashPassword(request.NewPassword);

            // Update the user's password
            await _userService.UpdateUserAsync(user);

            return Ok("Password reset successfully.");
        }
        catch (Exception ex)
        {
            // Log the error and return a server error message
            Console.WriteLine($"Error resetting password: {ex.Message}");
            return StatusCode(500, "Failed to reset password.");
        }
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
