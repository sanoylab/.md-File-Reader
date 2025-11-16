using Microsoft.EntityFrameworkCore;
using MdReader.Data;
using MdReader.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.HttpOverrides;

// Helper function to extract host from connection string for logging
static string ExtractHostFromConnectionString(string connectionString)
{
    var parts = connectionString.Split(';');
    var hostPart = parts.FirstOrDefault(p => p.StartsWith("Host=", StringComparison.OrdinalIgnoreCase));
    return hostPart?.Substring(5) ?? "unknown";
}

// Load .env file if it exists
var currentDir = Directory.GetCurrentDirectory();
var envPath = Path.Combine(currentDir, ".env");

if (File.Exists(envPath))
{
    // Try DotNetEnv first
    try
    {
        Env.Load(envPath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: DotNetEnv.Load failed: {ex.Message}");
    }
    
    // Also manually parse .env file as backup
    var envLines = File.ReadAllLines(envPath);
    foreach (var line in envLines)
    {
        var trimmedLine = line.Trim();
        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
            continue;
            
        var equalIndex = trimmedLine.IndexOf('=');
        if (equalIndex > 0)
        {
            var key = trimmedLine.Substring(0, equalIndex).Trim();
            var value = trimmedLine.Substring(equalIndex + 1).Trim();
            
            // Remove quotes if present
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2);
            if (value.StartsWith("'") && value.EndsWith("'"))
                value = value.Substring(1, value.Length - 2);
                
            // Only set if not already set (environment variables take precedence)
            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database configuration - Build connection string from individual PostgreSQL environment variables
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    // Try to build from individual PostgreSQL environment variables
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgSslMode = Environment.GetEnvironmentVariable("PGSSLMODE") ?? "require";

    if (!string.IsNullOrWhiteSpace(pgHost) && 
        !string.IsNullOrWhiteSpace(pgDatabase) && 
        !string.IsNullOrWhiteSpace(pgUser) && 
        !string.IsNullOrWhiteSpace(pgPassword))
    {
        connectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SslMode={pgSslMode}";
        Console.WriteLine($"Database connection built from individual variables: Host={pgHost}");
    }
    else
    {
        // Fallback to DATABASE_URL if individual variables not found
        connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: Database connection not configured");
            Console.WriteLine("Please set either:");
            Console.WriteLine("  - PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD (and optionally PGSSLMODE)");
            Console.WriteLine("  - OR DATABASE_URL");
            throw new InvalidOperationException(
                "Database connection string not found. Please set PostgreSQL environment variables (PGHOST, PGDATABASE, PGUSER, PGPASSWORD) or DATABASE_URL in .env file.");
        }
    }
}

Console.WriteLine($"Database connection: Host={ExtractHostFromConnectionString(connectionString)}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<DocumentLimitService>();

// Authentication
var githubClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");
var githubClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");

if (string.IsNullOrWhiteSpace(githubClientId) || string.IsNullOrWhiteSpace(githubClientSecret))
{
    Console.WriteLine("WARNING: GitHub OAuth credentials not found. Set GITHUB_CLIENT_ID and GITHUB_CLIENT_SECRET environment variables.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GitHubAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    // Ensure cookies work with HTTPS in production
    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
})
.AddGitHub(GitHubAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.ClientId = githubClientId ?? string.Empty;
    options.ClientSecret = githubClientSecret ?? string.Empty;
    options.CallbackPath = "/signin-github";
    options.Scope.Add("user:email");
});

// Configure forwarded headers for Render.com proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust all proxies (Render.com uses load balancers)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Authorization
builder.Services.AddAuthorization();

// Session for storing user data
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// IMPORTANT: Use forwarded headers BEFORE other middleware
// This allows the app to detect HTTPS when behind Render.com's proxy
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Ensure database is created
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        Console.WriteLine("Database connection successful!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Database connection error: {ex.Message}");
    throw;
}

// Configure port for Render.com (only override if PORT env var is set AND we're in production)
var port = Environment.GetEnvironmentVariable("PORT");
var isDevelopment = app.Environment.IsDevelopment();

if (!string.IsNullOrEmpty(port) && !isDevelopment)
{
    // In production (Render.com), bind to all interfaces
    app.Urls.Add($"http://+:{port}");
}
// In development, use launchSettings.json which has localhost

app.Run();
