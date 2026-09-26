namespace Refugio.Web.Helpers;

public static class PhotoFiles
{
    private static readonly string[] ImageExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    /// <summary>True when the extension is a recognised raster image format.</summary>
    public static bool IsImageExtension(string ext) => Array.IndexOf(ImageExtensions, ext) >= 0;

    /// <summary>True when the file's magic bytes match a known raster image format (any format).</summary>
    public static bool HasImageBytes(IFormFile file)
    {
        Span<byte> h = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(h);
        if (read < 4) return false;
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return true;                       // JPEG
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return true;       // PNG
        if (h[0] == (byte)'G' && h[1] == (byte)'I' && h[2] == (byte)'F') return true;        // GIF
        if (h[0] == (byte)'B' && h[1] == (byte)'M') return true;                             // BMP
        if (read >= 12 && h[0] == (byte)'R' && h[1] == (byte)'I' && h[2] == (byte)'F' && h[3] == (byte)'F'
            && h[8] == (byte)'W' && h[9] == (byte)'E' && h[10] == (byte)'B' && h[11] == (byte)'P') return true; // WEBP
        return false;
    }


    /// <summary>
    /// Validates and saves each file that passes the size/extension/magic-bytes checks into
    /// <paramref name="dir"/> (created if missing), under a random file name. Rejected files are
    /// skipped silently. Returns the public URL for each file actually saved.
    /// </summary>
    public static async Task<List<string>> SaveGalleryUploads(
        IFormFileCollection files, string dir, string urlPrefix, int maxBytes = 2 * 1024 * 1024)
    {
        Directory.CreateDirectory(dir);
        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > maxBytes) continue;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!IsImageExtension(ext)) continue;
            if (!HasImageBytes(file)) continue;
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = File.Create(Path.Combine(dir, fileName));
            await file.CopyToAsync(stream);
            urls.Add($"{urlPrefix}/{fileName}");
        }
        return urls;
    }

    /// <summary>
    /// Validates and saves a single replacing "primary" photo (e.g. a dog's or volunteer's main
    /// picture) into <paramref name="dir"/> (created if missing), deleting any existing
    /// <c>primary.*</c> file first. Returns null without writing anything when the file is
    /// missing, empty, too large, or fails the extension/magic-bytes checks.
    /// </summary>
    public static async Task<string?> SavePrimaryPhoto(
        IFormFile? file, string dir, string urlPrefix, int maxBytes = 2 * 1024 * 1024)
    {
        if (file is null || file.Length == 0 || file.Length > maxBytes) return null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsImageExtension(ext)) return null;
        if (!HasImageBytes(file)) return null;

        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, "primary.*")) File.Delete(old);
        var fileName = $"primary{ext}";
        await using var stream = File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);
        return $"{urlPrefix}/{fileName}";
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
