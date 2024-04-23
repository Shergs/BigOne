using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNet.Security.OAuth.Discord;
using Microsoft.Extensions.FileProviders;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("BigOne") ?? throw new InvalidOperationException("Connection string 'BigOne' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set a long timeout for session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = builder.Configuration["Discord:AuthScheme"];
})
.AddCookie()
.AddDiscord(discordOptions =>
{
    discordOptions.ClientId = builder.Configuration["Discord:ClientId"];  
    discordOptions.ClientSecret = builder.Configuration["Discord:ClientSecret"];
    discordOptions.CallbackPath = new PathString("/signin-discord"); // Ensure this matches the redirect URI in Discord
    discordOptions.Scope.Add("identify"); // Minimum scope for login
    discordOptions.Scope.Add("email"); // If you need the user's email
    discordOptions.Scope.Add("guilds"); // If you need to access the user's guilds
    discordOptions.SaveTokens = true;
});


var app = builder.Build();

// Create a session


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{ 
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Create a PhysicalFileProvider for an external directory
var fileProvider = new PhysicalFileProvider("C:/Workspace_Git/BigOne/BigOneDashboard/Sounds");

// Use static files middleware to serve files from the external directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = "/Sounds" // URL path where these files will be accessible
});

// Standard static files (wwwroot)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
