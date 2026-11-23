using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using TasnimBook.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ReleaseApiClient>(client =>
{
    var baseAddress = builder.Configuration["Api:BaseAddress"] ?? "https://localhost:44396/api/";
    client.BaseAddress = new Uri(baseAddress.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddResponseCompression();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public,max-age=604800" });
if (app.Environment.IsDevelopment())
{
    var repositoryRoot = Directory.GetParent(app.Environment.ContentRootPath)!.FullName;
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(repositoryRoot, "TasnimBook.MAUI", "TasnimBook.MAUI", "Resources", "AppIcon")),
        RequestPath = "/images"
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(repositoryRoot, "TasnimBook.Admin", "wwwroot", "fonts", "vazirmatn")),
        RequestPath = "/fonts"
    });
}
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
