using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetEnv;
using Supabase;
using UrlShortener.Services;
using UrlShortener.Utilities;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
{
    throw new InvalidOperationException("Supabase URL and Key must be set in the .env file.");
}

var options = new SupabaseOptions { AutoConnectRealtime = true };
var supabaseClient = new Client(supabaseUrl, supabaseKey, options);
await supabaseClient.InitializeAsync();
builder.Services.AddSingleton(supabaseClient);

builder.Services.AddSingleton<SupabaseService>();

builder.WebHost.UseUrls("http://localhost:5000"); // set custom port (5000)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CodeGenerator>();
builder.Services.AddSingleton<RateLimitService>();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
});

builder.Services.AddControllers();

// Enable CORS (for the frontend on localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();