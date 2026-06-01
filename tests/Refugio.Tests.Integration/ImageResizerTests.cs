using System.Buffers.Binary;
using Refugio.Web.Helpers;

namespace Refugio.Tests.Integration;

public class ImageResizerTests
{
    // Minimal valid 1x1 RGB PNG (colour type 2), same bytes the logo upload test uses.
    private static readonly byte[] OnePixelPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
        0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC,
        0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82
    ];

    private static byte[] RunResize(byte[] sourcePng)
    {
        var path = Path.Combine(Path.GetTempPath(), $"logo-{Guid.NewGuid():N}.png");
        try
        {
            using var input = new MemoryStream(sourcePng);
            ImageResizer.SaveSquareContainPng(input, path, 100);
            return File.ReadAllBytes(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static (int W, int H) ReadPngSize(byte[] png)
    {
        ReadOnlySpan<byte> sig = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(png.AsSpan(0, 8).SequenceEqual(sig), "Output is not a PNG.");
        // IHDR data starts at offset 16 (8 sig + 4 len + 4 type).
        var w = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var h = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        Assert.Equal(8, png[24]);  // bit depth
        Assert.Equal(6, png[25]);  // colour type RGBA
        return (w, h);
    }

    [Fact]
    public void Resize_RgbSource_ProducesValid100x100Png()
    {
        var output = RunResize(OnePixelPng);
        var (w, h) = ReadPngSize(output);
        Assert.Equal(100, w);
        Assert.Equal(100, h);
        Assert.True(output.Length > 100, "Output PNG looks empty.");
    }

    [Fact]
    public void Resize_CanDecodeItsOwnRgbaOutput()
    {
        // First pass yields a 100x100 RGBA (colour type 6) PNG; feeding it back proves the
        // decoder handles RGBA + the encoder's output round-trips.
        var first = RunResize(OnePixelPng);
        var second = RunResize(first);
        var (w, h) = ReadPngSize(second);
        Assert.Equal(100, w);
        Assert.Equal(100, h);
    }
}
