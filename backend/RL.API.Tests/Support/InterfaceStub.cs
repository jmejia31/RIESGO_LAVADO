using System.Reflection;
using System.Threading.Tasks;

namespace RL.API.Tests.Support;

public sealed record StubInvocation(string MethodName, object?[] Arguments);

public class InterfaceStub : DispatchProxy
{
    private readonly Dictionary<string, Func<object?[], object?>> _handlers = new(StringComparer.Ordinal);
    private readonly List<StubInvocation> _invocations = new();

    public IReadOnlyList<StubInvocation> Invocations => _invocations;

    public static T Create<T>(out InterfaceStub stub) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        stub = (InterfaceStub)(object)proxy;
        return proxy;
    }

    public void On(string methodName, Func<object?[], object?> handler) => _handlers[methodName] = handler;

    public IReadOnlyList<StubInvocation> CallsTo(string methodName) =>
        _invocations.Where(x => x.MethodName == methodName).ToList();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            throw new InvalidOperationException("No se pudo identificar el método invocado.");

        var arguments = args ?? Array.Empty<object?>();
        _invocations.Add(new StubInvocation(targetMethod.Name, arguments.ToArray()));

        if (_handlers.TryGetValue(targetMethod.Name, out var handler))
            return handler(arguments);

        // Los stubs existentes preceden al contrato de publicación semántica.
        // Su valor por defecto conserva el foco de esas pruebas en la operación
        // que estaban cubriendo; las pruebas del validador cubren el contrato.
        if (targetMethod.Name == "ValidarDefinicionPublicableAsync")
            return Task.FromResult(new RL.API.Features.MatricesRiesgos.Domain.FormularioDefinitionValidationResult());

        throw new InvalidOperationException($"No existe respuesta configurada para {targetMethod.Name}.");
    }
}
