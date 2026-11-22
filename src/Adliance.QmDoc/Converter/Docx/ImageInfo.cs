using System;
using System.Buffers.Binary;
using DocumentFormat.OpenXml.Packaging;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Reads the type and pixel size of PNG, JPEG and GIF images from their header, without any image library.
/// </summary>
internal sealed record ImageInfo(ImagePartType PartType, int Width, int Height)
{
    public static ImageInfo? Read(byte[] data)
    {
        try
        {
            return ReadPng(data) ?? ReadGif(data) ?? ReadJpeg(data);
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static ImageInfo? ReadPng(byte[] data)
    {
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (data.Length < 24 || !data.AsSpan(0, 8).SequenceEqual(signature)) return null;

        // the IHDR chunk always comes first
        return new ImageInfo(ImagePartType.Png,
            BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4)));
    }

    private static ImageInfo? ReadGif(byte[] data)
    {
        if (data.Length < 10 || data[0] != 'G' || data[1] != 'I' || data[2] != 'F') return null;
        return new ImageInfo(ImagePartType.Gif,
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(8, 2)));
    }

    private static ImageInfo? ReadJpeg(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) return null;

        var position = 2;
        while (position + 9 < data.Length)
        {
            if (data[position] != 0xFF)
            {
                position++;
                continue;
            }

            var marker = data[position + 1];
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7) or 0xFF)
            {
                position += marker == 0xFF ? 1 : 2;
                continue;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 2, 2));

            // start of frame markers (except DHT, JPG and DAC) contain the size
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return new ImageInfo(ImagePartType.Jpeg,
                    BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 7, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 5, 2)));
            }

            position += 2 + length;
        }

        return null;
    }
}
