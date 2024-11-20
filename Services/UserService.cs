using MongoDB.Driver;
using VaultShare.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class UserService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoDatabase _database; // Add this field to store the database instance
    private readonly ILogger<UserService> _logger;

    public UserService(IMongoClient client, ILogger<UserService> logger)
    {
        _database = client.GetDatabase("VaultShareDB"); // Initialize the database field
        _users = _database.GetCollection<User>("Users");
        _logger = logger;
    }

    public async Task<User?> GetUserByGoogleIdAsync(string googleId)
    {
        _logger.LogInformation("Attempting to retrieve user by GoogleId: {GoogleId}", googleId);
        var user = await _users.Find(user => user.GoogleId == googleId).FirstOrDefaultAsync();
        if (user == null)
        {
            _logger.LogWarning("No user found with GoogleId: {GoogleId}", googleId);
        }
        else
        {
            _logger.LogInformation("User found with GoogleId: {GoogleId}", googleId);
        }
        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Attempting to retrieve user by Email: {Email}", email);
        return await _users.Find(user => user.Email == email).FirstOrDefaultAsync();
    }

    public async Task CreateUserAsync(User newUser)
    {
        _logger.LogInformation("Creating new user with GoogleId: {GoogleId} and Email: {Email}", newUser.GoogleId, newUser.Email);
        await _users.InsertOneAsync(newUser);
        _logger.LogInformation("User created successfully.");
    }

    public async Task<bool> RegisterUserAsync(User newUser)
    {
        // Check if a user with the same Email or Id already exists
        var existingUser = await _users.Find(u => u.Email == newUser.Email || u.Id == newUser.Id).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            _logger.LogWarning("User with Email: {Email} or Id: {Id} already exists.", newUser.Email, newUser.Id);
            return false; // User already exists
        }

        // Insert the new user into the Users collection
        await _users.InsertOneAsync(newUser);
        _logger.LogInformation("New user registered with Email: {Email} and Id: {Id}", newUser.Email, newUser.Id);
        return true;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        _logger.LogInformation("Attempting to retrieve user by Id: {UserId}", userId);
        return await _users.Find(user => user.Id == userId).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetFriendsAsync(string userId)
    {
        _logger.LogInformation("Retrieving friends for user Id: {UserId}", userId);
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("No user found with Id: {UserId}", userId);
            return new List<User>();
        }

        var friendIds = user.FriendIds;
        _logger.LogInformation("Found {Count} friends for user Id: {UserId}", friendIds.Count, userId);
        return await _users.Find(u => friendIds.Contains(u.Id)).ToListAsync();
    }

    public async Task<List<Vault>> GetUserVaultsAsync(string userId)
    {
        _logger.LogInformation("Retrieving vaults for user Id: {UserId}", userId);
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("No user found with Id: {UserId}", userId);
            return new List<Vault>();
        }
        _logger.LogInformation("Retrieved {Count} vaults for user Id: {UserId}", user.Vaults?.Count ?? 0, userId);
        return user.Vaults ?? new List<Vault>();
    }

    public async Task<List<User>> GetAllUsersExceptAsync(string googleId)
    {
        _logger.LogInformation("Retrieving all users except for GoogleId: {GoogleId}", googleId);
        var filter = Builders<User>.Filter.Ne(u => u.GoogleId, googleId);
        var users = await _users.Find(filter).ToListAsync();
        _logger.LogInformation("Retrieved {Count} potential friends for GoogleId: {GoogleId}", users.Count, googleId);
        return users;
    }

    public async Task<List<User>> GetAllUsersExceptRegAsync(string id)
    {
        _logger.LogInformation("Retrieving all users except for GoogleId: {GoogleId}", id);
        var filter = Builders<User>.Filter.Ne(u => u.Id, id);
        var users = await _users.Find(filter).ToListAsync();
        _logger.LogInformation("Retrieved {Count} potential friends for GoogleId: {GoogleId}", users.Count, id);
        return users;
    }

    public async Task UpdateUserAsync(User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        await _users.ReplaceOneAsync(filter, user);
    }

    // Method to update username
    public async Task UpdateUsernameAsync(string userId, string newUsername)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Name, newUsername);
        await _users.UpdateOneAsync(filter, update);
    }

    // Method to update bio
    public async Task UpdateBioAsync(string userId, string newBio)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Bio, newBio);
        await _users.UpdateOneAsync(filter, update);
    }

    // Method to update notification settings
    public async Task UpdateNotifications(string userId, bool enable)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.NotificationsEnabled, enable);
        await _users.UpdateOneAsync(filter, update);
    }

public async Task UpdatePaymentMethodAsync(string userId, PaymentMethodRequest paymentMethod)
{
    var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
    
    var update = Builders<User>.Update
        .Set(u => u.CardNumber, paymentMethod.CardNumber)
        .Set(u => u.CardExpiry, paymentMethod.Expiry)
        .Set(u => u.CardCvc, paymentMethod.Cvv)
        .Set(u => u.CardNickname, paymentMethod.Name)
        .Set(u => u.AccountNumber, paymentMethod.AccountNumber)
        .Set(u => u.RoutingNumber, paymentMethod.RoutingNumber)
        .Set(u => u.MerchantID, paymentMethod.MerchantID);

    await _users.UpdateOneAsync(filter, update);
}


    // Method to reset password
    public async Task ResetPasswordAsync(string userId, string newPassword)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Password, newPassword); // Ensure password is hashed
        await _users.UpdateOneAsync(filter, update);
    }

    public async Task CreateVaultAsync(Vault vault)
    {
        var collection = _database.GetCollection<Vault>("Vaults"); // Use _database field here
        await collection.InsertOneAsync(vault);
    }
}
