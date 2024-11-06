using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly UserService _userService;

    public PaymentController(UserService userService)
    {
        _userService = userService;
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

// DTO class for FriendRequest
public class FriendRequest
{
    public string FriendId { get; set; }
}

}
