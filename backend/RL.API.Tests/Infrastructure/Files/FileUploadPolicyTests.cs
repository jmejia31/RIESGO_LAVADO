using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Http;
using RL.API.Infrastructure.Files;
using Xunit;

namespace RL.API.Tests.Infrastructure.Files;

public sealed class FileUploadPolicyTests
{
    [Fact]
    public void EvidencePolicyAcceptsValidPdfFixture()
    {
        var file = CreateFile("evidence.pdf", "application/pdf", "%PDF-1.4\n%%EOF"u8.ToArray());

        Assert.Null(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.Evidence()));
    }

    [Fact]
    public void ListImportPolicyAcceptsValidCsvAndXmlFixtures()
    {
        var csv = CreateFile("list.csv", "text/csv", Encoding.UTF8.GetBytes("name,id\nAlice,1\n"));
        var xml = CreateFile("list.xml", "application/xml", Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><list><entry id=\"1\"/></list>"));

        Assert.Null(FileUploadPolicyValidator.Validate(csv, FileUploadPolicies.ListImport()));
        Assert.Null(FileUploadPolicyValidator.Validate(xml, FileUploadPolicies.ListImport()));
    }

    [Fact]
    public void EvidencePolicyAcceptsSupportedLegacyOfficeSignatures()
    {
        byte[] oleSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        var doc = CreateFile("legacy.doc", "application/msword", oleSignature);
        var xls = CreateFile("legacy.xls", "application/vnd.ms-excel", oleSignature);

        Assert.Null(FileUploadPolicyValidator.Validate(doc, FileUploadPolicies.Evidence()));
        Assert.Null(FileUploadPolicyValidator.Validate(xls, FileUploadPolicies.Evidence()));
    }

    [Fact]
    public void ImagePolicyAcceptsSupportedFormatSignatureFixtures()
    {
        var policy = FileUploadPolicies.Images();
        var png = CreateFile("image.png", "image/png", ValidPng());
        var interlacedPng = CreateFile("interlaced.png", "image/png", ValidPng(interlace: 1));
        var jpeg = CreateFile("image.jpg", "image/jpeg", ValidJpegStructure());
        var gif = CreateFile("image.gif", "image/gif", Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"));
        var webp = CreateFile("image.webp", "image/webp", Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEAAUAmJaQAA3AA/v89"));

        Assert.Null(FileUploadPolicyValidator.Validate(png, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(interlacedPng, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(jpeg, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(gif, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(webp, policy));
    }

    [Fact]
    public void ImagePolicyRejectsSignatureSpoofAndTruncatedImageFixtures()
    {
        var policy = FileUploadPolicies.Images();
        var fakePng = CreateFile("fake.png", "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44]);
        var fakeJpeg = CreateFile("fake.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);
        var validPng = ValidPng();
        var validJpeg = ValidJpegStructure();
        var validGif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
        var validWebp = Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEAAUAmJaQAA3AA/v89");

        Assert.NotNull(FileUploadPolicyValidator.Validate(fakePng, policy));
        Assert.NotNull(FileUploadPolicyValidator.Validate(fakeJpeg, policy));
        Assert.NotNull(FileUploadPolicyValidator.Validate(CreateFile("header-only.webp", "image/webp", WebpExtendedHeaderOnly()), policy));
        Assert.NotNull(FileUploadPolicyValidator.Validate(CreateFile("truncated.png", "image/png", validPng[..^10]), policy));
        Assert.NotNull(FileUploadPolicyValidator.Validate(CreateFile("truncated.jpg", "image/jpeg", validJpeg[..^2]), policy));
        Assert.NotNull(FileUploadPolicyValidator.Validate(CreateFile("truncated.gif", "image/gif", validGif[..^1]), policy));
        Assert.NotNull(FileUploadPolicyValidator.Validate(CreateFile("truncated.webp", "image/webp", validWebp[..^1]), policy));
    }

    [Theory]
    [InlineData("payload.jpg", "image/jpeg", "MZ-file-content")]
    [InlineData("payload.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plain text")]
    [InlineData("payload.pdf", "application/pdf", "not-a-pdf")]
    public void ValidateRejectsContentThatDoesNotMatchExtension(string name, string mime, string content)
    {
        var file = CreateFile(name, mime, Encoding.UTF8.GetBytes(content));

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.Evidence()));
    }

    [Fact]
    public void ValidateRejectsSpoofedMimeType()
    {
        var file = CreateFile("image.png", "text/plain", ValidPng());

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.Images(1024 * 1024)));
    }

    [Fact]
    public void ValidateRejectsDisallowedExtension()
    {
        var file = CreateFile("payload.exe", "application/octet-stream", "MZ"u8.ToArray());

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.Evidence()));
    }

    [Fact]
    public void ValidateRejectsOversizedFile()
    {
        var file = CreateFile("list.csv", "text/csv", Encoding.UTF8.GetBytes("a,b,c"));

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.ListImport(4)));
    }

    [Theory]
    [InlineData("../escape.pdf")]
    [InlineData("..\\escape.pdf")]
    [InlineData("C:\\temp\\escape.pdf")]
    public void ValidateRejectsTraversalFileName(string name)
    {
        var file = CreateFile(name, "application/pdf", "%PDF-1.4\n%%EOF"u8.ToArray());

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.Evidence()));
    }

    [Fact]
    public void ValidateRejectsEmptyFile()
    {
        var file = CreateFile("empty.pdf", "application/pdf", []);

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.Evidence()));
    }

    [Fact]
    public void ValidateRejectsXmlWithDtd()
    {
        var content = "<!DOCTYPE list [<!ENTITY x SYSTEM 'file:///secret'>]><list>&x;</list>";
        var file = CreateFile("list.xml", "application/xml", Encoding.UTF8.GetBytes(content));

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.ListImport()));
    }

    [Fact]
    public void ValidateAcceptsOpenXmlOnlyWhenWorkbookStructureExists()
    {
        var valid = CreateOpenXml("xl/workbook.xml");
        var invalid = CreateOpenXml("xl/other.xml");

        Assert.Null(FileUploadPolicyValidator.Validate(valid, FileUploadPolicies.ListImport()));
        Assert.NotNull(FileUploadPolicyValidator.Validate(invalid, FileUploadPolicies.ListImport()));
    }

    [Fact]
    public void ValidateRejectsOpenXmlWithInvalidDocumentXml()
    {
        var file = CreateOpenXml("xl/workbook.xml", "not xml");

        Assert.NotNull(FileUploadPolicyValidator.Validate(file, FileUploadPolicies.ListImport()));
    }

    [Fact]
    public void ResolveSafePathRejectsTraversalAndKeepsGeneratedNameInsideRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "rl-upload-policy-tests");

        Assert.Null(FileUploadPolicyValidator.ResolveSafePath(root, "..\\escape.pdf"));
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "safe.pdf")), FileUploadPolicyValidator.ResolveSafePath(root, "safe.pdf"));
    }

    private static FormFile CreateFile(string name, string mime, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = mime }
        };
    }

    private static FormFile CreateOpenXml(string expectedEntry, string documentXml = "<workbook/>")
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var types = new StreamWriter(archive.CreateEntry("[Content_Types].xml").Open()))
                types.Write("<Types/>");
            using (var workbook = new StreamWriter(archive.CreateEntry(expectedEntry).Open()))
                workbook.Write(documentXml);
        }

        return CreateFile("list.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", stream.ToArray());
    }

    private static byte[] ValidPng(byte interlace = 0)
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WritePngChunk(stream, "IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, interlace]);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionMode.Compress, leaveOpen: true))
            zlib.Write([0, 0, 0, 0, 0]);
        WritePngChunk(stream, "IDAT", compressed.ToArray());
        WritePngChunk(stream, "IEND", []);
        return stream.ToArray();
    }

    private static void WritePngChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);
        var typeAndData = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        stream.Write(typeAndData);
        Span<byte> crc = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crc, CalculatePngCrc(typeAndData));
        stream.Write(crc);
    }

    private static uint CalculatePngCrc(byte[] data)
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

    private static byte[] ValidJpegStructure() => Convert.FromBase64String("/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD896KKK/1TPhz/2Q==");
    private static byte[] WebpExtendedHeaderOnly()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(30u);
        writer.Write(Encoding.ASCII.GetBytes("WEBP"));
        writer.Write(Encoding.ASCII.GetBytes("VP8X"));
        writer.Write(10u);
        writer.Write(new byte[10]);
        return stream.ToArray();
    }
}
