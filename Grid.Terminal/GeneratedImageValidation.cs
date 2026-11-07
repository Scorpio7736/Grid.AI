using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Grid.Terminal;

public static class GeneratedImageValidation
{
    public static GeneratedArtifact ValidatePng(byte[] bytes)
    {
        if (bytes.Length is < 45 or > 1024 * 1024 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw new InvalidDataException("Choose a PNG of at most 1 MiB.");
        var width = 0; var height = 0; var channels = 0; var offset = 8; var ended = false;
        using var compressed = new MemoryStream();
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12) throw new InvalidDataException("Truncated PNG chunk.");
            var size = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (size > bytes.Length - offset - 12) throw new InvalidDataException("Invalid PNG chunk size.");
            var length = (int)size;
            var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var payload = bytes.AsSpan(offset + 8, length);
            if (Crc(bytes.AsSpan(offset + 4, length + 4)) != BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + length, 4)))
                throw new InvalidDataException("PNG checksum failed.");
            switch (type)
            {
                case "IHDR":
                    if (offset != 8 || length != 13) throw new InvalidDataException("Invalid PNG header.");
                    width = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(payload[..4]), int.MaxValue);
                    height = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(4, 4)), int.MaxValue);
                    if (width is < 1 or > 2048 || height is < 1 or > 2048 || payload[8] != 8 || payload[9] is not (2 or 6) ||
                        payload[10] != 0 || payload[11] != 0 || payload[12] != 0)
                        throw new InvalidDataException("PNG output requires 8-bit RGB/RGBA, at most 2048 by 2048, without interlacing.");
                    channels = payload[9] == 2 ? 3 : 4;
                    break;
                case "IDAT":
                    if (channels == 0 || ended) throw new InvalidDataException("Invalid PNG data order.");
                    compressed.Write(payload);
                    break;
                case "IEND":
                    if (length != 0 || compressed.Length == 0 || offset + 12 != bytes.Length) throw new InvalidDataException("Invalid PNG end.");
                    ended = true;
                    break;
                default:
                    // Fixed output subset keeps metadata, external profiles and animation outside this template.
                    throw new InvalidDataException("PNG output supports only IHDR, IDAT and IEND chunks. Re-export a plain RGB/RGBA PNG.");
            }
            offset += length + 12;
        }
        if (!ended) throw new InvalidDataException("PNG end marker missing.");
        var stride = checked(width * channels);
        compressed.Position = 0;
        using var decoder = new ZLibStream(compressed, CompressionMode.Decompress);
        var previous = new byte[stride]; var row = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var filter = decoder.ReadByte();
            if (filter is < 0 or > 4) throw new InvalidDataException("Invalid PNG scanline filter.");
            decoder.ReadExactly(row);
            for (var x = 0; x < stride; x++)
            {
                var left = x >= channels ? row[x - channels] : 0;
                var up = previous[x];
                var upperLeft = x >= channels ? previous[x - channels] : 0;
                var prediction = filter switch { 0 => 0, 1 => left, 2 => up, 3 => (left + up) / 2, _ => Paeth(left, up, upperLeft) };
                row[x] = unchecked((byte)(row[x] + prediction));
            }
            (row, previous) = (previous, row);
        }
        if (decoder.ReadByte() != -1) throw new InvalidDataException("Excess PNG pixel data.");
        return new(bytes.ToArray(), "<!doctype html><meta charset=utf-8><title>GRID image preview</title><p>" + width + " × " + height +
            " pixels</p><img alt='Validated PNG output' src='candidate.png'>");
    }

    private static int Paeth(int left, int up, int upperLeft)
    {
        var prediction = left + up - upperLeft;
        var a = Math.Abs(prediction - left); var b = Math.Abs(prediction - up); var c = Math.Abs(prediction - upperLeft);
        return a <= b && a <= c ? left : b <= c ? up : upperLeft;
    }

    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0U : 0xedb88320U);
        }
        return ~crc;
    }
}
