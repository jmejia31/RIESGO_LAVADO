using System.IO.Compression;
using System.Buffers.Binary;
using System.Xml;
using Microsoft.AspNetCore.Http;

namespace RL.API.Infrastructure.Files;

public sealed record FileUploadPolicy(
    long MaxBytes,
    IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedMimeTypes);

public static class FileUploadPolicies
{
    public const long ListImportMaxBytes = 128L * 1024 * 1024;
    public const long EvidenceMaxBytes = 10L * 1024 * 1024;

    public static FileUploadPolicy Evidence(long? maxBytes = null,
        IReadOnlyDictionary<string, string[]>? configuredMimeTypes = null)
    {
        var mimeTypes = configuredMimeTypes ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg", "image/pjpeg"],
            [".jpeg"] = ["image/jpeg", "image/pjpeg"],
            [".doc"] = ["application/msword", "application/octet-stream"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream"],
            [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream"]
        };

        return CreatePolicy(maxBytes ?? EvidenceMaxBytes, mimeTypes);
    }

    public static FileUploadPolicy ListImport(long? maxBytes = null) => CreatePolicy(
        maxBytes ?? ListImportMaxBytes,
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel", "application/octet-stream"],
            [".xml"] = ["application/xml", "text/xml", "application/octet-stream"],
            [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream"]
        });

    public static FileUploadPolicy Images(long? maxBytes = null) => CreatePolicy(
        maxBytes ?? EvidenceMaxBytes,
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg", "image/pjpeg"],
            [".jpeg"] = ["image/jpeg", "image/pjpeg"],
            [".png"] = ["image/png"],
            [".gif"] = ["image/gif"],
            [".webp"] = ["image/webp"]
        });

    private static FileUploadPolicy CreatePolicy(long maxBytes, IReadOnlyDictionary<string, string[]> mimeTypes) => new(
        maxBytes,
        mimeTypes
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is { Length: > 0 })
            .ToDictionary(
                pair => NormalizeExtension(pair.Key),
                pair => (IReadOnlySet<string>)new HashSet<string>(pair.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase));

    internal static string NormalizeExtension(string extension) =>
        extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
}

public static class FileUploadPolicyValidator
{
    public static string? Validate(IFormFile? file, FileUploadPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (file is null || file.Length <= 0)
            return "El archivo está vacío o no fue seleccionado.";
        if (policy.MaxBytes <= 0 || file.Length > policy.MaxBytes)
            return "El archivo supera el límite de tamaño permitido.";

        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeName)
            || !string.Equals(safeName, file.FileName, StringComparison.Ordinal)
            || file.FileName.IndexOfAny(['/', '\\']) >= 0
            || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "El nombre del archivo no es válido.";

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        if (!policy.AllowedMimeTypes.TryGetValue(extension, out var allowedMimeTypes))
            return "La extensión del archivo no está permitida.";

        var mimeType = file.ContentType?.Trim();
        if (string.IsNullOrWhiteSpace(mimeType) || !allowedMimeTypes.Contains(mimeType))
            return "El tipo de contenido del archivo no está permitido.";

        return ValidateContent(file, extension);
    }

    public static string? ResolveSafePath(string rootDirectory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory) || string.IsNullOrWhiteSpace(fileName))
            return null;

        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal)
            || fileName.IndexOfAny(['/', '\\']) >= 0
            || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return null;

        var root = Path.GetFullPath(rootDirectory);
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, safeName));
        return path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ? path : null;
    }

    private static string? ValidateContent(IFormFile file, string extension)
    {
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        stream.Position = 0;

        var valid = extension switch
        {
            ".pdf" => StartsWith(header[..read], [0x25, 0x50, 0x44, 0x46]),
            ".png" => HasPngStructure(stream, header[..read]),
            ".jpg" or ".jpeg" => HasJpegStructure(stream, header[..read]),
            ".gif" => HasGifStructure(stream, header[..read]),
            ".webp" => HasWebpStructure(stream, header[..read]),
            ".doc" or ".xls" => StartsWith(header[..read], [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]),
            ".docx" => HasOpenXmlStructure(stream, "word/document.xml"),
            ".xlsx" => HasOpenXmlStructure(stream, "xl/workbook.xml"),
            ".xml" => IsSafeXml(stream),
            ".csv" => IsTextData(stream),
            _ => false
        };

        return valid ? null : "El contenido del archivo no coincide con el formato declarado.";
    }

    private static bool StartsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> signature) =>
        source.Length >= signature.Length && source[..signature.Length].SequenceEqual(signature);

    private static bool HasOpenXmlStructure(Stream stream, string expectedEntry)
    {
        try
        {
            using var bufferedStream = new MemoryStream();
            stream.CopyTo(bufferedStream);
            bufferedStream.Position = 0;
            using var archive = new ZipArchive(bufferedStream, ZipArchiveMode.Read, leaveOpen: true);
            var contentTypes = archive.GetEntry("[Content_Types].xml");
            var document = archive.GetEntry(expectedEntry);
            return contentTypes is not null
                && document is not null
                && IsSafeXmlEntry(contentTypes)
                && IsSafeXmlEntry(document);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool IsSafeXmlEntry(ZipArchiveEntry entry)
    {
        const long MaxXmlEntryBytes = 1024 * 1024;
        if (entry.Length <= 0 || entry.Length > MaxXmlEntryBytes) return false;

        try
        {
            using var stream = entry.Open();
            return IsSafeXml(stream);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            return false;
        }
    }

    private static bool IsSafeXml(Stream stream)
    {
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(stream, settings);
            while (reader.Read()) { }
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static bool IsTextData(Stream stream)
    {
        var buffer = new byte[8192];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var index = 0; index < read; index++)
            {
                var value = buffer[index];
                if (value == 0 || (value < 0x20 && value is not (0x09 or 0x0A or 0x0D)))
                    return false;
            }
        }

        return true;
    }

    private static bool HasPngStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (!StartsWith(header, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return false;

        var bytes = ReadAllBytes(stream);
        if (bytes.Length < 45) return false;

        var offset = 8;
        var sawHeader = false;
        var sawPalette = false;
        var sawImageData = false;
        var imageDataEnded = false;
        var sawEnd = false;
        uint width = 0;
        uint height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        byte interlace = 0;
        using var compressedImageData = new MemoryStream();

        while (offset <= bytes.Length - 12)
        {
            var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            var chunkType = bytes.AsSpan(offset + 4, 4);
            var dataOffset = offset + 8;
            var dataLength = (long)chunkLength;
            var crcOffset = dataOffset + dataLength;
            if (crcOffset + 4 > bytes.Length || chunkLength > int.MaxValue) return false;

            var chunkData = bytes.AsSpan(dataOffset, (int)chunkLength);
            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan((int)crcOffset, 4));
            if (PngCrc32(bytes.AsSpan(offset + 4, 4 + (int)chunkLength)) != expectedCrc) return false;

            if (!sawHeader)
            {
                if (!chunkType.SequenceEqual("IHDR"u8) || chunkLength != 13) return false;
                width = BinaryPrimitives.ReadUInt32BigEndian(chunkData[..4]);
                height = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(4, 4));
                bitDepth = chunkData[8];
                colorType = chunkData[9];
                interlace = chunkData[12];
                if (!IsValidPngHeader(width, height, bitDepth, colorType, chunkData[10], chunkData[11], interlace)) return false;
                sawHeader = true;
            }
            else if (chunkType.SequenceEqual("IHDR"u8))
            {
                return false;
            }
            else if (chunkType.SequenceEqual("IDAT"u8))
            {
                if (imageDataEnded) return false;
                compressedImageData.Write(chunkData);
                sawImageData = true;
            }
            else if (chunkType.SequenceEqual("PLTE"u8))
            {
                if (sawPalette || sawImageData || chunkLength is 0 or > 768 || chunkLength % 3 != 0
                    || colorType is 0 or 4) return false;
                sawPalette = true;
            }
            else if (chunkType.SequenceEqual("IEND"u8))
            {
                if (chunkLength != 0 || !sawImageData || (colorType == 3 && !sawPalette)
                    || compressedImageData.Length == 0 || crcOffset + 4 != bytes.Length) return false;
                sawEnd = true;
                break;
            }
            else
            {
                if (sawImageData) imageDataEnded = true;
                // Unknown critical chunks are not safe to interpret as a PNG image.
                if (chunkType[0] is >= (byte)'A' and <= (byte)'Z'
                    && !chunkType.SequenceEqual("PLTE"u8)) return false;
            }

            offset = (int)crcOffset + 4;
        }

        if (!sawEnd || !sawHeader) return false;
        compressedImageData.Position = 0;
        return ValidatePngImageData(compressedImageData, width, height, bitDepth, colorType, interlace);
    }

    private static bool HasJpegStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (!StartsWith(header, [0xFF, 0xD8, 0xFF])) return false;
        var bytes = ReadAllBytes(stream);
        if (bytes.Length < 12 || bytes[0] != 0xFF || bytes[1] != 0xD8) return false;

        var offset = 2;
        var sawFrame = false;
        var sawScan = false;
        var sawScanData = false;
        var sawQuantizationTable = false;
        var sawEntropyTable = false;
        while (offset < bytes.Length)
        {
            if (bytes[offset++] != 0xFF) return false;
            while (offset < bytes.Length && bytes[offset] == 0xFF) offset++;
            if (offset >= bytes.Length) return false;

            var marker = bytes[offset++];
            if (marker == 0xD9)
                return sawFrame && sawScan && sawScanData && sawQuantizationTable
                    && sawEntropyTable && offset == bytes.Length;
            if (marker == 0xD8 || marker == 0x00 || marker is >= 0xD0 and <= 0xD7)
                return false;
            if (offset > bytes.Length - 2) return false;

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length) return false;
            var segment = bytes.AsSpan(offset + 2, segmentLength - 2);

            if (marker == 0xDB)
            {
                if (segment.Length < 65) return false;
                sawQuantizationTable = true;
            }
            else if (marker == 0xC4)
            {
                if (segment.Length < 18) return false;
                sawEntropyTable = true;
            }
            else if (marker == 0xCC)
            {
                if (segment.Length < 2) return false;
                sawEntropyTable = true;
            }

            if (IsJpegFrameMarker(marker))
            {
                if (segment.Length < 6) return false;
                var componentCount = segment[5];
                if (segment.Length != 6 + componentCount * 3
                    || BinaryPrimitives.ReadUInt16BigEndian(segment.Slice(1, 2)) == 0
                    || BinaryPrimitives.ReadUInt16BigEndian(segment.Slice(3, 2)) == 0
                    || componentCount == 0) return false;
                sawFrame = true;
            }

            offset += segmentLength;
            if (marker != 0xDA) continue;
            if (!sawFrame || !sawQuantizationTable || !sawEntropyTable || segment.Length < 6) return false;
            var scanComponentCount = segment[0];
            if (scanComponentCount == 0 || segment.Length != 1 + scanComponentCount * 2 + 3) return false;
            sawScan = true;
            var scanHasData = false;
            while (offset < bytes.Length)
            {
                if (bytes[offset++] != 0xFF)
                {
                    scanHasData = true;
                    continue;
                }

                if (offset >= bytes.Length) return false;
                var next = bytes[offset];
                if (next == 0x00 || next is >= 0xD0 and <= 0xD7)
                {
                    offset++;
                    scanHasData = true;
                    continue;
                }

                offset--;
                break;
            }
            if (!scanHasData) return false;
            sawScanData = true;
        }

        return false;
    }

    private static bool HasGifStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (!StartsWith(header, "GIF87a"u8) && !StartsWith(header, "GIF89a"u8)) return false;
        var bytes = ReadAllBytes(stream);
        if (bytes.Length < 14) return false;

        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2)) == 0
            || BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2)) == 0) return false;
        var packed = bytes[10];
        var offset = 13;
        if ((packed & 0x80) != 0)
            offset += 3 * (1 << ((packed & 0x07) + 1));
        if (offset > bytes.Length) return false;

        var sawImage = false;
        while (offset < bytes.Length)
        {
            var block = bytes[offset++];
            if (block == 0x3B) return sawImage && offset == bytes.Length;
            if (block == 0x21)
            {
                if (offset >= bytes.Length) return false;
                offset++;
                if (!SkipGifSubBlocks(bytes, ref offset, out _)) return false;
                continue;
            }
            if (block != 0x2C || offset > bytes.Length - 9) return false;

            var imageWidth = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 4, 2));
            var imageHeight = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 6, 2));
            var imagePacked = bytes[offset + 8];
            if (imageWidth == 0 || imageHeight == 0) return false;
            offset += 9;
            if ((imagePacked & 0x80) != 0)
                offset += 3 * (1 << ((imagePacked & 0x07) + 1));
            if (offset >= bytes.Length) return false;

            var lzwMinimumCodeSize = bytes[offset++];
            if (lzwMinimumCodeSize is < 2 or > 8) return false;
            if (!SkipGifSubBlocks(bytes, ref offset, out var imageDataLength) || imageDataLength == 0) return false;
            sawImage = true;
        }

        return false;
    }

    private static bool HasWebpStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (header.Length < 12 || !header[..4].SequenceEqual("RIFF"u8) || !header[8..12].SequenceEqual("WEBP"u8)) return false;
        var bytes = ReadAllBytes(stream);
        if (bytes.Length < 26 || !bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            || !bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)) + 8L != bytes.Length) return false;

        var offset = 12;
        var sawImageChunk = false;
        var sawExtendedHeader = false;
        while (offset < bytes.Length)
        {
            if (offset > bytes.Length - 8) return false;
            var chunkType = bytes.AsSpan(offset, 4);
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4));
            var chunkDataOffset = offset + 8;
            var chunkEnd = (long)chunkDataOffset + chunkLength;
            if (chunkEnd > bytes.Length) return false;
            var data = bytes.AsSpan(chunkDataOffset, (int)chunkLength);

            if (chunkType.SequenceEqual("VP8 "u8))
            {
                if (sawImageChunk || data.Length < 10 || !data.Slice(3, 3).SequenceEqual(new byte[] { 0x9D, 0x01, 0x2A })) return false;
                var width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2)) & 0x3FFF;
                var height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2)) & 0x3FFF;
                if (width == 0 || height == 0) return false;
                sawImageChunk = true;
            }
            else if (chunkType.SequenceEqual("VP8L"u8))
            {
                if (sawImageChunk || data.Length < 6 || data[0] != 0x2F) return false;
                var dimensions = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(1, 4));
                if ((dimensions & 0x3FFF) == 0x3FFF || ((dimensions >> 14) & 0x3FFF) == 0x3FFF) return false;
                sawImageChunk = true;
            }
            else if (chunkType.SequenceEqual("VP8X"u8))
            {
                if (offset != 12 || sawExtendedHeader || data.Length != 10) return false;
                sawExtendedHeader = true;
            }

            offset = (int)chunkEnd + ((chunkLength & 1) == 1 ? 1 : 0);
            if (offset > bytes.Length) return false;
        }

        return sawImageChunk && offset == bytes.Length;
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        stream.Position = 0;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static bool IsValidPngHeader(uint width, uint height, byte bitDepth, byte colorType,
        byte compression, byte filter, byte interlace)
    {
        if (width == 0 || height == 0 || (ulong)width * height > 40_000_000UL
            || compression != 0 || filter != 0 || interlace > 1) return false;

        return colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            2 => bitDepth is 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            4 or 6 => bitDepth is 8 or 16,
            _ => false
        };
    }

    private static bool ValidatePngImageData(Stream compressed, uint width, uint height, byte bitDepth, byte colorType, byte interlace)
    {
        try
        {
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress, leaveOpen: true);
            if (interlace == 1)
            {
                var interlacedChannels = colorType switch { 0 or 3 => 1, 2 => 3, 4 => 2, 6 => 4, _ => 0 };
                if (interlacedChannels == 0) return false;
                ReadOnlySpan<uint> xStarts = [0, 4, 0, 2, 0, 1, 0];
                ReadOnlySpan<uint> yStarts = [0, 0, 4, 0, 2, 0, 1];
                ReadOnlySpan<uint> xSteps = [8, 8, 4, 4, 2, 2, 1];
                ReadOnlySpan<uint> ySteps = [8, 8, 8, 4, 4, 2, 2];
                long total = 0;
                for (var pass = 0; pass < xStarts.Length; pass++)
                {
                    var passWidth = width <= xStarts[pass] ? 0 : (width - xStarts[pass] + xSteps[pass] - 1) / xSteps[pass];
                    var passHeight = height <= yStarts[pass] ? 0 : (height - yStarts[pass] + ySteps[pass] - 1) / ySteps[pass];
                    if (passWidth == 0 || passHeight == 0) continue;
                    var rowBytes = ((long)passWidth * interlacedChannels * bitDepth + 7) / 8;
                    if (rowBytes <= 0 || rowBytes > 64L * 1024 * 1024
                        || total + (rowBytes + 1) * passHeight > 128L * 1024 * 1024) return false;
                    total += (rowBytes + 1) * passHeight;
                    var interlacedRow = new byte[(int)rowBytes];
                    for (var y = 0u; y < passHeight; y++)
                    {
                        var filter = zlib.ReadByte();
                        if (filter is < 0 or > 4 || !ReadExactly(zlib, interlacedRow)) return false;
                    }
                }
                return total > 0 && zlib.ReadByte() == -1;
            }

            var channels = colorType switch { 0 or 3 => 1, 2 => 3, 4 => 2, 6 => 4, _ => 0 };
            var rowBytesLong = ((long)width * channels * bitDepth + 7) / 8;
            if (channels == 0 || rowBytesLong <= 0 || rowBytesLong > 64L * 1024 * 1024
                || rowBytesLong * height > 128L * 1024 * 1024) return false;
            var row = new byte[(int)rowBytesLong];
            for (var y = 0u; y < height; y++)
            {
                var filter = zlib.ReadByte();
                if (filter is < 0 or > 4 || !ReadExactly(zlib, row)) return false;
            }
            return zlib.ReadByte() == -1;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            return false;
        }
    }

    private static bool ReadExactly(Stream stream, Span<byte> destination)
    {
        var totalRead = 0;
        while (totalRead < destination.Length)
        {
            var read = stream.Read(destination[totalRead..]);
            if (read == 0) return false;
            totalRead += read;
        }
        return true;
    }

    private static uint PngCrc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }

    private static bool IsJpegFrameMarker(byte marker) =>
        marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF;

    private static bool SkipGifSubBlocks(byte[] bytes, ref int offset, out int totalLength)
    {
        totalLength = 0;
        while (offset < bytes.Length)
        {
            var length = bytes[offset++];
            if (length == 0) return true;
            if (offset > bytes.Length - length) return false;
            offset += length;
            totalLength += length;
        }
        return false;
    }
}
