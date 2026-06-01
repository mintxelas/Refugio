using System.Buffers.Binary;
using System.IO.Compression;

namespace Refugio.Web.Helpers;

/// <summary>
/// Minimal, fully-managed PNG pipeline (no external library, Linux-compatible).
/// Decodes an 8-bit PNG, scales it without distortion so its LONGEST side reaches
/// <c>size</c>, centers it in a <c>size</c>×<c>size</c> canvas, fills the remaining
/// area with transparency, and re-encodes a 32-bit RGBA PNG.
///
/// Supports PNG colour types 0/2/3/4/6 at bit depth 8, non-interlaced — which covers
/// the logos this app accepts. Anything else throws (the caller treats that as an
/// invalid upload). JPEG/WebP are intentionally unsupported: the BCL has no
/// cross-platform decoder for them.
/// </summary>
public static class ImageResizer
{
    private const byte SigByte0 = 0x89;
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static void SaveSquareContainPng(Stream source, string destPath, int size = 100)
    {
        using var ms = new MemoryStream();
        source.CopyTo(ms);
        var png = DecodePng(ms.ToArray(), out var sw, out var sh);

        // "Contain" scale: longest side reaches the target; never distort.
        var scale = Math.Min((double)size / sw, (double)size / sh);
        var dw = Math.Max(1, (int)Math.Round(sw * scale));
        var dh = Math.Max(1, (int)Math.Round(sh * scale));
        dw = Math.Min(dw, size);
        dh = Math.Min(dh, size);

        var scaled = ResizeBilinear(png, sw, sh, dw, dh);

        // Transparent square canvas (all zero = transparent black), scaled image centered.
        var canvas = new byte[size * size * 4];
        var offX = (size - dw) / 2;
        var offY = (size - dh) / 2;
        for (var y = 0; y < dh; y++)
        {
            var srcRow = y * dw * 4;
            var dstRow = ((offY + y) * size + offX) * 4;
            Array.Copy(scaled, srcRow, canvas, dstRow, dw * 4);
        }

        File.WriteAllBytes(destPath, EncodePng(size, size, canvas));
    }

    // ── Decode ──────────────────────────────────────────────────────────────

    private static byte[] DecodePng(byte[] data, out int width, out int height)
    {
        if (data.Length < 8 || data[0] != SigByte0 || !data.AsSpan(0, 8).SequenceEqual(Signature))
            throw new InvalidDataException("Not a PNG.");

        int w = 0, h = 0, bitDepth = 0, colorType = 0, interlace = 0;
        byte[]? palette = null, trns = null;
        using var idat = new MemoryStream();

        var pos = 8;
        while (pos + 8 <= data.Length)
        {
            var len = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(pos, 4));
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            var dataStart = pos + 8;
            if (len < 0 || dataStart + len + 4 > data.Length) throw new InvalidDataException("Truncated PNG.");
            var chunk = data.AsSpan(dataStart, len);

            switch (type)
            {
                case "IHDR":
                    w = BinaryPrimitives.ReadInt32BigEndian(chunk[..4]);
                    h = BinaryPrimitives.ReadInt32BigEndian(chunk.Slice(4, 4));
                    bitDepth = chunk[8];
                    colorType = chunk[9];
                    interlace = chunk[12];
                    break;
                case "PLTE":
                    palette = chunk.ToArray();
                    break;
                case "tRNS":
                    trns = chunk.ToArray();
                    break;
                case "IDAT":
                    idat.Write(chunk);
                    break;
                case "IEND":
                    pos = data.Length; // stop
                    continue;
            }
            pos = dataStart + len + 4; // skip data + CRC
        }

        if (w <= 0 || h <= 0 || w > 10000 || h > 10000) throw new InvalidDataException("Bad PNG dimensions.");
        if (bitDepth != 8) throw new NotSupportedException("Only 8-bit PNG supported.");
        if (interlace != 0) throw new NotSupportedException("Interlaced PNG not supported.");

        var channels = colorType switch
        {
            0 => 1, // grayscale
            2 => 3, // RGB
            3 => 1, // indexed
            4 => 2, // grayscale + alpha
            6 => 4, // RGBA
            _ => throw new NotSupportedException($"PNG colour type {colorType} not supported.")
        };

        // Inflate the zlib stream. PNG IDAT is zlib-wrapped: 2-byte header, raw DEFLATE,
        // then a 4-byte Adler-32. Skip the header and inflate the DEFLATE body directly.
        var idatBytes = idat.ToArray();
        if (idatBytes.Length < 2) throw new InvalidDataException("Empty PNG data.");
        using var inflated = new MemoryStream();
        using (var body = new MemoryStream(idatBytes, 2, idatBytes.Length - 2))
        using (var z = new DeflateStream(body, CompressionMode.Decompress))
            z.CopyTo(inflated);
        var raw = inflated.ToArray();

        var stride = w * channels;
        var expected = h * (stride + 1);
        if (raw.Length < expected) throw new InvalidDataException("PNG data underrun.");

        var samples = Unfilter(raw, w, h, channels);
        width = w;
        height = h;
        return ToRgba(samples, w, h, colorType, channels, palette, trns);
    }

    private static byte[] Unfilter(byte[] raw, int w, int h, int channels)
    {
        var stride = w * channels;
        var bpp = channels; // 8-bit: one byte per sample
        var outBytes = new byte[h * stride];
        var rawPos = 0;
        for (var y = 0; y < h; y++)
        {
            var filter = raw[rawPos++];
            var outRow = y * stride;
            var prevRow = outRow - stride;
            for (var i = 0; i < stride; i++)
            {
                int x = raw[rawPos++];
                int a = i >= bpp ? outBytes[outRow + i - bpp] : 0;
                int b = y > 0 ? outBytes[prevRow + i] : 0;
                int c = (y > 0 && i >= bpp) ? outBytes[prevRow + i - bpp] : 0;
                int val = filter switch
                {
                    0 => x,
                    1 => x + a,
                    2 => x + b,
                    3 => x + ((a + b) >> 1),
                    4 => x + Paeth(a, b, c),
                    _ => throw new InvalidDataException("Bad PNG filter.")
                };
                outBytes[outRow + i] = (byte)(val & 0xFF);
            }
        }
        return outBytes;
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static byte[] ToRgba(byte[] s, int w, int h, int colorType, int channels, byte[]? palette, byte[]? trns)
    {
        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            var si = i * channels;
            var di = i * 4;
            byte r, g, b, a;
            switch (colorType)
            {
                case 0: // grayscale
                    r = g = b = s[si]; a = 255; break;
                case 2: // RGB
                    r = s[si]; g = s[si + 1]; b = s[si + 2]; a = 255; break;
                case 4: // grayscale + alpha
                    r = g = b = s[si]; a = s[si + 1]; break;
                case 6: // RGBA
                    r = s[si]; g = s[si + 1]; b = s[si + 2]; a = s[si + 3]; break;
                case 3: // indexed
                    var idx = s[si];
                    if (palette is null || idx * 3 + 2 >= palette.Length) throw new InvalidDataException("Missing PLTE.");
                    r = palette[idx * 3]; g = palette[idx * 3 + 1]; b = palette[idx * 3 + 2];
                    a = trns is not null && idx < trns.Length ? trns[idx] : (byte)255;
                    break;
                default:
                    throw new NotSupportedException();
            }
            rgba[di] = r; rgba[di + 1] = g; rgba[di + 2] = b; rgba[di + 3] = a;
        }
        return rgba;
    }

    // ── Scale ───────────────────────────────────────────────────────────────

    private static byte[] ResizeBilinear(byte[] src, int sw, int sh, int dw, int dh)
    {
        if (sw == dw && sh == dh) return src;
        var dst = new byte[dw * dh * 4];
        for (var y = 0; y < dh; y++)
        {
            var gy = (y + 0.5) * sh / dh - 0.5;
            var y0 = (int)Math.Floor(gy);
            var fy = gy - y0;
            var y0c = Math.Clamp(y0, 0, sh - 1);
            var y1c = Math.Clamp(y0 + 1, 0, sh - 1);
            for (var x = 0; x < dw; x++)
            {
                var gx = (x + 0.5) * sw / dw - 0.5;
                var x0 = (int)Math.Floor(gx);
                var fx = gx - x0;
                var x0c = Math.Clamp(x0, 0, sw - 1);
                var x1c = Math.Clamp(x0 + 1, 0, sw - 1);

                var di = (y * dw + x) * 4;
                for (var ch = 0; ch < 4; ch++)
                {
                    var p00 = src[(y0c * sw + x0c) * 4 + ch];
                    var p10 = src[(y0c * sw + x1c) * 4 + ch];
                    var p01 = src[(y1c * sw + x0c) * 4 + ch];
                    var p11 = src[(y1c * sw + x1c) * 4 + ch];
                    var top = p00 + (p10 - p00) * fx;
                    var bot = p01 + (p11 - p01) * fx;
                    dst[di + ch] = (byte)Math.Clamp(Math.Round(top + (bot - top) * fy), 0, 255);
                }
            }
        }
        return dst;
    }

    // ── Encode ──────────────────────────────────────────────────────────────

    private static byte[] EncodePng(int w, int h, byte[] rgba)
    {
        using var ms = new MemoryStream();
        ms.Write(Signature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[..4], w);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(4, 4), h);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 6;  // colour type RGBA
        ihdr[10] = 0; // compression
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // interlace
        WriteChunk(ms, "IHDR", ihdr);

        // Raw scanlines, each prefixed with filter byte 0 (None).
        var stride = w * 4;
        var raw = new byte[h * (stride + 1)];
        for (var y = 0; y < h; y++)
        {
            raw[y * (stride + 1)] = 0;
            Array.Copy(rgba, y * stride, raw, y * (stride + 1) + 1, stride);
        }

        WriteChunk(ms, "IDAT", ZlibCompress(raw));

        WriteChunk(ms, "IEND", ReadOnlySpan<byte>.Empty);
        return ms.ToArray();
    }

    // Wrap raw bytes in a zlib stream by hand (DEFLATE body + zlib header + Adler-32),
    // so we depend only on DeflateStream (no ZLibStream).
    private static byte[] ZlibCompress(byte[] raw)
    {
        using var comp = new MemoryStream();
        using (var d = new DeflateStream(comp, CompressionLevel.SmallestSize, leaveOpen: true))
            d.Write(raw, 0, raw.Length);
        var deflated = comp.ToArray();

        var outBytes = new byte[2 + deflated.Length + 4];
        outBytes[0] = 0x78; // zlib: CM=8, CINFO=7 (32K window)
        outBytes[1] = 0x9C; // default compression level, header checksum-valid
        Array.Copy(deflated, 0, outBytes, 2, deflated.Length);
        var adler = Adler32(raw);
        BinaryPrimitives.WriteUInt32BigEndian(outBytes.AsSpan(2 + deflated.Length, 4), adler);
        return outBytes;
    }

    private static uint Adler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1, b = 0;
        foreach (var x in data)
        {
            a = (a + x) % mod;
            b = (b + a) % mod;
        }
        return (b << 16) | a;
    }

    private static void WriteChunk(Stream s, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[8];
        BinaryPrimitives.WriteInt32BigEndian(header[..4], data.Length);
        for (var i = 0; i < 4; i++) header[4 + i] = (byte)type[i];
        s.Write(header);
        s.Write(data);

        var crc = Crc32(header.Slice(4, 4), data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        s.Write(crcBytes);
    }

    // ── CRC-32 (PNG / zlib polynomial) ───────────────────────────────────────

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in type) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        foreach (var b in data) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }
}
