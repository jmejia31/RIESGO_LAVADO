using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Serializa las pruebas Oracle del módulo para evitar agotamiento del pool
/// ODP.NET cuando xUnit ejecuta en paralelo las clases de integración.
/// No cambia el contrato productivo ni relaja las aserciones de la suite.
/// </summary>
[CollectionDefinition("MatricesRiesgosOracle", DisableParallelization = true)]
public sealed class OracleIntegrationGroup;
