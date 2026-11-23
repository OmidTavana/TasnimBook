using Microsoft.AspNetCore.Mvc;
using TasnimBook.Web.Models;
using TasnimBook.Web.Services;

namespace TasnimBook.Web.Controllers;

public sealed class HomeController(ReleaseApiClient releases, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var latest = await releases.GetLatestAsync(cancellationToken);
        return View(new LandingViewModel
        {
            Android = latest.Android,
            Windows = latest.Windows,
            BackendUnavailable = latest.Unavailable,
            CanonicalUrl = configuration["Site:PublicUrl"]?.TrimEnd('/')
        });
    }

    [Route("Home/Error")]
    public IActionResult Error() => View();

    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    public IActionResult Sitemap()
    {
        var root = configuration["Site:PublicUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(root)) root = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        return Content($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><url><loc>{System.Security.SecurityElement.Escape(root)}</loc><changefreq>weekly</changefreq></url></urlset>", "application/xml");
    }

    [HttpGet("robots.txt")]
    [Produces("text/plain")]
    public IActionResult Robots()
    {
        var root = configuration["Site:PublicUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(root)) root = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        return Content($"User-agent: *\nAllow: /\nSitemap: {root}/sitemap.xml\n", "text/plain");
    }
}
