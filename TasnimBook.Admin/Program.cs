using Microsoft.AspNetCore.Authentication.Cookies;
using TasnimBook.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    var baseAddress="";
#if DEBUG
    baseAddress = builder.Configuration["Api:LocalBaseAddress"]
        ?? "https://localhost:44396/api/";
#else
    baseAddress = builder.Configuration["Api:OnlineBaseAddress"]
        ?? "https://localhost:44396/api/";
#endif

    client.BaseAddress = new Uri(baseAddress);
    client.Timeout = TimeSpan.FromMinutes(10);
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "TasnimBook.Admin.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
