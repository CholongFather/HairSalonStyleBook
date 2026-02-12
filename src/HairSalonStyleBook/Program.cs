using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using HairSalonStyleBook;
using HairSalonStyleBook.Services;
using HairSalonStyleBook.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 인증
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, AdminAuthStateProvider>();
builder.Services.AddScoped<IAuthService, SimpleAuthService>();

// 서비스
builder.Services.AddScoped<IStyleService, FirestoreStyleService>();
builder.Services.AddScoped<IAuditService, FirestoreAuditService>();
builder.Services.AddScoped<IImageService, FirebaseStorageService>();
builder.Services.AddScoped<IShopConfigService, FirestoreShopConfigService>();
builder.Services.AddScoped<ILoginSecurityService, FirestoreLoginSecurityService>();

await builder.Build().RunAsync();
