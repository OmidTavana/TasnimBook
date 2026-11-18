namespace TasnimBook.MAUI.Services;

/// <summary>
/// Development-only mobile API configuration. Do not use a shared API key in a production APK.
/// </summary>
internal static class ApiConfiguration
{
    internal const string BaseAddress = "http://192.168.1.71:8065/api/";
    //internal const string BaseAddress = "https://api.tasnimbook.ir/api/";
    internal const string ApiKey = "993026bbc13371a5e4c298de5@4a1x6e-8b4b3d9a-e931-478d-a994-23akkkou#$b0";

    internal static bool TryResolveResourceUri(string? value, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var apiBaseUri = new Uri(BaseAddress, UriKind.Absolute);
        if (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var absoluteUri))
        {
            // Older admin builds stored Request.Host in the database. When the admin
            // called the API locally this became localhost, which points back to the
            // Android device instead of the API computer.
            if (!absoluteUri.IsLoopback)
            {
                uri = absoluteUri;
                return true;
            }

            var corrected = new UriBuilder(
                apiBaseUri.Scheme,
                apiBaseUri.Host,
                apiBaseUri.Port,
                absoluteUri.AbsolutePath)
            {
                Query = absoluteUri.Query.TrimStart('?'),
                Fragment = absoluteUri.Fragment.TrimStart('#')
            };
            uri = corrected.Uri;
            return true;
        }

        return Uri.TryCreate(apiBaseUri, value.Trim(), out uri!);
    }

    internal static string? ResolveResourceUrl(string? value) =>
        TryResolveResourceUri(value, out var uri) ? uri.AbsoluteUri : null;
}
