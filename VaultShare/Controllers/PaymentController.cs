using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly UserService _userService;
    private readonly USAuthService _authService;

    public PaymentController(UserService userService, USAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpGet("friends/{googleId}")]
    public async Task<IActionResult> GetFriends(string googleId)
    {
        Console.WriteLine($"Received request to retrieve friends for GoogleId: {googleId}");
        var user = await _userService.GetUserByGoogleIdAsync(googleId);
        if (user == null)
        {
            Console.WriteLine($"User with GoogleId {googleId} not found");
            return NotFound("User not found");
        }

        var friends = await _userService.GetFriendsAsync(user.Id);
        Console.WriteLine($"Returning {friends.Count} friends for GoogleId: {googleId}");
        return Ok(friends);
    }

    [HttpGet("vaults/{googleId}")]
    public async Task<IActionResult> GetVaults(string googleId)
    {
        Console.WriteLine($"Received request to retrieve vaults for GoogleId: {googleId}");
        var user = await _userService.GetUserByGoogleIdAsync(googleId);
        if (user == null)
        {
            Console.WriteLine($"User with GoogleId {googleId} not found");
            return NotFound("User not found");
        }

        var vaults = await _userService.GetUserVaultsAsync(user.Id);
        Console.WriteLine($"Returning {vaults.Count} vaults for GoogleId: {googleId}");
        return Ok(vaults);
    }

    [HttpGet("potential-friends/{googleId}")]
    public async Task<IActionResult> GetPotentialFriends(string googleId)
    {
        Console.WriteLine($"Received request to retrieve potential friends for GoogleId: {googleId}");
        var potentialFriends = await _userService.GetAllUsersExceptAsync(googleId);
        Console.WriteLine($"Returning {potentialFriends.Count} potential friends for GoogleId: {googleId}");
        return Ok(potentialFriends);
    }

    [HttpPost("add-friend")]
public async Task<IActionResult> AddFriend([FromBody] FriendRequest friendRequest)
{
    Console.WriteLine($"Received request to add friend with ID: {friendRequest.FriendId}");
    
    var googleId = HttpContext.Session.GetString("GoogleId");
    if (string.IsNullOrEmpty(googleId))
    {
        Console.WriteLine("Google ID is missing in session, cannot add friend.");
        return Unauthorized("User not logged in");
    }

    var user = await _userService.GetUserByGoogleIdAsync(googleId);
    if (user == null)
    {
        Console.WriteLine($"User with GoogleId {googleId} not found");
        return NotFound("User not found");
    }

    var friend = await _userService.GetUserByIdAsync(friendRequest.FriendId);
    if (friend == null)
    {
        Console.WriteLine($"Friend with ID {friendRequest.FriendId} not found");
        return NotFound("Friend not found");
    }

    // Add friend ID to user's friend list
    user.FriendIds.Add(friendRequest.FriendId);
    await _userService.UpdateUserAsync(user); // Assuming UpdateUserAsync updates the user

    Console.WriteLine($"Friend {friendRequest.FriendId} added to user {googleId}'s friend list.");
    return Ok("Friend added successfully");
}

[HttpPost("send")]
public async Task<IActionResult> SendPayment([FromBody] PaymentRequest request)
{
    var user = await _userService.GetUserByGoogleIdAsync(request.GoogleId);
    var recipient = await _userService.GetUserByIdAsync(request.RecipientId);

    if (user == null || recipient == null)
    {
        return BadRequest("Invalid sender or recipient.");
    }

    // Ensure sender and recipient have the necessary payment details
    if (string.IsNullOrEmpty(user.AccountNumber) || string.IsNullOrEmpty(user.RoutingNumber) ||
        string.IsNullOrEmpty(user.MerchantID) || string.IsNullOrEmpty(recipient.CardNumber) ||
        string.IsNullOrEmpty(recipient.CardExpiry))
    {
        return BadRequest("Sender or recipient lacks necessary payment details.");
    }

    var paymentService = new PaymentService(await _authService.GetAccessTokenAsync());
    var success = await paymentService.PushToCardAsync(
        user,  // Pass the user as the sender
        recipient,  // Pass the recipient directly
        request.Amount
    );

    if (success)
    {
        return Ok("Payment sent successfully.");
    }
    else
    {
        return StatusCode(500, "Failed to send payment.");
    }
}



public class PaymentRequest
{
    public string GoogleId { get; set; }
    public string VaultId { get; set; }
    public string RecipientId { get; set; }
    public decimal Amount { get; set; }
}


// DTO class for FriendRequest
public class FriendRequest
{
    public string FriendId { get; set; }
}

}
