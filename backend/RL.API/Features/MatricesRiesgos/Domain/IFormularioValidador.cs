using System.Collections.Generic;
using System.Threading.Tasks;

namespace RL.API.Features.MatricesRiesgos.Domain;

public interface IFormularioValidador
{
    /// <summary>
    /// Valida las respuestas capturadas por el usuario contra el esquema de configuración del formulario.
    /// Valida obligatoriedad, tipos de datos, regex y bloquea campos sucios no declarados.
    /// </summary>
    Task<FormularioValidationResult> ValidarRespuestasAsync(string jsonRespuestas, string jsonConfigFormulario);

    /// <summary>
    /// Valida la definición persistida antes de permitir su publicación.
    /// Esta validación es independiente de las respuestas del usuario.
    /// </summary>
    Task<FormularioDefinitionValidationResult> ValidarDefinicionPublicableAsync(string jsonConfigFormulario);
}

public sealed class FormularioDefinitionValidationResult
{
    public bool Valido => Errores.Count == 0;
    public List<FormularioValidationError> Errores { get; } = new();
}

public sealed class FormularioValidationResult
{
    public bool Valido => Errores.Count == 0;
    public List<FormularioValidationError> Errores { get; } = new();
}

public sealed class FormularioValidationError
{
    public string Campo { get; }
    public string Mensaje { get; }

    public FormularioValidationError(string campo, string mensaje)
    {
        Campo = campo;
        Mensaje = mensaje;
    }
}
