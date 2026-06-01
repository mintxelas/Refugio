namespace Refugio.Web.Helpers;

public static class PhotoFiles
{
    /// <summary>
    /// Hard-deletes the wwwroot-relative file an app-generated photo URL points to.
    /// No-op for empty URLs, external (http) URLs, or files that no longer exist.
    /// </summary>
    public static void DeleteByUrl(IWebHostEnvironment env, string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith('/')) return;
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(env.WebRootPath, relative);
        if (File.Exists(path)) File.Delete(path);
    }
}
