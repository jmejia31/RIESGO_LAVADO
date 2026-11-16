using Microsoft.AspNetCore.Hosting;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Resuelve el almacenamiento público de activos institucionales de forma única
/// para la carga HTTP y para los generadores de documentos.
/// </summary>
public static class InstitutionalBrandingStorage
{
    public static string GetWebRoot(IWebHostEnvironment environment)
    {
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        return Path.GetFullPath(webRoot);
    }

    public static string GetUploadsDirectory(IWebHostEnvironment environment) =>
        Path.Combine(GetWebRoot(environment), "uploads");

    public static string BuildPublicUploadUrl(string fileName) => $"/uploads/{fileName}";

    public static string? ResolveLocalAsset(IWebHostEnvironment environment, string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)
            || logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || logoUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;

        var root = GetWebRoot(environment);
        var relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate)
            ? candidate
            : null;
    }
}
