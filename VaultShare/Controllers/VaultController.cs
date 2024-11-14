using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/vault")]
public class VaultController : ControllerBase
{
    private readonly UserService _userService;
    private readonly StripeService _stripeService;

    public VaultController(UserService userService, StripeService stripeService)
    {
        _userService = userService;
        _stripeService = stripeService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateVault([FromBody] VaultRequest request)
    {
        if (string.IsNullOrEmpty(request.VaultName))
        {
            return BadRequest("Vault name is required.");
        }

        // Retrieve the user who is creating the vault
        var googleId = HttpContext.Session.GetString("GoogleId");
        var creator = await _userService.GetUserByGoogleIdAsync(googleId);

        if (creator == null)
        {
            return Unauthorized("User not logged in or not found.");
        }

        // Add the creator as an admin in the vault
        var members = new List<VaultMember>
        {
            new VaultMember { UserId = creator.Id, Role = "admin" }
        };

        // Add other members with specified roles
        foreach (var member in request.Members)
        {
            members.Add(new VaultMember
            {
                UserId = member.FriendId,
                Role = member.Role
            });
        }

        // Create the vault and set members
        var vault = new Vault
        {
            VaultId = Guid.NewGuid().ToString(),
            Name = request.VaultName,
            Members = members
        };

        Console.WriteLine("Starting the process to create a vault: " + vault.Name);

        // Generate a virtual card and link vault to users
        try
        {
            var createdVault = await _stripeService.CreateVirtualCardForVaultAsync(vault, request.SpendingLimit);
            createdVault.Members = members; 

            // Add the vault to each user's Vaults array
            foreach (var member in members)
            {
                var user = await _userService.GetUserByIdAsync(member.UserId);
                if (user != null)
                {
                    user.Vaults.Add(createdVault); // Add the vault to the user's Vaults list
                    await _userService.UpdateUserAsync(user);
                }
            }

            // Store the vault itself in the database
            await _userService.CreateVaultAsync(createdVault); 
            return Ok("Vault and virtual card created successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating virtual card: {ex.Message}");
        }
    }
}

// DTO for the Vault creation request
public class VaultRequest
{
    public long SpendingLimit { get; set; }
    public string VaultName { get; set; }
    public List<MemberRequest> Members { get; set; }
}

// DTO for individual members in VaultRequest
public class MemberRequest
{
    public string FriendId { get; set; }
    public string Role { get; set; }
}

