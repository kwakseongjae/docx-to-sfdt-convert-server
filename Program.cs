using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS configuration - Allow Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://www.k-startup.ai",
                "https://k-startup.ai"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Register Syncfusion License (MUST be after builder.Build())
// Priority: Environment Variable > appsettings.json
var envLicense = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
var configLicense = builder.Configuration["Syncfusion:LicenseKey"];
var syncfusionLicense = envLicense ?? configLicense;

Console.WriteLine("📋 License Key Source Check:");
Console.WriteLine($"   - Environment Variable: {(envLicense != null ? $"Found ({envLicense.Substring(0, 20)}...)" : "Not found")}");
Console.WriteLine($"   - appsettings.json: {(configLicense != null ? $"Found ({configLicense.Substring(0, 20)}...)" : "Not found")}");
Console.WriteLine($"   - Using: {(envLicense != null ? "Environment Variable" : configLicense != null ? "appsettings.json" : "NONE")}");

if (!string.IsNullOrEmpty(syncfusionLicense))
{
    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
    Console.WriteLine($"✅ Syncfusion license registered successfully (Length: {syncfusionLicense.Length} chars)");
    Console.WriteLine($"   Key preview: {syncfusionLicense.Substring(0, Math.Min(30, syncfusionLicense.Length))}...");
}
else
{
    Console.WriteLine("⚠️  WARNING: Syncfusion license key not found!");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowNextJS");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "DocumentEditor Server" }));

app.Run();
