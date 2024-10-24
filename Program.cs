using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MongoDB.Driver;

var connectionString = "mongodb+srv://admin:admin123@cluster0.qna17.mongodb.net/?retryWrites=true&w=majority";
var mongoClient = new MongoClient(connectionString);
var database = mongoClient.GetDatabase("VaultShareDB");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = "211102231626-nimn1hsavm542tejgf4q1t37b1ocvnc9.apps.googleusercontent.com";
    options.ClientSecret = "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";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = async context =>
    {
        var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();

        // Retrieve user info from Google
        var googleId = context.Principal.FindFirst(c => c.Type == "sub")?.Value;
        var email = context.Principal.FindFirst(c => c.Type == "email")?.Value;
        var name = context.Principal.FindFirst(c => c.Type == "name")?.Value;

        if (!string.IsNullOrEmpty(googleId) && !string.IsNullOrEmpty(email))
        {
            // Check if user already exists
            var existingUser = await userService.GetUserByGoogleIdAsync(googleId);

            if (existingUser == null)
            {
                // If user doesn't exist, create a new user
                var newUser = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    Name = name ?? "Google User"
                };

                await userService.CreateUserAsync(newUser);
            }
        }
    };
});


builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddScoped<UserService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Ensure this is before UseAuthorization
app.UseAuthorization();

// Define routes for the application.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
