using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VaultShare.Models;
using System.Collections.Generic; // For IEnumerable
using System.Threading.Tasks; // For Task
using BCrypt.Net; // For BCrypt
using MongoDB.Driver;  // For Builders and _users
using System.Security.Claims;  // For Claims and ClaimsPrincipal
using Microsoft.AspNetCore.Authentication.Cookies;  // For CookieAuthenticationDefaults


namespace VaultShare.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly UserService _userService;

        // Inject UserService instead of MongoDbService
        public UserController(UserService userService)
        {
            _userService = userService;
        }

        // Get all user profiles
        [HttpGet]
        public async Task<IEnumerable<User>> Get()
        {
            return await _userService.GetAllUsersAsync();
        }

        // Get a specific user by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<User?>> GetById(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user is not null ? Ok(user) : NotFound();
        }

        // Register a new user
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] User user)
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
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // Hash the password using BCrypt
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Search users by username
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<User>>> SearchByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Username cannot be empty." });
            }

            var users = await _userService.SearchUsersByUsernameAsync(username);

            if (users == null || !users.Any())
            {
                return NotFound(new { message = "No users found." });
            }

            return Ok(users);
        }

        // Delete a user by ID
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userService.DeleteUserAsync(id); // Use UserService to delete user
            return Ok();
        }
    }
}
