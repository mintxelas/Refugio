using Microsoft.AspNetCore.Http;
using Refugio.Web.Helpers;

namespace Refugio.Tests.Integration;

public class PhotoFilesTests
{
    private static IFormFile File(byte[] bytes)
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
        Assert.True(PhotoFiles.HasImageBytes(File([0xFF, 0xD8, 0xFF, 0xE0])));                          // JPEG
        Assert.True(PhotoFiles.HasImageBytes(File([0x89, 0x50, 0x4E, 0x47])));                          // PNG
        Assert.True(PhotoFiles.HasImageBytes(File([(byte)'G', (byte)'I', (byte)'F', (byte)'8'])));      // GIF
        Assert.True(PhotoFiles.HasImageBytes(File([(byte)'B', (byte)'M', 0x00, 0x00])));                // BMP
        Assert.True(PhotoFiles.HasImageBytes(File(
            [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P']))); // WEBP
    }

    [Fact]
    public void HasImageBytes_RejectsNonImage() =>
        Assert.False(PhotoFiles.HasImageBytes(File([0x00, 0x01, 0x02, 0x03])));
}
