using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RL.API.Infrastructure.Reporting;

public sealed record InstitutionalDownloadedLogoResult(
    bool Success,
    byte[]? Bytes,
    string? MimeType,
    string? FailureReason,
    int Width = 0,
    int Height = 0);

public interface IInstitutionalLogoDownloader
{
    Task<InstitutionalDownloadedLogoResult> DownloadLogoAsync(string url, CancellationToken cancellationToken = default);
    Task<InstitutionalDownloadedLogoResult> DownloadFaviconAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Descargador seguro de logotipos institucionales remotos HTTPS con protección estricta contra SSRF.
/// Aplica lista blanca de dominios, validación de esquemas y puertos, resolución DNS previa con exclusión
/// de redes privadas/loopback/link-local, fijación de IP contra DNS rebinding, desactivación de redirecciones
/// y límites estrictos de tiempo y tamaño de payload.
/// </summary>
public sealed class InstitutionalLogoDownloader : IInstitutionalLogoDownloader
{
    public const long MaxDownloadSizeBytes = 2 * 1024 * 1024; // 2 MB
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(3);

    private readonly IReadOnlyList<string> _allowedDomains;
    private readonly ILogger<InstitutionalLogoDownloader>? _logger;

    public InstitutionalLogoDownloader(
        IConfiguration configuration,
        ILogger<InstitutionalLogoDownloader>? logger = null)
    {
        _logger = logger;
        var domains = configuration.GetSection("Reporting:AllowedLogoDomains").Get<string[]>();
        _allowedDomains = domains is { Length: > 0 }
            ? domains.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim().ToLowerInvariant()).ToArray()
            : ["ihss.hn", "sgrla.ihss.hn", "www.ihss.hn"];
    }

    public async Task<InstitutionalDownloadedLogoResult> DownloadLogoAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Fail("URL vacía o no proporcionada.");

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return Fail("URL con formato inválido.");

        // 1. Esquema obligatorio HTTPS (prohibido HTTP plano, file, ftp, data, etc.)
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return Fail($"Esquema no permitido '{uri.Scheme}'. Se requiere HTTPS obligatorio.");

        // 2. Prohibido credenciales embebidas en URL (user:pass@host)
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return Fail("No se permiten credenciales embebidas en la URL.");

        // 3. Prohibido puertos no estándar (solo 443 o puerto por defecto)
        if (!uri.IsDefaultPort && uri.Port != 443)
            return Fail($"Puerto no permitido '{uri.Port}'. Solo se admite el puerto estándar HTTPS (443).");

        // 4. Prohibido direcciones IP literales en el hostname
        if (IPAddress.TryParse(uri.Host, out _))
            return Fail("No se permiten direcciones IP literales como destino de logotipo.");

        // 5. Validación contra lista blanca de dominios autorizados
        var host = uri.Host.Trim().ToLowerInvariant();
        if (!IsDomainAllowed(host, _allowedDomains))
            return Fail($"El dominio '{host}' no se encuentra en la lista blanca institucional de dominios permitidos.");

        // 6. Resolución DNS previa y validación de direcciones IP (protección SSRF)
        IPAddress[] ipAddresses;
        try
        {
            using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            dnsCts.CancelAfter(ConnectTimeout);
            ipAddresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, dnsCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SSRF: Fallo en la resolución DNS para el host {Host}", host);
            return Fail("No fue posible resolver la dirección IP del host institucional.");
        }

        if (ipAddresses.Length == 0)
            return Fail("La resolución DNS no retornó direcciones IP.");

        foreach (var ip in ipAddresses)
        {
            if (IsDisallowedIp(ip))
            {
                _logger?.LogWarning("SSRF: Se denegó la conexión al host {Host} porque resuelve a una IP restringida: {Ip}", host, ip);
                return Fail("La dirección IP del host institucional está restringida por políticas de seguridad (red interna, loopback o enlace local).");
            }
        }

        var targetIp = ipAddresses[0];

        // 7. Descarga segura fijando la IP validada (previene ataques de DNS rebinding) y desactivando redirecciones
        try
        {
            using var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false, // Prohibidas redirecciones hacia destinos no validados
                ConnectTimeout = ConnectTimeout,
                ConnectCallback = async (context, ct) =>
                {
                    var socket = new Socket(targetIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(targetIp, uri.Port), ct).ConfigureAwait(false);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            using var httpClient = new HttpClient(handler)
            {
                Timeout = RequestTimeout
            };

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("SGRLA-IHSS/ReportingLogoDownloader");

            using var responseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            responseCts.CancelAfter(RequestTimeout);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, responseCts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return Fail($"El servidor remoto retornó código HTTP {response.StatusCode}.");

            // Validar Content-Length si viene presente
            if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > MaxDownloadSizeBytes)
                return Fail($"El tamaño del archivo ({response.Content.Headers.ContentLength.Value} bytes) excede el límite permitido de {MaxDownloadSizeBytes} bytes.");

            // Validar Content-Type
            var mediaType = response.Content.Headers.ContentType?.MediaType?.Trim().ToLowerInvariant();
            if (mediaType is not ("image/png" or "image/jpeg" or "image/jpg"))
                return Fail($"Tipo de contenido no admitido: '{mediaType}'. Solo se admite image/png o image/jpeg.");

            // Leer payload con tope estricto de bytes
            await using var stream = await response.Content.ReadAsStreamAsync(responseCts.Token).ConfigureAwait(false);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            long totalBytes = 0;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, responseCts.Token).ConfigureAwait(false)) > 0)
            {
                totalBytes += read;
                if (totalBytes > MaxDownloadSizeBytes)
                    return Fail($"La descarga excedió el límite máximo permitido de {MaxDownloadSizeBytes} bytes.");
                memory.Write(buffer, 0, read);
            }

            var downloadedBytes = memory.ToArray();
            if (downloadedBytes.Length < 16)
                return Fail("El archivo descargado está vacío o truncado.");

            // 8. Verificación de firma mágica y capacidad de decodificación
            var isPng = downloadedBytes.Length >= 8 && downloadedBytes[0] == 137 && downloadedBytes[1] == 80 && downloadedBytes[2] == 78 && downloadedBytes[3] == 71;
            var isJpeg = downloadedBytes.Length >= 3 && downloadedBytes[0] == 0xFF && downloadedBytes[1] == 0xD8 && downloadedBytes[2] == 0xFF;

            if (!isPng && !isJpeg)
                return Fail("El contenido descargado no coincide con una firma de imagen válida (PNG o JPEG).");

            int width, height;
            string canonicalMime;
            if (isPng)
            {
                canonicalMime = "image/png";
                var decoded = InstitutionalPngDecoder.DecodePngForBranding(downloadedBytes);
                width = decoded.Width;
                height = decoded.Height;
            }
            else
            {
                canonicalMime = "image/jpeg";
                var dims = InstitutionalJpegInspector.InspectJpeg(downloadedBytes);
                width = dims.Width;
                height = dims.Height;
            }

            if (width is < 32 or > 4096 || height is < 16 or > 4096)
                return Fail($"Dimensiones {width}x{height} fuera del rango permitido (32..4096 ancho, 16..4096 alto).");

            if ((long)width * height > 16_777_216)
                return Fail("El total de píxeles supera el límite máximo permitido de 16,777,216.");

            return new InstitutionalDownloadedLogoResult(true, downloadedBytes, canonicalMime, null, width, height);
        }
        catch (OperationCanceledException)
        {
            return Fail("Tiempo de espera agotado al descargar el logotipo remoto.");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo al descargar logotipo HTTPS desde {Url}", url);
            return Fail("No fue posible descargar o verificar el logotipo institucional remoto.");
        }
    }

    public async Task<InstitutionalDownloadedLogoResult> DownloadFaviconAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Fail("URL vacía o no proporcionada.");

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return Fail("URL con formato inválido.");

        // 1. Esquema obligatorio HTTPS (prohibido HTTP plano, file, ftp, data, etc.)
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return Fail($"Esquema no permitido '{uri.Scheme}'. Se requiere HTTPS obligatorio.");

        // 2. Prohibido credenciales embebidas en URL (user:pass@host)
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return Fail("No se permiten credenciales embebidas en la URL.");

        // 3. Prohibido puertos no estándar (solo 443 o puerto por defecto)
        if (!uri.IsDefaultPort && uri.Port != 443)
            return Fail($"Puerto no permitido '{uri.Port}'. Solo se admite el puerto estándar HTTPS (443).");

        // 4. Prohibido direcciones IP literales en el hostname
        if (IPAddress.TryParse(uri.Host, out _))
            return Fail("No se permiten direcciones IP literales como destino de favicon.");

        // 5. Validación contra lista blanca de dominios autorizados
        var host = uri.Host.Trim().ToLowerInvariant();
        if (!IsDomainAllowed(host, _allowedDomains))
            return Fail($"El dominio '{host}' no se encuentra en la lista blanca institucional de dominios permitidos.");

        // 6. Resolución DNS previa y validación de direcciones IP (protección SSRF)
        IPAddress[] ipAddresses;
        try
        {
            using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            dnsCts.CancelAfter(ConnectTimeout);
            ipAddresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, dnsCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "SSRF: Fallo en la resolución DNS para el host {Host}", host);
            return Fail("No fue posible resolver la dirección IP del host institucional.");
        }

        if (ipAddresses.Length == 0)
            return Fail("La resolución DNS no retornó direcciones IP.");

        foreach (var ip in ipAddresses)
        {
            if (IsDisallowedIp(ip))
            {
                _logger?.LogWarning("SSRF: Se denegó la conexión al host {Host} porque resuelve a una IP restringida: {Ip}", host, ip);
                return Fail("La dirección IP del host institucional está restringida por políticas de seguridad (red interna, loopback o enlace local).");
            }
        }

        var targetIp = ipAddresses[0];

        // 7. Descarga segura fijando la IP validada (previene ataques de DNS rebinding) y desactivando redirecciones
        try
        {
            using var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectTimeout = ConnectTimeout,
                ConnectCallback = async (context, ct) =>
                {
                    var socket = new Socket(targetIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(targetIp, uri.Port), ct).ConfigureAwait(false);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            using var httpClient = new HttpClient(handler)
            {
                Timeout = RequestTimeout
            };

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("SGRLA-IHSS/FaviconDownloader");

            using var responseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            responseCts.CancelAfter(RequestTimeout);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, responseCts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return Fail($"El servidor remoto retornó código HTTP {response.StatusCode}.");

            if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > InstitutionalFaviconValidator.MaxFaviconSizeBytes)
                return Fail($"El tamaño del archivo ({response.Content.Headers.ContentLength.Value} bytes) excede el límite permitido para favicon.");

            await using var stream = await response.Content.ReadAsStreamAsync(responseCts.Token).ConfigureAwait(false);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            long totalBytes = 0;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, responseCts.Token).ConfigureAwait(false)) > 0)
            {
                totalBytes += read;
                if (totalBytes > InstitutionalFaviconValidator.MaxFaviconSizeBytes)
                    return Fail($"La descarga excedió el límite máximo permitido para favicon.");
                memory.Write(buffer, 0, read);
            }

            var downloadedBytes = memory.ToArray();
            var validation = InstitutionalFaviconValidator.Validate(downloadedBytes, Path.GetExtension(uri.AbsolutePath));
            if (!validation.IsValid)
                return Fail(validation.FailureReason ?? "El archivo descargado no es un favicon válido.");

            return new InstitutionalDownloadedLogoResult(true, downloadedBytes, validation.MimeType, null, validation.Width, validation.Height);
        }
        catch (OperationCanceledException)
        {
            return Fail("Tiempo de espera agotado al descargar el favicon remoto.");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo al descargar favicon HTTPS desde {Url}", url);
            return Fail("No fue posible descargar o verificar el favicon institucional remoto.");
        }
    }

    public static bool IsDomainAllowed(string host, IEnumerable<string> allowedDomains)
    {
        var normalizedHost = host.Trim().ToLowerInvariant();
        foreach (var allowed in allowedDomains)
        {
            var pattern = allowed.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(pattern)) continue;

            if (pattern.StartsWith("*."))
            {
                var suffix = pattern[1..]; // ".ihss.hn"
                if (normalizedHost.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && normalizedHost.Length > suffix.Length)
                    return true;
            }
            else if (string.Equals(normalizedHost, pattern, StringComparison.OrdinalIgnoreCase) ||
                     normalizedHost.EndsWith("." + pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsDisallowedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            // 0.0.0.0/8
            if (b[0] == 0) return true;
            // 10.0.0.0/8 (RFC 1918 Private)
            if (b[0] == 10) return true;
            // 127.0.0.0/8 (Loopback)
            if (b[0] == 127) return true;
            // 169.254.0.0/16 (Link-Local / Cloud Metadata 169.254.169.254)
            if (b[0] == 169 && b[1] == 254) return true;
            // 172.16.0.0/12 (RFC 1918 Private)
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            // 192.168.0.0/16 (RFC 1918 Private)
            if (b[0] == 192 && b[1] == 168) return true;
            // 100.64.0.0/10 (Carrier-Grade NAT)
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
            // 198.18.0.0/15 (Benchmarking)
            if (b[0] == 198 && (b[1] == 18 || b[1] == 19)) return true;
            // >= 224.0.0.0 (Multicast and Reserved 240.0.0.0/4)
            if (b[0] >= 224) return true;

            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.Equals(IPAddress.IPv6Loopback) || ip.Equals(IPAddress.IPv6Any)) return true;
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;

            var b = ip.GetAddressBytes();
            // fc00::/7 (Unique Local Address RFC 4193)
            if ((b[0] & 0xFE) == 0xFC) return true;
            // fe80::/10 (Link-Local)
            if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80) return true;
            // ff00::/8 (Multicast)
            if (b[0] == 0xFF) return true;

            return false;
        }

        return true;
    }

    private static InstitutionalDownloadedLogoResult Fail(string reason) =>
        new(false, null, null, reason);
}
