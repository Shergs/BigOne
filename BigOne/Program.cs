using Discord.Interactions;
using Discord.WebSocket;
using BigOne;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using Microsoft.EntityFrameworkCore;
using BigOne.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Discord
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<ConversationService>();
builder.Services.AddHostedService<DiscordClientHost>();

// Lavalink
builder.Services.AddLavalink();
builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

//Database
var connectionString = builder.Configuration.GetConnectionString("BigOne") ?? throw new InvalidOperationException("Connection string 'BigOne' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add controllers for API
builder.Services.AddControllers();

// Logging
builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Run the application
app.Run();





