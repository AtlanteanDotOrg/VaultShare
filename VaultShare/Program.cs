using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MongoDB.Driver;
using VaultShare.Models;
using Stripe;
using DotNetEnv;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
builder.Services.AddSingleton<USAuthService>();
builder.Services.AddSingleton<StripeService>();



// Enable Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set a reasonable session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Required for session to work without explicit user consent
});


// Add Authentication configuration
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

            if (!string.IsNullOrEmpty(googleId))
            {
                Console.WriteLine("Storing Google ID in session: " + googleId);
                context.HttpContext.Session.SetString("GoogleId", googleId);
            }

            if (!string.IsNullOrEmpty(googleId) && !string.IsNullOrEmpty(email))
            {
                var existingUser = await userService.GetUserByGoogleIdAsync(googleId);
                if (existingUser == null)
                {
                    Console.WriteLine("User not found, creating a new user.");
                    var newUser = new VaultShare.Models.User
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

// Add Controllers with Views
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseSession();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Use session middleware

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.MapGet("/test-usbank-auth", async () =>
{
    var authService = new USAuthService();
    var accessToken = await authService.GetAccessTokenAsync();
    return accessToken != null ? Results.Ok("Access Token retrieved successfully!") : Results.Problem("Failed to retrieve access token.");
});

// app.MapGet("/test-push-payment", async () =>
// {
//     var authService = new USAuthService();
//     var accessToken = await authService.GetAccessTokenAsync();

//     if (string.IsNullOrEmpty(accessToken))
//     {
//         return Results.Problem("Unable to obtain access token; cannot proceed with payment.");
//     }

//     var paymentService = new PaymentService(accessToken);
//     var sender = new PaymentUser { CardNumber = "4111111111111111", CardExpiry = "12/25", CardCvc = "123" };
//     var recipient = new PaymentUser { CardNumber = "4111111111111112", CardExpiry = "12/25" };
//     decimal amount = 50.00M;

//     var success = await paymentService.PushToCardAsync(sender, recipient, amount);
//     return success ? Results.Ok("Payment successfully processed.") : Results.Problem("Failed to process payment.");
// });

app.Run();

