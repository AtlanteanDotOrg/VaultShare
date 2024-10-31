using MongoDB.Driver;
using VaultShare.Models;
using System.Threading.Tasks;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(IMongoClient client)
    {
        var database = client.GetDatabase("VaultShareDB");
        _users = database.GetCollection<User>("Users");
    }

    public async Task<User?> GetUserByGoogleIdAsync(string googleId)
    {
        return await _users.Find(user => user.GoogleId == googleId).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _users.Find(user => user.Email == email).FirstOrDefaultAsync();
    }

    public async Task CreateUserAsync(User newUser)
    {
        await _users.InsertOneAsync(newUser);
    }
}
