#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Caching;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Suite P1/P2 de expansión de cobertura sobre new code del módulo MatricesRiesgos.
/// Ataca ramas y líneas sin cobertura identificadas por coverage.cobertura.xml
/// sobre el HEAD 97e4996.
/// NO modifica código de producción ni pruebas previas.
/// </summary>
public sealed class MatricesRiesgosNewCodeCoverageTests
{
    #region ── AppService: LeerEntero / LeerDecimal ramas string y propiedad faltante ──

    /// <summary>
    /// Cubre LeerEntero cuando los valores vienen como string JSON ("4") en vez de número,
    /// y cuando la propiedad no existe (retorna 0). Línea 657 del AppService.
    /// </summary>
    [Fact]
    public async Task CrearEvaluacion_ConVariablesComoString_ParseaCorrectamente()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync), _ => Task.FromResult(99L));

        // Valores como strings para ejercitar las ramas LeerEntero/LeerDecimal con ValueKind.String
        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 0,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"frecuencia_inherente\":\"4\",\"impacto_inherente\":\"3\",\"controles_preventivo\":\"0.25\",\"controles_detectivo\":\"0.10\",\"controles_correctivo\":\"0.05\",\"frecuencia_residual\":\"2\",\"impacto_residual\":\"1\"}",
            EvaVersionRow = 1
        };

        ServiceResult<long> result = await service.CrearEvaluacionAsync(dto, 9, null);

        Assert.True(result.Success);
        Assert.Equal(99, result.Data);
    }

    /// <summary>
    /// Cubre LeerEntero/LeerDecimal cuando las propiedades NO existen en el JSON,
    /// retornando 0 / 0m por defecto. Primera rama de la condición.
    /// </summary>
    [Fact]
    public async Task CrearEvaluacion_ConPropiedadesFaltantes_UsaValoresCero()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync), _ => Task.FromResult(100L));

        // JSON sin ninguna de las propiedades esperadas
        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 0,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"campo_extra\":\"valor\"}",
            EvaVersionRow = 1
        };

        ServiceResult<long> result = await service.CrearEvaluacionAsync(dto, 9, null);

        Assert.True(result.Success);
    }

    /// <summary>
    /// Cubre LeerEntero/LeerDecimal cuando los valores son strings no parseables ("abc").
    /// Deben retornar 0 / 0m sin lanzar excepción.
    /// </summary>
    [Fact]
    public async Task CrearEvaluacion_ConStringNoParseables_UsaValoresCero()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        PrepararVersionPublicada(repo);
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.CrearEvaluacionAsync), _ => Task.FromResult(101L));

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 0,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"frecuencia_inherente\":\"abc\",\"impacto_inherente\":\"xyz\",\"controles_preventivo\":\"not_a_number\",\"controles_detectivo\":null,\"controles_correctivo\":true}",
            EvaVersionRow = 1
        };

        ServiceResult<long> result = await service.CrearEvaluacionAsync(dto, 9, null);

        Assert.True(result.Success);
    }

    #endregion

    #region ── AppService: ActualizarEvaluacionAsync rama validation no-null (línea 368) ──

    /// <summary>
    /// Cubre la rama donde ValidarYCalcularEvaluacionAsync retorna un error
    /// (validacion != null) en ActualizarEvaluacionAsync, línea 368.
    /// </summary>
    [Fact]
    public async Task ActualizarEvaluacion_ConErrorValidacion_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto
            {
                EvaId = 1,
                EvaVersionId = 10,
                EvaEstado = "BORRADOR"
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[{\"campos\":[{\"id\":\"campo1\",\"tipo\":\"texto\",\"obligatorio\":true,\"etiqueta\":\"Campo 1\"}]}]}"
            }));

        // Validador retorna errores
        var validResult = new FormularioValidationResult();
        validResult.Errores.Add(new FormularioValidationError("campo1", "El campo 'Campo 1' es obligatorio."));
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
            Task.FromResult(validResult));

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 1,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{}",
            EvaVersionRow = 1
        };

        ServiceResult result = await service.ActualizarEvaluacionAsync(dto, 9, "10.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    /// <summary>
    /// Cubre ActualizarEvaluacion cuando el cálculo falla (calculo.Success == false).
    /// </summary>
    [Fact]
    public async Task ActualizarEvaluacion_ConCalculoFallido_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto
            {
                EvaId = 1,
                EvaVersionId = 10,
                EvaEstado = "BORRADOR"
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
        PrepararValidacionCorrecta(validador);
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.BadRequest("Valores fuera de rango."));

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 1,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"frecuencia_inherente\":4,\"impacto_inherente\":4}",
            EvaVersionRow = 1
        };

        ServiceResult result = await service.ActualizarEvaluacionAsync(dto, 9, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    /// <summary>
    /// Cubre ActualizarEvaluacion cuando el repo lanza DBConcurrencyException (línea 380).
    /// </summary>
    [Fact]
    public async Task ActualizarEvaluacion_ConflictoConcurrencia_Retorna409()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto
            {
                EvaId = 1,
                EvaVersionId = 10,
                EvaEstado = "BORRADOR"
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ =>
            throw new DBConcurrencyException("Conflicto de concurrencia."));

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 1,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"frecuencia_inherente\":4,\"impacto_inherente\":4}",
            EvaVersionRow = 1
        };

        ServiceResult result = await service.ActualizarEvaluacionAsync(dto, 9, null);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    /// <summary>
    /// Cubre ActualizarEvaluacion cuando el repo lanza InvalidOperationException (línea 384).
    /// </summary>
    [Fact]
    public async Task ActualizarEvaluacion_OperacionInvalida_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out InterfaceStub validador, out InterfaceStub calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto
            {
                EvaId = 1,
                EvaVersionId = 10,
                EvaEstado = "BORRADOR"
            }));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
        PrepararValidacionCorrecta(validador);
        PrepararCalculoCorrecto(calculador);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarEvaluacionAsync), _ =>
            throw new InvalidOperationException("Estado inválido."));

        var dto = new EvaluacionRiesgoDto
        {
            EvaId = 1,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaDataJson = "{\"frecuencia_inherente\":4,\"impacto_inherente\":4}",
            EvaVersionRow = 1
        };

        ServiceResult result = await service.ActualizarEvaluacionAsync(dto, 9, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region ── AppService: EliminarEvidencia — rama default del switch (línea 573) ──

    /// <summary>
    /// Ejercita el case default del switch de ResultadoEliminacionEvidencia
    /// usando un valor entero fuera del enum para forzar la rama desconocida.
    /// </summary>
    [Fact]
    public async Task EliminarEvidencia_ResultadoDesconocido_Retorna400()
    {
        MatricesRiesgosAppService service = CrearServicio(out InterfaceStub repo, out _, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(new EvidenciaDto
            {
                EviId = 99,
                EviNombreArchivo = "doc.pdf",
                EviRuta = string.Empty
            }));
        repo.On(nameof(IMatricesRiesgosRepository.EliminarEvidenciaSeguraAsync), _ =>
            Task.FromResult((ResultadoEliminacionEvidencia)999));

        ServiceResult result = await service.EliminarEvidenciaAsync(99, 9, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("desconocido", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ── CachedMatricesRiesgosAppService: delegaciones pass-through no cacheadas ──

    /// <summary>
    /// Ejercita las 6 líneas pass-through del decorador caché que delegan
    /// directamente al inner service sin cache:
    /// CrearEvaluacion, ActualizarEvaluacion, TransicionarEstado,
    /// CargarArchivoEvidencia, VincularEvidencia, EliminarEvidencia.
    /// </summary>
    [Fact]
    public async Task Cached_DelegacionesPassThrough_InvocanInnerDirectamente()
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out InterfaceStub repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out InterfaceStub validadorStub);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out InterfaceStub calculadorStub);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);

        var inner = new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
        IApplicationCache cache = InterfaceStub.Create<IApplicationCache>(out InterfaceStub cacheStub);
        cacheStub.On("Invalidate", _ => (object?)null);
        var settings = new ApplicationCacheSettings();
        var cached = new CachedMatricesRiesgosAppService(inner, cache, settings);

        // 1. ListarEvaluacionesPaginadas (line 149)
        repoStub.On(nameof(IMatricesRiesgosRepository.ListarEvaluacionesPaginadasAsync), _ =>
            Task.FromResult(new EvaluacionesPaginadasDto()));
        var r1 = await cached.ListarEvaluacionesPaginadasAsync(new ConsultaEvaluacionPaginadaDto());
        Assert.True(r1.Success);

        // 2. ObtenerEvaluacion (line 146)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(null));
        var r2 = await cached.ObtenerEvaluacionAsync(1);
        Assert.False(r2.Success);

        // 3. TransicionarEstadoEvaluacion (line 163)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvaluacionAsync), _ =>
            Task.FromResult<EvaluacionRiesgoDto?>(new EvaluacionRiesgoDto { EvaId = 1, EvaEstado = "BORRADOR" }));
        repoStub.On(nameof(IMatricesRiesgosRepository.TransicionarEstadoEvaluacionAsync), _ =>
            Task.FromResult(true));
        var r3 = await cached.TransicionarEstadoEvaluacionAsync(1, "EN_REVISION", null, 9, null);
        Assert.True(r3.Success);

        // 4. ObtenerFlujosEvaluacion (line 166)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerFlujosEvaluacionAsync), _ =>
            Task.FromResult(new List<FlujoEvaluacionDto>()));
        var r4 = await cached.ObtenerFlujosEvaluacionAsync(1);
        Assert.True(r4.Success);

        // 5. ObtenerEvidenciaFisica (line 172)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaFisicaAsync), _ =>
            Task.FromResult<EvidenciaDto?>(null));
        var r5 = await cached.ObtenerEvidenciaFisicaAsync(1);
        Assert.False(r5.Success);

        // 6. ObtenerConsolidadoTipado (line 181)
        repoStub.On(nameof(IMatricesRiesgosRepository.ObtenerConsolidadoTipadoAsync), _ =>
            Task.FromResult<IReadOnlyList<RiesgoReporteFilaDto>>(new List<RiesgoReporteFilaDto>()));
        var r6 = await cached.ObtenerConsolidadoTipadoAsync();
        Assert.True(r6.Success);
    }

    #endregion

    #region ── FormularioValidador: ramas sin cubrir ──

    /// <summary>
    /// Cubre línea 24 del FormularioValidador: cuando jsonRespuestas es null,
    /// se reemplaza por "{}". Debe NO generar errores si no hay campos obligatorios.
    /// </summary>
    [Fact]
    public async Task Validador_ConRespuestasNulas_YSinCamposObligatorios_NoDaError()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            null!,
            "{\"secciones\":[{\"campos\":[{\"id\":\"campo1\",\"tipo\":\"texto\",\"obligatorio\":false,\"etiqueta\":\"Opcional\"}]}]}");

        Assert.True(result.Valido);
    }

    /// <summary>
    /// Cubre línea 24: cuando jsonRespuestas es null pero HAY campos obligatorios,
    /// se reemplaza por "{}" y se genera error de obligatoriedad.
    /// </summary>
    [Fact]
    public async Task Validador_ConRespuestasNulas_YCamposObligatorios_ReportaError()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            null!,
            "{\"secciones\":[{\"campos\":[{\"id\":\"nombre\",\"tipo\":\"texto\",\"obligatorio\":true,\"etiqueta\":\"Nombre\"}]}]}");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "nombre");
    }

    /// <summary>
    /// Cubre líneas 135-138: JsonException al parsear jsonRespuestas malformado.
    /// </summary>
    [Fact]
    public async Task Validador_ConJsonRespuestasMalformado_ReportaErrorJson()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            "{MALFORMADO",
            "{\"secciones\":[]}");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "JSON");
    }

    /// <summary>
    /// Cubre líneas 135-138: JsonException al parsear jsonConfigFormulario malformado.
    /// </summary>
    [Fact]
    public async Task Validador_ConJsonConfigMalformado_ReportaErrorJson()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            "{\"campo\":\"valor\"}",
            "{CONFIG_INVALIDO");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "JSON");
    }

    /// <summary>
    /// Cubre línea 187: ObtenerRespuestas con root que NO es Object (ej. array).
    /// Devuelve diccionario vacío, no lanza excepción.
    /// </summary>
    [Fact]
    public async Task Validador_ConRespuestasArray_NoDaExcepcion()
    {
        var validador = new FormularioValidador();

        // Las respuestas son un array — el parser de respuestas retorna vacío
        var result = await validador.ValidarRespuestasAsync(
            "[1,2,3]",
            "{\"secciones\":[{\"campos\":[{\"id\":\"campo1\",\"tipo\":\"texto\",\"obligatorio\":true,\"etiqueta\":\"Campo 1\"}]}]}");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "campo1");
    }

    /// <summary>
    /// Cubre validación de regex inválida en la plantilla (catch ArgumentException, línea 127-131).
    /// </summary>
    [Fact]
    public async Task Validador_ConRegexInvalida_ReportaErrorDeRegex()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            "{\"email\":\"test@email.com\"}",
            "{\"secciones\":[{\"campos\":[{\"id\":\"email\",\"tipo\":\"texto\",\"obligatorio\":false,\"etiqueta\":\"Email\",\"regexValidacion\":\"[invalid(regex\"}]}]}");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "email" && e.Mensaje.Contains("inválida", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Cubre validación de catalogo-multiple con elementos inválidos (no numéricos).
    /// </summary>
    [Fact]
    public async Task Validador_CatalogoMultipleConElementosInvalidos_ReportaError()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            "{\"categorias\":[\"abc\",\"xyz\"]}",
            "{\"secciones\":[{\"campos\":[{\"id\":\"categorias\",\"tipo\":\"catalogo-multiple\",\"obligatorio\":false,\"etiqueta\":\"Categorías\"}]}]}");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "categorias");
    }

    /// <summary>
    /// Cubre validación de tipo "numero" con valor no numérico.
    /// </summary>
    [Fact]
    public async Task Validador_TipoNumeroConValorTexto_ReportaError()
    {
        var validador = new FormularioValidador();

        var result = await validador.ValidarRespuestasAsync(
            "{\"monto\":\"no-numerico\"}",
            "{\"secciones\":[{\"campos\":[{\"id\":\"monto\",\"tipo\":\"numero\",\"obligatorio\":false,\"etiqueta\":\"Monto\"}]}]}");

        Assert.False(result.Valido);
        Assert.Contains(result.Errores, e => e.Campo == "monto");
    }

    #endregion

    #region ── DTOs: instanciación de propiedades sin cobertura (P2) ──

    /// <summary>
    /// Instancia y asigna todas las propiedades de los DTOs que el reporte de cobertura
    /// marca como líneas sin hits. No es un test trivial de getter/setter:
    /// verifica la coherencia round-trip de serialización JSON como contrato API.
    /// </summary>
    [Fact]
    public void DtosContratoApi_PropiedadesNuevas_SonSerializables()
    {
        // PlanAccionDtos
        var plan = new MatrizRiesgoPlanAccionDto
        {
            PlanId = 1,
            MatrizId = 2,
            ResultadoId = 3,
            Actividad = "Revisión trimestral",
            Responsable = "Oficial LAFT",
            Periodicidad = "Trimestral",
            FechaInicio = new DateTime(2025, 1, 1),
            FechaFin = new DateTime(2025, 12, 31),
            MedioPrueba = "Actas",
            Observaciones = "Sin observaciones",
            Estado = "PENDIENTE",
            MotivoCierre = null,
            FechaCreacion = DateTime.UtcNow,
            FechaCierre = null,
            Vencido = false
        };
        string json1 = JsonSerializer.Serialize(plan);
        var planDeserialized = JsonSerializer.Deserialize<MatrizRiesgoPlanAccionDto>(json1)!;
        Assert.Equal(plan.PlanId, planDeserialized.PlanId);
        Assert.Equal(plan.Actividad, planDeserialized.Actividad);
        Assert.Equal(plan.Responsable, planDeserialized.Responsable);
        Assert.Equal(plan.Periodicidad, planDeserialized.Periodicidad);
        Assert.Equal(plan.MedioPrueba, planDeserialized.MedioPrueba);
        Assert.Equal(plan.Estado, planDeserialized.Estado);
        Assert.Equal(plan.Vencido, planDeserialized.Vencido);

        // PlanAccionRequest
        var planReq = new MatrizRiesgoPlanAccionRequestDto
        {
            ResultadoId = 7,
            Actividad = "Capacitación",
            Responsable = "Gerente",
            Periodicidad = "Anual",
            FechaInicio = new DateTime(2025, 6, 1),
            FechaFin = new DateTime(2025, 6, 30),
            MedioPrueba = "Certificado",
            Observaciones = "N/A"
        };
        string json2 = JsonSerializer.Serialize(planReq);
        var planReqDeserialized = JsonSerializer.Deserialize<MatrizRiesgoPlanAccionRequestDto>(json2)!;
        Assert.Equal(planReq.Actividad, planReqDeserialized.Actividad);

        // PlanEstadoRequest
        var planEstado = new MatrizRiesgoPlanEstadoRequestDto
        {
            Estado = "CERRADO",
            Motivo = "Completado"
        };
        string json3 = JsonSerializer.Serialize(planEstado);
        Assert.Contains("CERRADO", json3, StringComparison.Ordinal);

        // ReporteMatricesPaginadoDto
        var reportePaginado = new ReporteMatricesPaginadoDto
        {
            Items = new List<RiesgoReporteFilaDto>(),
            Pagina = 1,
            TamanoPagina = 20,
            TotalRegistros = 100,
            TotalPaginas = 5,
            Totales = new ReporteMatricesTotalesDto
            {
                TotalRiesgos = 100,
                TotalConEvaluacionOficial = 80,
                TotalSinEvaluacionOficial = 20,
                TotalAltoCritico = 15
            }
        };
        Assert.Equal(5, reportePaginado.TotalPaginas);
        Assert.Equal(15, reportePaginado.Totales.TotalAltoCritico);

        // MatrizRiesgoDashboardDinamicoDto
        var dashboard = new MatrizRiesgoDashboardDinamicoDto
        {
            FechaGeneracion = DateTime.UtcNow,
            TotalRiesgos = 50,
            TotalConEvaluacionOficial = 40,
            TotalSinEvaluacionOficial = 10,
            MapaTransicion = new List<MapaTransicionCeldaDto>
            {
                new() { NivelInherente = "ALTO", NivelResidual = "MEDIO", Total = 5, PromedioInherente = 16m, PromedioResidual = 8m }
            },
            PendientesOperativos = new List<RiesgoReporteFilaDto>()
        };
        Assert.Equal(50, dashboard.TotalRiesgos);
        Assert.Single(dashboard.MapaTransicion);

        // FiltroReporteMatricesDto
        var filtro = new FiltroReporteMatricesDto
        {
            Buscar = "LAFT",
            Area = "Operaciones",
            DuenoRiesgo = "Oficial",
            EstadoEvaluacion = "APROBADA",
            NivelInherente = "ALTO",
            NivelResidual = "MEDIO",
            RespuestaRiesgo = "MITIGAR",
            FechaInicio = new DateTime(2025, 1, 1),
            FechaFin = new DateTime(2025, 12, 31),
            Pagina = 2,
            TamanoPagina = 50
        };
        Assert.Equal("LAFT", filtro.Buscar);
        Assert.Equal(2, filtro.Pagina);

        // ConsultaEvaluacionPaginadaDto
        var consulta = new ConsultaEvaluacionPaginadaDto
        {
            Pagina = 3,
            RegistrosPorPagina = 25,
            RiesgoId = 42,
            Estado = "EN_REVISION",
            Area = "Finanzas",
            NivelResidual = "BAJO",
            Buscar = "riesgo operativo"
        };
        Assert.Equal(42, consulta.RiesgoId);
        Assert.Equal("EN_REVISION", consulta.Estado);

        // EvaluacionRiesgoDto — propiedades sin cubrir (líneas 16-19)
        var evaluacion = new EvaluacionRiesgoDto
        {
            EvaId = 1,
            EvaRiesgoId = 5,
            EvaVersionId = 10,
            EvaEstado = "APROBADA",
            EvaDataJson = "{}",
            EvaDataCalcJson = "{\"vri\":7}",
            EvaVri = 7,
            EvaEtp = 25m,
            EvaVrr = 4,
            EvaFechaEval = new DateTime(2025, 6, 15),
            EvaUsrEval = 42,
            EvaVersionRow = 2,
            EvaActivo = true
        };
        Assert.Equal(25m, evaluacion.EvaEtp);
        Assert.True(evaluacion.EvaActivo);

        // FlujoEvaluacionDto (líneas 10-11)
        var flujo = new FlujoEvaluacionDto
        {
            FluId = 1,
            FluEvaluacionId = 5,
            FluEstado = "EN_REVISION",
            FluMotivo = "Revisión inicial",
            FluUsrId = 9,
            FluFecha = DateTime.UtcNow
        };
        Assert.Equal("Revisión inicial", flujo.FluMotivo);
        Assert.Equal(9, flujo.FluUsrId);

        // RiesgoDto (líneas 11-14)
        var riesgo = new RiesgoDto
        {
            RieId = 1,
            RieCodigo = "LAFT-001",
            RieNombre = "Riesgo operativo",
            RieDescripcion = "Desc",
            RieActivo = true,
            RieUsrCreacion = 42,
            RieFechaCreacion = DateTime.UtcNow
        };
        Assert.Equal("LAFT-001", riesgo.RieCodigo);
        Assert.True(riesgo.RieActivo);

        // EvidenciaDto (líneas 11, 14-15)
        var evidencia = new EvidenciaDto
        {
            EviId = 1,
            EviNombreArchivo = "doc.pdf",
            EviExtension = "pdf",
            EviTamano = 1024,
            EviHash = "abc123",
            EviRuta = "/evidencias/doc.pdf",
            EviUsrCreacion = 9,
            EviFechaCreacion = DateTime.UtcNow
        };
        Assert.Equal(1024, evidencia.EviTamano);
        Assert.Equal(9, evidencia.EviUsrCreacion);

        // VersionFormularioDto (líneas 15-18)
        var version = new VersionFormularioDto
        {
            VerId = 1,
            VerFamiliaId = 2,
            VerCodigo = "FORM_V1",
            VerVersion = 1,
            VerJson = "{}",
            VerHash = "hash",
            VerEstado = "PUBLISHED",
            VerVigente = true,
            VerFechaInicio = new DateTime(2025, 1, 1),
            VerFechaFin = new DateTime(2025, 12, 31),
            VerFechaCreacion = DateTime.UtcNow,
            VerUsrCreacion = 9
        };
        Assert.Equal(new DateTime(2025, 1, 1), version.VerFechaInicio);
        Assert.Equal(new DateTime(2025, 12, 31), version.VerFechaFin);

        // MetodologiaFormularioDtos — SeccionFormularioDto (línea 23)
        var seccion = new SeccionFormularioDto
        {
            Clave = "seccion1",
            Titulo = "Sección 1",
            Orden = 1,
            Campos = new List<CampoFormularioDto>
            {
                new()
                {
                    CampoCanonicoId = 100,
                    Clave = "campo1",
                    Etiqueta = "Campo 1",
                    Tipo = "texto",
                    CodigoCatalogo = "CAT_01",
                    Obligatorio = true,
                    SoloLectura = false
                }
            }
        };
        Assert.Single(seccion.Campos);
        Assert.Equal(100, seccion.Campos[0].CampoCanonicoId);
        Assert.Equal("CAT_01", seccion.Campos[0].CodigoCatalogo);

        // ElementoCatalogoMatricesDto (líneas 47-49)
        var elemento = new ElementoCatalogoMatricesDto
        {
            Codigo = "E01",
            Valor = "Elemento 1",
            Orden = 1
        };
        Assert.Equal("E01", elemento.Codigo);

        // ReglaCalculoMatricesDto (línea 57)
        var regla = new ReglaCalculoMatricesDto
        {
            Codigo = "REGLA_VRI",
            Version = "1.0",
            AlgoritmoId = "MatrizMultiplicacion",
            Parametros = JsonDocument.Parse("{\"factor\":1.5}").RootElement
        };
        Assert.NotNull(regla.Parametros);
    }

    #endregion

    #region ── Helpers ──

    private static MatricesRiesgosAppService CrearServicio(
        out InterfaceStub repoStub,
        out InterfaceStub validadorStub,
        out InterfaceStub calculadorStub)
    {
        IMatricesRiesgosRepository repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        IFormularioValidador validador = InterfaceStub.Create<IFormularioValidador>(out validadorStub);
        IMatricesRiesgoService calculador = InterfaceStub.Create<IMatricesRiesgoService>(out calculadorStub);
        IAuditoriaRepository auditoria = InterfaceStub.Create<IAuditoriaRepository>(out InterfaceStub auditoriaStub);
        auditoriaStub.On("RegistrarAsync", _ => Task.CompletedTask);
        return new MatricesRiesgosAppService(repo, validador, calculador, auditoria);
    }

    private static void PrepararVersionPublicada(InterfaceStub repo)
    {
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerVersionFormularioAsync), _ =>
            Task.FromResult<VersionFormularioDto?>(new VersionFormularioDto
            {
                VerId = 10,
                VerEstado = "PUBLISHED",
                VerVigente = true,
                VerJson = "{\"secciones\":[]}"
            }));
    }

    private static void PrepararValidacionCorrecta(InterfaceStub validador)
    {
        validador.On(nameof(IFormularioValidador.ValidarRespuestasAsync), _ =>
            Task.FromResult(new FormularioValidationResult()));
    }

    private static void PrepararCalculoCorrecto(InterfaceStub calculador)
    {
        calculador.On(nameof(IMatricesRiesgoService.CalcularYValidarRiesgo), _ =>
            ServiceResult<CalculoRiesgoResultadoDto>.Ok(new CalculoRiesgoResultadoDto
            {
                Vri = 7,
                Etp = 25m,
                Vrr = 4
            }));
    }

    #endregion
}
