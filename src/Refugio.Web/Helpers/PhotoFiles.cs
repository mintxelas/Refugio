namespace Refugio.Web.Helpers;

public static class PhotoFiles
{
    /// <summary>Returns true when the file's magic bytes match its declared extension.</summary>
    public static bool HasValidImageBytes(IFormFile file, string ext)
    {
        Span<byte> header = stackalloc byte[4];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 3) return false;
        return ext == ".jpg"
            ? header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF
            : read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
    }


    /// <summary>
    /// Hard-deletes the wwwroot-relative file an app-generated photo URL points to.
    /// No-op for empty URLs, external (http) URLs, or files that no longer exist.
    /// </summary>
    public static void DeleteByUrl(IWebHostEnvironment env, string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith('/')) return;
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(env.WebRootPath, relative));
        var root = Path.GetFullPath(env.WebRootPath);
        if (!path.StartsWith(root + Path.DirectorySeparatorChar)) return;
        if (File.Exists(path)) File.Delete(path);
    }
}
