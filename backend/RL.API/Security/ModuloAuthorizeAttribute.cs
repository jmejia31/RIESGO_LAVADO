using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RL.API.Security;

/// <summary>
/// Valida que el JWT tenga acceso a uno de los modulos requeridos por el endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ModuloAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly HashSet<int> _moduloIds;

    public ModuloAuthorizeAttribute(params int[] moduloIds)
    {
        _moduloIds = moduloIds.Where(id => id > 0).ToHashSet();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                mensaje = "No autenticado."
            });
            return;
        }

        if (_moduloIds.Count == 0)
            return;

        var modulosClaim = user.FindFirst("modulos")?.Value;
        if (string.IsNullOrWhiteSpace(modulosClaim))
        {
            context.Result = CrearForbidden("No tiene modulos asignados para esta accion.");
            return;
        }

        var modulosUsuario = modulosClaim
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(valor => int.TryParse(valor, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        if (!_moduloIds.Any(modulosUsuario.Contains))
        {
            context.Result = CrearForbidden("No tiene permiso para acceder a este modulo.");
        }
    }

    private static ObjectResult CrearForbidden(string mensaje)
    {
        return new ObjectResult(new
        {
            success = false,
            mensaje
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
