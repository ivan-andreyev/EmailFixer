using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EmailFixer.Client;
using EmailFixer.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Blazored LocalStorage (must be registered before auth services)
builder.Services.AddBlazoredLocalStorage();

// Configure API base URL
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5165/";

// Register Authentication Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Configure HttpClient with AuthHttpClientHandler and timeout
builder.Services.AddScoped<AuthHttpClientHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHttpClientHandler>();
    var innerHandler = new HttpClientHandler
    {
        // Enable automatic decompression for gzip/deflate
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    };
    handler.InnerHandler = innerHandler;

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl),
        // Set timeout to prevent hanging requests (30 seconds)
        Timeout = TimeSpan.FromSeconds(30)
    };

    return httpClient;
});

// Register Application Services
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IToastNotificationService, ToastNotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<DebounceService>();
builder.Services.AddScoped<CacheService>();

// Add Authorization
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
