using Microsoft.AspNetCore.Http;
using Refugio.Web.Helpers;

namespace Refugio.Tests.Integration;

public class PhotoFilesTests
{
    private static IFormFile Fake(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "Photo", "photo");
    }

    [Theory]
    [InlineData(".jpg", true)]
    [InlineData(".jpeg", true)]
    [InlineData(".png", true)]
    [InlineData(".gif", true)]
    [InlineData(".webp", true)]
    [InlineData(".bmp", true)]
    [InlineData(".txt", false)]
    [InlineData(".svg", false)]
    public void IsImageExtension_RecognisesRasterFormats(string ext, bool expected) =>
        Assert.Equal(expected, PhotoFiles.IsImageExtension(ext));

    [Fact]
    public void HasImageBytes_AcceptsJpegPngGifWebpBmp()
    {
        Assert.True(PhotoFiles.HasImageBytes(Fake([0xFF, 0xD8, 0xFF, 0xE0])));                          // JPEG
        Assert.True(PhotoFiles.HasImageBytes(Fake([0x89, 0x50, 0x4E, 0x47])));                          // PNG
        Assert.True(PhotoFiles.HasImageBytes(Fake([(byte)'G', (byte)'I', (byte)'F', (byte)'8'])));      // GIF
        Assert.True(PhotoFiles.HasImageBytes(Fake([(byte)'B', (byte)'M', 0x00, 0x00])));                // BMP
        Assert.True(PhotoFiles.HasImageBytes(Fake(
            [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P']))); // WEBP
    }

    [Fact]
    public void HasImageBytes_RejectsNonImage() =>
        Assert.False(PhotoFiles.HasImageBytes(Fake([0x00, 0x01, 0x02, 0x03])));

    private static IFormFile NamedFile(byte[] bytes, string fileName)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "Photos", fileName);
    }

    private static FormFileCollection Files(params IFormFile[] files)
    {
        var collection = new FormFileCollection();
        collection.AddRange(files);
        return collection;
    }

    private static string TempDir() =>
        Path.Combine(Path.GetTempPath(), "refugio-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveGalleryUploads_SavesValidImagesAndReturnsTheirUrls()
    {
        var dir = TempDir();
        try
        {
            var files = Files(NamedFile([0x89, 0x50, 0x4E, 0x47], "a.png"));

            var urls = await PhotoFiles.SaveGalleryUploads(files, dir, "/photos/dogs/1");

            Assert.Single(urls);
            Assert.StartsWith("/photos/dogs/1/", urls[0]);
            Assert.EndsWith(".png", urls[0]);
            Assert.True(File.Exists(Path.Combine(dir, Path.GetFileName(urls[0]))));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveGalleryUploads_SkipsInvalidFilesButKeepsValidOnes()
    {
        var dir = TempDir();
        try
        {
            var files = Files(
                NamedFile([0x00, 0x01, 0x02, 0x03], "bad.png"),      // wrong magic bytes
                NamedFile([0x89, 0x50, 0x4E, 0x47], "good.txt"),     // wrong extension
                NamedFile([], "empty.png"),                          // empty
                NamedFile([0x89, 0x50, 0x4E, 0x47], "good.png"));    // valid

            var urls = await PhotoFiles.SaveGalleryUploads(files, dir, "/photos/dogs/1");

            Assert.Single(urls);
            Assert.EndsWith(".png", urls[0]);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveGalleryUploads_RejectsFilesOverMaxBytes()
    {
        var dir = TempDir();
        try
        {
            var files = Files(NamedFile([0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0], "big.png"));

            var urls = await PhotoFiles.SaveGalleryUploads(files, dir, "/photos/dogs/1", maxBytes: 4);

            Assert.Empty(urls);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SavePrimaryPhoto_SavesValidImageAsPrimaryAndReturnsUrl()
    {
        var dir = TempDir();
        try
        {
            var url = await PhotoFiles.SavePrimaryPhoto(NamedFile([0x89, 0x50, 0x4E, 0x47], "a.png"), dir, "/photos/dogs/1");

            Assert.Equal("/photos/dogs/1/primary.png", url);
            Assert.True(File.Exists(Path.Combine(dir, "primary.png")));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SavePrimaryPhoto_ReplacesAnyExistingPrimaryFile()
    {
        var dir = TempDir();
        try
        {
            Directory.CreateDirectory(dir);
            var stalePath = Path.Combine(dir, "primary.jpg");
            await File.WriteAllBytesAsync(stalePath, [0xFF, 0xD8, 0xFF]);

            var url = await PhotoFiles.SavePrimaryPhoto(NamedFile([0x89, 0x50, 0x4E, 0x47], "a.png"), dir, "/photos/dogs/1");

            Assert.Equal("/photos/dogs/1/primary.png", url);
            Assert.False(File.Exists(stalePath));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SavePrimaryPhoto_ReturnsNullForMissingFile()
    {
        var dir = TempDir();
        var url = await PhotoFiles.SavePrimaryPhoto(null, dir, "/photos/dogs/1");
        Assert.Null(url);
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public async Task SavePrimaryPhoto_RejectsInvalidExtensionAndBadMagicBytesAndOversizeFile()
    {
        var dir = TempDir();
        try
        {
            Assert.Null(await PhotoFiles.SavePrimaryPhoto(NamedFile([0x89, 0x50, 0x4E, 0x47], "a.txt"), dir, "/x"));
            Assert.Null(await PhotoFiles.SavePrimaryPhoto(NamedFile([0x00, 0x01, 0x02, 0x03], "a.png"), dir, "/x"));
            Assert.Null(await PhotoFiles.SavePrimaryPhoto(
                NamedFile([0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0], "a.png"), dir, "/x", maxBytes: 4));
            Assert.False(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
