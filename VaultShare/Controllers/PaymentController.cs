using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;

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
        Console.WriteLine("[PaymentController] Initialized.");
    }

    [HttpGet("friends/{googleId}")]
    public async Task<IActionResult> GetFriends(string googleId)
    {
        Console.WriteLine($"[GetFriends] Received request for GoogleId: {googleId}");
        var user = await _userService.GetUserByGoogleIdAsync(googleId);

        if (user == null)
        {
            Console.WriteLine($"[GetFriends] User with GoogleId {googleId} not found.");
            return NotFound("User not found");
        }

        var friends = await _userService.GetFriendsAsync(user.Id);
        Console.WriteLine($"[GetFriends] Returning {friends.Count} friends for GoogleId: {googleId}");
        return Ok(friends);
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

    [HttpGet("vaults/{googleId}")]
    public async Task<IActionResult> GetVaults(string googleId)
    {
        Console.WriteLine($"[GetVaults] Received request for GoogleId: {googleId}");
        var user = await _userService.GetUserByGoogleIdAsync(googleId);

        if (user == null)
        {
            Console.WriteLine($"[GetVaults] User with GoogleId {googleId} not found.");
            return NotFound("User not found");
        }

        var vaults = await _userService.GetUserVaultsAsync(user.Id);
        Console.WriteLine($"[GetVaults] Returning {vaults.Count} vaults for GoogleId: {googleId}");
        return Ok(vaults);
    }

[HttpPost("send")]
public async Task<IActionResult> SendPayment([FromBody] PaymentRequest request)
{
    Console.WriteLine($"[SendPayment] Received payment request: GoogleId={request.GoogleId}, RecipientId={request.RecipientId}, Amount={request.Amount}");

    // Retrieve sender (user) based on GoogleId
    var user = await _userService.GetUserByGoogleIdAsync(request.GoogleId);
    var recipient = await _userService.GetUserByIdAsync(request.RecipientId);

    if (user == null || recipient == null)
    {
        Console.WriteLine("[SendPayment] Invalid sender or recipient.");
        return BadRequest("Invalid sender or recipient.");
    }

    // Check and log missing payment details for sender and recipient
    bool missingDetails = false;
    if (string.IsNullOrEmpty(user.AccountNumber))
    {
        Console.WriteLine("[SendPayment] Sender's AccountNumber is missing.");
        missingDetails = true;
    }
    if (string.IsNullOrEmpty(user.RoutingNumber))
    {
        Console.WriteLine("[SendPayment] Sender's RoutingNumber is missing.");
        missingDetails = true;
    }
    if (string.IsNullOrEmpty(user.MerchantID))
    {
        Console.WriteLine("[SendPayment] Sender's MerchantID is missing.");
        missingDetails = true;
    }
    if (string.IsNullOrEmpty(recipient.CardNumber))
    {
        Console.WriteLine("[SendPayment] Recipient's CardNumber is missing.");
        missingDetails = true;
    }
    if (string.IsNullOrEmpty(recipient.CardExpiry))
    {
        Console.WriteLine("[SendPayment] Recipient's CardExpiry is missing.");
        missingDetails = true;
    }

    if (missingDetails)
    {
        return BadRequest("Sender or recipient lacks necessary payment details.");
    }

    Console.WriteLine("[SendPayment] Fetching access token for payment.");
    var accessToken = await _authService.GetAccessTokenAsync();
    
    Console.WriteLine($"[SendPayment] Access token retrieved: {(string.IsNullOrEmpty(accessToken) ? "No token retrieved" : accessToken.Substring(0, 10) + "...")}");

    if (string.IsNullOrEmpty(accessToken))
    {
        Console.WriteLine("[SendPayment] Failed to retrieve access token.");
        return StatusCode(500, "Failed to retrieve access token.");
    }

    var paymentService = new PaymentService(accessToken);
    Console.WriteLine("[SendPayment] Access token passed to PaymentService. Initiating payment...");

    var success = await paymentService.PushToCardAsync(user, recipient, request.Amount);

    if (success)
    {
        Console.WriteLine("[SendPayment] Payment sent successfully.");
        return Ok("Payment sent successfully.");
    }
    else
    {
        Console.WriteLine("[SendPayment] Payment failed.");
        return StatusCode(500, "Failed to send payment.");
    }
}


}

public class PaymentRequest
{
    public string GoogleId { get; set; } // The ID of the logged-in user (sender)
    public string RecipientId { get; set; } // Either a user or vault ID
    public decimal Amount { get; set; }
}


public class FriendRequest
{
    public string FriendId { get; set; }
}
