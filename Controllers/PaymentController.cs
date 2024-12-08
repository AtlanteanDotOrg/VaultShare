using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly UserService _userService;
    private readonly USAuthService _authService;
    private readonly VaultService _vaultService;

    public PaymentController(UserService userService, USAuthService authService, VaultService vaultService)
    {
        _userService = userService;
        _authService = authService;
        _vaultService = vaultService;
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

    [HttpGet("friends-reg/{id}")]
    public async Task<IActionResult> GetFriendsReg(string id)
    {
        Console.WriteLine($"[GetFriends] Received request for Id: {id}");
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            Console.WriteLine($"[GetFriends] User with Id {id} not found.");
            return NotFound("User not found");
        }

        var friends = await _userService.GetFriendsAsync(user.Id);
        Console.WriteLine($"[GetFriends] Returning {friends.Count} friends for Id: {id}");
        return Ok(friends);
    }

[HttpGet("potential-friends/{googleId}")]
public async Task<IActionResult> GetPotentialFriends(string googleId)
{
    try
    {
        Console.WriteLine($"[GetPotentialFriends] Received request for GoogleId: {googleId}");

        // Fetch all users except the one with this GoogleId
        var potentialFriends = await _userService.GetAllUsersExceptAsync(googleId);

        if (potentialFriends == null || !potentialFriends.Any())
        {
            Console.WriteLine($"[GetPotentialFriends] No potential friends found for GoogleId: {googleId}");
            return Ok(new List<object>()); // Return an empty list if no friends are found
        }

        Console.WriteLine($"[GetPotentialFriends] Returning {potentialFriends.Count} potential friends for GoogleId: {googleId}");
        return Ok(potentialFriends);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GetPotentialFriends] An error occurred: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return StatusCode(500, "An error occurred while retrieving potential friends.");
    }
}

    [HttpGet("potential-friends-reg/{id}")]
    public async Task<IActionResult> GetPotentialRegFriends(string id)
    {
        Console.WriteLine($"Received request to retrieve potential friends for GoogleId: {id}");
        var potentialFriends = await _userService.GetAllUsersExceptRegAsync(id);
        Console.WriteLine($"Returning {potentialFriends.Count} potential friends for GoogleId: {id}");
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

    [HttpPost("add-friend-reg")]
    public async Task<IActionResult> AddFriendReg([FromBody] FriendRequest friendRequest)
    {
        Console.WriteLine($"Received request to add friend with ID: {friendRequest.FriendId}");
        
        var id = HttpContext.Session.GetString("Id");
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine("ID is missing in session, cannot add friend.");
            return Unauthorized("User not logged in");
        }

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            Console.WriteLine($"User with Id {id} not found");
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

        Console.WriteLine($"Friend {friendRequest.FriendId} added to user {id}'s friend list.");
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

    [HttpGet("vaults-reg/{id}")]
    public async Task<IActionResult> GetVaultsReg(string id)
    {
        Console.WriteLine($"[GetVaults] Received request for Id: {id}");
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            Console.WriteLine($"[GetVaults] User with Id {id} not found.");
            return NotFound("User not found");
        }

        var vaults = await _userService.GetUserVaultsAsync(user.Id);
        Console.WriteLine($"[GetVaults] Returning {vaults.Count} vaults for Id: {id}");
        return Ok(vaults);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendPayment([FromBody] PaymentRequest request)
    {
        if(!string.IsNullOrEmpty(request.GoogleId)) {
            Console.WriteLine($"[SendPayment] Received payment request: GoogleId={request.GoogleId}, RecipientId={request.RecipientId}, Amount={request.Amount}");

            // Retrieve sender (user) based on GoogleId
            var user = await _userService.GetUserByGoogleIdAsync(request.GoogleId);
            if (user == null)
            {
                Console.WriteLine("[SendPayment] Sender not found.");
                return BadRequest("Sender not found.");
            }

            if (user.Balance < request.Amount)
            {
                Console.WriteLine("[SendPayment] Insufficient balance.");
                return BadRequest("Insufficient balance.");
            }

            // Determine if the recipient is a vault or a user
            object recipient = user.Vaults.FirstOrDefault(v => v.VaultId == request.RecipientId);
            if (recipient == null)
            {
                recipient = await _userService.GetUserByIdAsync(request.RecipientId);
            }

            if (recipient == null)
            {
                Console.WriteLine("[SendPayment] Invalid recipient.");
                return BadRequest("Invalid recipient.");
            }

            // Deduct amount from sender's balance
            user.Balance -= (int)request.Amount;
            await _userService.UpdateUserAsync(user);
            Console.WriteLine($"[SendPayment] Deducted {request.Amount} from sender {user.Name}. New Balance={user.Balance}");

            if (recipient is Vault recipientVault)
            {
                // Update the recipient's balance in the database
                recipientVault.Balance += (int)request.Amount;
                await _vaultService.UpdateVaultAsync(recipientVault);
                Console.WriteLine($"[SendPayment] Added {request.Amount} to User {recipientVault.Name}. New User Balance={recipientVault.Balance}");

                // Push payment to card
                Console.WriteLine("[SendPayment] Fetching access token for PushToCard...");
                var accessToken = await _authService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("[SendPayment] Failed to retrieve access token.");
                    return StatusCode(500, "Failed to retrieve access token.");
                }

                Console.WriteLine($"[SendPayment] Access token retrieved: {accessToken.Substring(0, 10)}...");
                var paymentService = new PaymentService(accessToken);

                Console.WriteLine("[SendPayment] Initiating PushToCard...");
                var pushSuccess = await paymentService.PushToCardAsync(user, recipientVault, request.Amount);

                if (!pushSuccess)
                {
                    Console.WriteLine("[SendPayment] Push to card failed. Rolling back database changes...");
                    // Rollback sender and recipient balances
                    user.Balance += (int)request.Amount;
                    recipientVault.Balance -= (int)request.Amount;

                    await _userService.UpdateUserAsync(user);
                    await _vaultService.UpdateVaultAsync(recipientVault);

                    await _userService.UpdateUserAsync(user);

                    return StatusCode(500, "Failed to send payment through PushToCard.");
                }

                Console.WriteLine("[SendPayment] Payment sent successfully via PushToCard.");
                return Ok($"Sent {request.Amount} to User {recipientVault.Name}");
            }
            else if (recipient is User recipientUser)
            {
                // Update the recipient's balance in the database
                recipientUser.Balance += (int)request.Amount;
                await _userService.UpdateUserAsync(recipientUser);
                Console.WriteLine($"[SendPayment] Added {request.Amount} to User {recipientUser.Name}. New User Balance={recipientUser.Balance}");

                // Push payment to card
                Console.WriteLine("[SendPayment] Fetching access token for PushToCard...");
                var accessToken = await _authService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("[SendPayment] Failed to retrieve access token.");
                    return StatusCode(500, "Failed to retrieve access token.");
                }

                Console.WriteLine($"[SendPayment] Access token retrieved: {accessToken.Substring(0, 10)}...");
                var paymentService = new PaymentService(accessToken);

                Console.WriteLine("[SendPayment] Initiating PushToCard...");
                var pushSuccess = await paymentService.PushToCardAsync(user, recipientUser, request.Amount);

                if (!pushSuccess)
                {
                    Console.WriteLine("[SendPayment] Push to card failed. Rolling back database changes...");
                    // Rollback sender and recipient balances
                    user.Balance += (int)request.Amount;
                    recipientUser.Balance -= (int)request.Amount;

                    await _userService.UpdateUserAsync(user);
                    await _userService.UpdateUserAsync(recipientUser);

                    return StatusCode(500, "Failed to send payment through PushToCard.");
                }

                Console.WriteLine("[SendPayment] Payment sent successfully via PushToCard.");
                return Ok($"Sent {request.Amount} to User {recipientUser.Name}");
            }

            return BadRequest("Unexpected error occurred.");
        } else {
            Console.WriteLine($"[SendPayment] Received payment request: Id={request.GoogleId}, RecipientId={request.RecipientId}, Amount={request.Amount}");

            // Retrieve sender (user) based on Id
            var user = await _userService.GetUserByIdAsync(request.GoogleId);
            if (user == null)
            {
                Console.WriteLine("[SendPayment] Sender not found.");
                return BadRequest("Sender not found.");
            }

            if (user.Balance < request.Amount)
            {
                Console.WriteLine("[SendPayment] Insufficient balance.");
                return BadRequest("Insufficient balance.");
            }

            // Determine if the recipient is a vault or a user
            object recipient = user.Vaults.FirstOrDefault(v => v.VaultId == request.RecipientId);
            if (recipient == null)
            {
                recipient = await _userService.GetUserByIdAsync(request.RecipientId);
            }

            if (recipient == null)
            {
                Console.WriteLine("[SendPayment] Invalid recipient.");
                return BadRequest("Invalid recipient.");
            }

            // Deduct amount from sender's balance
            user.Balance -= (int)request.Amount;
            await _userService.UpdateUserAsync(user);
            Console.WriteLine($"[SendPayment] Deducted {request.Amount} from sender {user.Name}. New Balance={user.Balance}");

            if (recipient is Vault recipientVault)
            {
                // Update the recipient's balance in the database
                recipientVault.Balance += (int)request.Amount;
                await _vaultService.UpdateVaultAsync(recipientVault);
                Console.WriteLine($"[SendPayment] Added {request.Amount} to User {recipientVault.Name}. New User Balance={recipientVault.Balance}");

                // Push payment to card
                Console.WriteLine("[SendPayment] Fetching access token for PushToCard...");
                var accessToken = await _authService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("[SendPayment] Failed to retrieve access token.");
                    return StatusCode(500, "Failed to retrieve access token.");
                }

                Console.WriteLine($"[SendPayment] Access token retrieved: {accessToken.Substring(0, 10)}...");
                var paymentService = new PaymentService(accessToken);

                Console.WriteLine("[SendPayment] Initiating PushToCard...");
                var pushSuccess = await paymentService.PushToCardAsync(user, recipientVault, request.Amount);

                if (!pushSuccess)
                {
                    Console.WriteLine("[SendPayment] Push to card failed. Rolling back database changes...");
                    // Rollback sender and recipient balances
                    user.Balance += (int)request.Amount;
                    recipientVault.Balance -= (int)request.Amount;

                    await _userService.UpdateUserAsync(user);
                    await _vaultService.UpdateVaultAsync(recipientVault);

                    await _userService.UpdateUserAsync(user);

                    return StatusCode(500, "Failed to send payment through PushToCard.");
                }

                Console.WriteLine("[SendPayment] Payment sent successfully via PushToCard.");
                return Ok($"Sent {request.Amount} to User {recipientVault.Name}");
            }
            else if (recipient is User recipientUser)
            {
                // Update the recipient's balance in the database
                recipientUser.Balance += (int)request.Amount;
                await _userService.UpdateUserAsync(recipientUser);
                Console.WriteLine($"[SendPayment] Added {request.Amount} to User {recipientUser.Name}. New User Balance={recipientUser.Balance}");

                // Push payment to card
                Console.WriteLine("[SendPayment] Fetching access token for PushToCard...");
                var accessToken = await _authService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("[SendPayment] Failed to retrieve access token.");
                    return StatusCode(500, "Failed to retrieve access token.");
                }

                Console.WriteLine($"[SendPayment] Access token retrieved: {accessToken.Substring(0, 10)}...");
                var paymentService = new PaymentService(accessToken);

                Console.WriteLine("[SendPayment] Initiating PushToCard...");
                var pushSuccess = await paymentService.PushToCardAsync(user, recipientUser, request.Amount);

                if (!pushSuccess)
                {
                    Console.WriteLine("[SendPayment] Push to card failed. Rolling back database changes...");
                    // Rollback sender and recipient balances
                    user.Balance += (int)request.Amount;
                    recipientUser.Balance -= (int)request.Amount;

                    await _userService.UpdateUserAsync(user);
                    await _userService.UpdateUserAsync(recipientUser);

                    return StatusCode(500, "Failed to send payment through PushToCard.");
                }

                Console.WriteLine("[SendPayment] Payment sent successfully via PushToCard.");
                return Ok($"Sent {request.Amount} to User {recipientUser.Name}");
            }

            return BadRequest("Unexpected error occurred.");
        }
    }



}

public class PaymentRequest
{
    public string GoogleId { get; set; } // Optional Google ID
    public string RecipientId { get; set; }
    public int Amount { get; set; }
}



public class FriendRequest
{
    public string FriendId { get; set; }
}
