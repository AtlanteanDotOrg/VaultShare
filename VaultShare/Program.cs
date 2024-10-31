using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MongoDB.Driver;
using VaultShare.Models;
using Stripe;  // Added Stripe import
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
DotNetEnv.Env.Load();

// Access environment variables
var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var stripeApiKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY");

// Configure Stripe API Key
StripeConfiguration.ApiKey = stripeApiKey;

// MongoDB configuration
var mongoClient = new MongoClient(connectionString);
var database = mongoClient.GetDatabase("VaultShareDB");

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddScoped<UserService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.Events.OnCreatingTicket = async context =>
    {
        try
        {
            Console.WriteLine("Google OAuth OnCreatingTicket event started.");

            var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
            Console.WriteLine("UserService retrieved successfully.");

            var googleId = context.Principal?.FindFirst(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            var email = context.Principal?.FindFirst(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
            var name = context.Principal?.FindFirst(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            Console.WriteLine($"GoogleId: {googleId}");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"Name: {name}");

            if (!string.IsNullOrEmpty(googleId) && !string.IsNullOrEmpty(email))
            {
                var existingUser = await userService.GetUserByGoogleIdAsync(googleId);
                if (existingUser == null)
                {
                    Console.WriteLine("User not found, creating a new user.");
                    var newUser = new VaultShare.Models.User  // Fully qualified name here
                    {
                        GoogleId = googleId,
                        Email = email,
                        Name = name ?? "Google User"
                    };

                    await userService.CreateUserAsync(newUser);
                    Console.WriteLine("New user created successfully.");
                }
                else
                {
                    Console.WriteLine("User already exists in the database.");
                }
            }
            else
            {
                Console.WriteLine("GoogleId or Email is missing.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred during OnCreatingTicket: {ex.Message}");
        }
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
