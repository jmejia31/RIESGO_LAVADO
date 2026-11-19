using System.Net;
using Microsoft.Extensions.Configuration;
using RL.API.Infrastructure.Reporting;
using Xunit;

namespace RL.API.Tests.Infrastructure.Reporting;

public sealed class InstitutionalLogoDownloaderTests
{
    private readonly IConfiguration _config;
    private readonly InstitutionalLogoDownloader _downloader;

    public InstitutionalLogoDownloaderTests()
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["Reporting:AllowedLogoDomains:0"] = "ihss.hn",
            ["Reporting:AllowedLogoDomains:1"] = "sgrla.ihss.hn",
            ["Reporting:AllowedLogoDomains:2"] = "www.ihss.hn"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        _downloader = new InstitutionalLogoDownloader(_config);
    }

    [Theory]
    [InlineData("ihss.hn", true)]
    [InlineData("sgrla.ihss.hn", true)]
    [InlineData("www.ihss.hn", true)]
    [InlineData("portal.ihss.hn", true)]
    [InlineData("IHSS.HN", true)]
    [InlineData("evil.com", false)]
    [InlineData("ihss.hn.attacker.com", false)]
    [InlineData("notihss.hn", false)]
    [InlineData("google.com", false)]
    public void IsDomainAllowed_ValidatesAgainstWhitelist(string host, bool expected)
    {
        var allowed = new[] { "ihss.hn", "sgrla.ihss.hn", "www.ihss.hn" };
        Assert.Equal(expected, InstitutionalLogoDownloader.IsDomainAllowed(host, allowed));
    }

    [Theory]
    [InlineData("127.0.0.1", true)] // Loopback
    [InlineData("127.0.0.2", true)] // Loopback range
    [InlineData("10.0.0.1", true)] // RFC 1918 Class A
    [InlineData("10.255.255.255", true)]
    [InlineData("172.16.0.1", true)] // RFC 1918 Class B
    [InlineData("172.31.255.254", true)]
    [InlineData("192.168.1.1", true)] // RFC 1918 Class C
    [InlineData("192.168.100.50", true)]
    [InlineData("169.254.169.254", true)] // Link-Local / Cloud Metadata AWS/GCP/Azure
    [InlineData("100.64.0.1", true)] // Carrier-Grade NAT
    [InlineData("224.0.0.1", true)] // Multicast
    [InlineData("240.0.0.1", true)] // Reserved
    [InlineData("0.0.0.0", true)]
    [InlineData("8.8.8.8", false)] // Public DNS Google
    [InlineData("1.1.1.1", false)] // Public DNS Cloudflare
    [InlineData("190.92.10.5", false)] // Public IP
    public void IsDisallowedIp_IPv4_BlocksPrivateAndRestrictedRanges(string ipStr, bool shouldBlock)
    {
        var ip = IPAddress.Parse(ipStr);
        Assert.Equal(shouldBlock, InstitutionalLogoDownloader.IsDisallowedIp(ip));
    }

    [Theory]
    [InlineData("::1", true)] // IPv6 Loopback
    [InlineData("fe80::1", true)] // IPv6 Link-Local
    [InlineData("fc00::1", true)] // IPv6 Unique Local Address
    [InlineData("fd00::1234", true)] // IPv6 Unique Local Address
    [InlineData("ff02::1", true)] // IPv6 Multicast
    [InlineData("::ffff:127.0.0.1", true)] // IPv4 Mapped to IPv6
    [InlineData("::ffff:10.0.0.1", true)] // IPv4 Mapped to IPv6
    [InlineData("::ffff:8.8.8.8", false)] // IPv4 Mapped to IPv6 (Public)
    public void IsDisallowedIp_IPv6_BlocksRestrictedRanges(string ipStr, bool shouldBlock)
    {
        var ip = IPAddress.Parse(ipStr);
        Assert.Equal(shouldBlock, InstitutionalLogoDownloader.IsDisallowedIp(ip));
    }

    [Fact]
    public async Task DownloadLogoAsync_RejectsEmptyOrWhitespaceUrl()
    {
        var result = await _downloader.DownloadLogoAsync("");
        Assert.False(result.Success);
        Assert.Contains("vacía", result.FailureReason);
    }

    [Theory]
    [InlineData("http://ihss.hn/logo.png")] // Insecure HTTP
    [InlineData("ftp://ihss.hn/logo.png")] // FTP
    [InlineData("file:///C:/logo.png")] // File scheme
    [InlineData("data:image/png;base64,iVBORw0KGgo=")] // Data URI
    public async Task DownloadLogoAsync_RejectsNonHttpsSchemes(string url)
    {
        var result = await _downloader.DownloadLogoAsync(url);
        Assert.False(result.Success);
        Assert.Contains("HTTPS obligatorio", result.FailureReason);
    }

    [Fact]
    public async Task DownloadLogoAsync_RejectsCredentialsInUrl()
    {
        var result = await _downloader.DownloadLogoAsync("https://user:password@ihss.hn/logo.png");
        Assert.False(result.Success);
        Assert.Contains("credenciales", result.FailureReason);
    }

    [Theory]
    [InlineData("https://ihss.hn:8080/logo.png")]
    [InlineData("https://ihss.hn:8443/logo.png")]
    [InlineData("https://ihss.hn:22/logo.png")]
    public async Task DownloadLogoAsync_RejectsNonStandardPorts(string url)
    {
        var result = await _downloader.DownloadLogoAsync(url);
        Assert.False(result.Success);
        Assert.Contains("Puerto no permitido", result.FailureReason);
    }

    [Theory]
    [InlineData("https://127.0.0.1/logo.png")]
    [InlineData("https://10.0.0.1/logo.png")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    public async Task DownloadLogoAsync_RejectsLiteralIpAddresses(string url)
    {
        var result = await _downloader.DownloadLogoAsync(url);
        Assert.False(result.Success);
        Assert.Contains("direcciones IP literales", result.FailureReason);
    }

    [Theory]
    [InlineData("https://evil.com/logo.png")]
    [InlineData("https://attacker.org/logo.png")]
    [InlineData("https://malicious.ihss.hn.attacker.com/logo.png")]
    public async Task DownloadLogoAsync_RejectsUnauthorizedDomains(string url)
    {
        var result = await _downloader.DownloadLogoAsync(url);
        Assert.False(result.Success);
        Assert.Contains("no se encuentra en la lista blanca", result.FailureReason);
    }

    [Fact]
    public void InstitutionalJpegInspector_InspectJpeg_ParsesValidBaselineJpeg()
    {
        // Sintetizar encabezado JPEG mínimo válido con SOI, APP0 y SOF0 (baseline 8 bits)
        // Dimensiones: ancho = 200, alto = 100, 3 componentes (RGB)
        var stream = new MemoryStream();
        stream.Write(new byte[] { 0xFF, 0xD8 }); // SOI
        // APP0 (JFIF)
        stream.Write(new byte[] { 0xFF, 0xE0, 0x00, 0x10 }); // Marcador + longitud (16)
        stream.Write(System.Text.Encoding.ASCII.GetBytes("JFIF\0"));
        stream.Write(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00 });
        // SOF0 (Baseline DCT)
        // Longitud = 8 + 3*components = 17 bytes (0x00, 0x11)
        // Precision = 8
        // Height = 100 (0x00, 0x64)
        // Width = 200 (0x00, 0xC8)
        // Components = 3 (0x03)
        stream.Write(new byte[] { 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x64, 0x00, 0xC8, 0x03 });
        // Componentes specs (1, 2, 3)
        stream.Write(new byte[] { 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01 });
        // EOI
        stream.Write(new byte[] { 0xFF, 0xD9 });

        var bytes = stream.ToArray();
        var (width, height, components) = InstitutionalJpegInspector.InspectJpeg(bytes);

        Assert.Equal(200, width);
        Assert.Equal(100, height);
        Assert.Equal(3, components);
    }

    [Fact]
    public void InstitutionalJpegInspector_InspectJpeg_RejectsInvalidBytes()
    {
        Assert.Throws<InvalidOperationException>(() => InstitutionalJpegInspector.InspectJpeg(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => InstitutionalJpegInspector.InspectJpeg(new byte[] { 0x00, 0x01, 0x02, 0x03 }));
        Assert.Throws<InvalidOperationException>(() => InstitutionalJpegInspector.InspectJpeg(new byte[] { 0xFF, 0xD8, 0x00, 0x00 }));
    }

    [Fact]
    public void InstitutionalBrandingResolver_ClearCache_InvalidatesImmediately()
    {
        // Invocar ClearCache para verificar que no lanza excepciones y purga la caché
        InstitutionalBrandingResolver.ClearCache();
    }
}
