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
        var jpeg = CreateFile("image.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0x00, 0xFF, 0xD9]);
        var gif = CreateFile("image.gif", "image/gif", "GIF89a;"u8.ToArray());
        var webp = CreateFile("image.webp", "image/webp", [0x52, 0x49, 0x46, 0x46, 0x04, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]);

        Assert.Null(FileUploadPolicyValidator.Validate(png, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(jpeg, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(gif, policy));
        Assert.Null(FileUploadPolicyValidator.Validate(webp, policy));
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

    private static byte[] ValidPng()
    {
        return [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52,
            0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44, 0, 0, 0, 0
        ];
    }
}
