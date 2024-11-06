using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;


[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly UserService _userService;

    public ProfileController(UserService userService)
    {
        _userService = userService;
    }

    private string GetUserIdFromSession()
    {
        var userId = HttpContext.Session.GetString("userId");
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User is not logged in.");
        }
        return userId;
    }

    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] ProfileUpdateRequest request)
    {
        var userId = GetUserIdFromSession();
        await _userService.UpdateUsernameAsync(userId, request.Value);
        return Ok();
    }

    [HttpPut("bio")]
    public async Task<IActionResult> UpdateBio([FromBody] ProfileUpdateRequest request)
    {
        var userId = GetUserIdFromSession();
        await _userService.UpdateBioAsync(userId, request.Value);
        return Ok();
    }

    [HttpPost("push-notifications")]
    public IActionResult UpdatePushNotifications([FromBody] NotificationRequest request)
    {
        var userId = GetUserIdFromSession();
        _userService.UpdateNotifications(userId, request.Enable);
        return Ok();
    }

    [HttpPost("payment-method")]
    public async Task<IActionResult> UpdatePaymentMethod([FromBody] PaymentMethodRequest request)
    {
        var userId = GetUserIdFromSession();
        await _userService.UpdatePaymentMethodAsync(userId, request);
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var userId = GetUserIdFromSession();
        await _userService.ResetPasswordAsync(userId, request.NewPassword);
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
