using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosApplicationTests
{
    [Fact]
    public async Task Obtener_IdInvalido_NoConsultaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ObtenerAsync(0);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync)));
    }

    [Fact]
    public async Task Crear_DatosValidos_NormalizaYRetornaDetalle()
    {
        var service = CrearServicio(out var repo, out _);
        var detalle = new MatrizRiesgoDetalleDto { MatrizId = 21, NombreSujeto = "Proveedor Uno" };
        repo.On(nameof(IMatricesRiesgosRepository.CrearMatrizAsync), _ => Task.FromResult(21L));
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(detalle));
        var dto = CrearMatrizValida();
        dto.SujetoTipo = " proveedor ";
        dto.NombreSujeto = " Proveedor Uno ";

        var result = await service.CrearAsync(dto, 7, "analista@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Same(detalle, result.Data);
        Assert.Equal("PROVEEDOR", dto.SujetoTipo);
        Assert.Equal("Proveedor Uno", dto.NombreSujeto);
        Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearMatrizAsync)));
    }

    [Fact]
    public async Task Crear_SinDetalles_RechazaSinEscribir()
    {
        var service = CrearServicio(out var repo, out _);
        var dto = CrearMatrizValida();
        dto.Detalles.Clear();

        var result = await service.CrearAsync(dto, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearMatrizAsync)));
    }

    [Fact]
    public async Task Crear_ReglaRepositorioInvalida_ConvierteExcepcionEnBadRequest()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CrearMatrizAsync), _ => throw new InvalidOperationException("Matriz duplicada"));

        var result = await service.CrearAsync(CrearMatrizValida(), 7, null, null);

        Assert.False(result.Success);
        Assert.Equal("Matriz duplicada", result.Message);
    }

    [Fact]
    public async Task Actualizar_MatrizInexistente_DevuelveNotFound()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarMatrizAsync), _ => Task.FromResult(false));

        var result = await service.ActualizarAsync(90, CrearMatrizValida(), 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync)));
    }

    [Fact]
    public async Task Recalcular_SinMotivo_RechazaAntesDePrepararSolicitud()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.CalcularAsync(12, new MatrizRiesgoCalcularRequestDto
        {
            TipoCalculo = "FACTOR",
            MotivoCalculo = " "
        }, true, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.PrepararSolicitudCalculoAsync)));
    }

    [Fact]
    public async Task Calcular_ResultadoValido_PersisteYRetornaMotor()
    {
        var service = CrearServicio(out var repo, out var motor);
        var solicitud = new MatrizCalculoRequestDto { TipoCalculo = "FACTOR" };
        var calculo = new MatrizCalculoResultadoDto { PuntajeResidual = 3.5m, NivelResidual = "ALTO" };
        repo.On(nameof(IMatricesRiesgosRepository.PrepararSolicitudCalculoAsync), _ => Task.FromResult<MatrizCalculoRequestDto?>(solicitud));
        repo.On(nameof(IMatricesRiesgosRepository.PersistirResultadoCalculoAsync), _ => Task.CompletedTask);
        motor.On(nameof(IMatricesRiesgoService.Calcular), _ => ServiceResult<MatrizCalculoResultadoDto>.Ok(calculo));

        var result = await service.CalcularAsync(12, new MatrizRiesgoCalcularRequestDto { TipoCalculo = "FACTOR" }, false, 7, "a@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Same(calculo, result.Data);
        Assert.Single(motor.CallsTo(nameof(IMatricesRiesgoService.Calcular)));
        Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.PersistirResultadoCalculoAsync)));
    }

    [Fact]
    public async Task CambiarEstado_NormalizaEstadoYDelegaMotivo()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync), _ => Task.FromResult(true));

        var result = await service.CambiarEstadoAsync(15, new MatrizRiesgoCambiarEstadoRequestDto
        {
            Estado = " aprobada ",
            Motivo = "Aprobación del comité"
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync)));
        Assert.Equal("APROBADA", call.Arguments[1]);
        Assert.Equal("Aprobación del comité", call.Arguments[2]);
    }

    [Fact]
    public async Task CambiarEstado_EstadoInvalido_NoInvocaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.CambiarEstadoAsync(15, new MatrizRiesgoCambiarEstadoRequestDto
        {
            Estado = "PUBLICADA",
            Motivo = "Cambio"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync)));
    }

    [Fact]
    public async Task CambiarEstado_CierreConPlanRequeridoPendiente_RechazaSinCambiar()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(new MatrizRiesgoDetalleDto
        {
            MatrizId = 15,
            RequierePlanAccion = true
        }));
        repo.On(nameof(IMatricesRiesgosRepository.TienePlanTratadoParaCierreAsync), _ => Task.FromResult(false));

        var result = await service.CambiarEstadoAsync(15, new MatrizRiesgoCambiarEstadoRequestDto
        {
            Estado = "CERRADA",
            Motivo = "Fin de evaluación"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Contains("plan de acción", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoAsync)));
    }

    [Fact]
    public async Task ListarPlanes_MatrizInexistente_DevuelveNotFound()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(null));

        var result = await service.ListarPlanesAsync(88);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ListarPlanesAsync)));
    }

    [Fact]
    public async Task CrearPlan_DatosValidos_NormalizaYRetornaPlan()
    {
        var service = CrearServicio(out var repo, out _);
        var plan = new MatrizRiesgoPlanAccionDto { PlanId = 44, MatrizId = 12, Actividad = "Revisar expediente" };
        repo.On(nameof(IMatricesRiesgosRepository.CrearPlanAsync), _ => Task.FromResult(44L));
        repo.On(nameof(IMatricesRiesgosRepository.ListarPlanesAsync), _ => Task.FromResult(new List<MatrizRiesgoPlanAccionDto> { plan }));
        var dto = new MatrizRiesgoPlanAccionRequestDto
        {
            Actividad = " Revisar expediente ",
            Responsable = " Unidad de Cumplimiento ",
            Periodicidad = " Mensual "
        };

        var result = await service.CrearPlanAsync(12, dto, 7, null, null);

        Assert.True(result.Success);
        Assert.Same(plan, result.Data);
        Assert.Equal("Revisar expediente", dto.Actividad);
        Assert.Equal("Unidad de Cumplimiento", dto.Responsable);
        Assert.Equal("Mensual", dto.Periodicidad);
    }

    [Fact]
    public async Task CrearPlan_FechasInvertidas_RechazaSinEscribir()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.CrearPlanAsync(12, new MatrizRiesgoPlanAccionRequestDto
        {
            Actividad = "Revisar expediente",
            Responsable = "Cumplimiento",
            FechaInicio = new DateTime(2026, 7, 20),
            FechaFin = new DateTime(2026, 7, 10)
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.CrearPlanAsync)));
    }

    [Fact]
    public async Task ActualizarPlan_NoEncontrado_NoConsultaListado()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarPlanAsync), _ => Task.FromResult(false));

        var result = await service.ActualizarPlanAsync(12, 44, new MatrizRiesgoPlanAccionRequestDto
        {
            Actividad = "Revisar expediente",
            Responsable = "Cumplimiento"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ListarPlanesAsync)));
    }

    [Fact]
    public async Task CambiarEstadoPlan_Valido_NormalizaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CambiarEstadoPlanAsync), _ => Task.FromResult(true));

        var result = await service.CambiarEstadoPlanAsync(12, 44, new MatrizRiesgoPlanEstadoRequestDto
        {
            Estado = " cerrado ",
            Motivo = " Evidencia revisada "
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.CambiarEstadoPlanAsync)));
        Assert.Equal("CERRADO", call.Arguments[2]);
        Assert.Equal("Evidencia revisada", call.Arguments[3]);
    }

    [Fact]
    public async Task InactivarPlan_MotivoValido_RecortaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.InactivarPlanAsync), _ => Task.FromResult(true));

        var result = await service.InactivarPlanAsync(12, 44, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = " Plan sustituido "
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.InactivarPlanAsync)));
        Assert.Equal("Plan sustituido", call.Arguments[2]);
    }

    [Theory]
    [InlineData(0, 44, "Reapertura autorizada")]
    [InlineData(12, 0, "Reapertura autorizada")]
    [InlineData(12, 44, " ")]
    public async Task ReactivarPlan_DatosInvalidos_RechazaSinEscribir(long matrizId, long planId, string motivo)
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ReactivarPlanAsync(matrizId, planId, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = motivo
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ReactivarPlanAsync)));
    }

    [Fact]
    public async Task ReactivarPlan_MotivoValido_RecortaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ReactivarPlanAsync), _ => Task.FromResult(true));

        var result = await service.ReactivarPlanAsync(12, 44, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = " Reapertura autorizada "
        }, 7, "analista@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.ReactivarPlanAsync)));
        Assert.Equal(12L, call.Arguments[0]);
        Assert.Equal(44L, call.Arguments[1]);
        Assert.Equal("Reapertura autorizada", call.Arguments[2]);
        Assert.Equal(7L, call.Arguments[3]);
        Assert.Equal("analista@ihss.hn", call.Arguments[4]);
        Assert.Equal("127.0.0.1", call.Arguments[5]);
    }

    [Fact]
    public async Task ReactivarPlan_NoEncontrado_DevuelveNotFound()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ReactivarPlanAsync), _ => Task.FromResult(false));

        var result = await service.ReactivarPlanAsync(12, 44, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = "Reapertura autorizada"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ReactivarPlan_ReglaRepositorioInvalida_DevuelveBadRequest()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ReactivarPlanAsync), _ => throw new InvalidOperationException("El plan ya se encuentra activo."));

        var result = await service.ReactivarPlanAsync(12, 44, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = "Reapertura autorizada"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("El plan ya se encuentra activo.", result.Message);
    }

    [Fact]
    public async Task ListarEvidencias_MatrizExistente_RetornaColeccion()
    {
        var service = CrearServicio(out var repo, out _);
        var evidencias = new List<MatrizRiesgoEvidenciaDto> { new() { EvidenciaId = 5, MatrizId = 12 } };
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(new MatrizRiesgoDetalleDto { MatrizId = 12 }));
        repo.On(nameof(IMatricesRiesgosRepository.ListarEvidenciasAsync), _ => Task.FromResult(evidencias));

        var result = await service.ListarEvidenciasAsync(12);

        Assert.True(result.Success);
        Assert.Same(evidencias, result.Data);
    }

    [Fact]
    public async Task CargarEvidencia_FirmaInvalida_RechazaAntesDelRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.CargarEvidenciaAsync(12, null, null, CrearArchivo("evidencia.pdf", "application/pdf", new byte[] { 1, 2, 3, 4 }), 7, null, null);

        Assert.False(result.Success);
        Assert.Contains("firma real", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task CargarEvidencia_ArchivoValido_RegistraHashYRetornaMetadata()
    {
        var directorio = CrearDirectorioTemporal();
        try
        {
            var service = CrearServicio(out var repo, out _, CrearConfiguracionEvidencias(directorio));
            MatrizRiesgoEvidenciaRegistroDto? registro = null;
            repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(new MatrizRiesgoDetalleDto { MatrizId = 12 }));
            repo.On(nameof(IMatricesRiesgosRepository.RegistrarEvidenciaAsync), args =>
            {
                registro = Assert.IsType<MatrizRiesgoEvidenciaRegistroDto>(args[0]);
                return Task.FromResult(80L);
            });
            repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaAsync), _ => Task.FromResult<MatrizRiesgoEvidenciaDto?>(new MatrizRiesgoEvidenciaDto
            {
                EvidenciaId = 80,
                MatrizId = 12,
                NombreOriginal = "evidencia.pdf",
                Activa = true
            }));

            var result = await service.CargarEvidenciaAsync(12, 3, 4, CrearArchivo("evidencia.pdf", "application/pdf", PdfValido()), 7, null, null);

            Assert.True(result.Success);
            Assert.Equal(80, result.Data!.EvidenciaId);
            Assert.NotNull(registro);
            Assert.Equal(64, registro.HashSha256!.Length);
            Assert.True(File.Exists(registro.RutaFisica));
        }
        finally
        {
            EliminarDirectorioTemporal(directorio);
        }
    }

    [Fact]
    public async Task CargarEvidencia_FalloRepositorio_EliminaArchivoFisico()
    {
        var directorio = CrearDirectorioTemporal();
        try
        {
            var service = CrearServicio(out var repo, out _, CrearConfiguracionEvidencias(directorio));
            repo.On(nameof(IMatricesRiesgosRepository.ObtenerMatrizAsync), _ => Task.FromResult<MatrizRiesgoDetalleDto?>(new MatrizRiesgoDetalleDto { MatrizId = 12 }));
            repo.On(nameof(IMatricesRiesgosRepository.RegistrarEvidenciaAsync), _ => throw new InvalidOperationException("Oracle no disponible"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CargarEvidenciaAsync(12, null, null, CrearArchivo("evidencia.pdf", "application/pdf", PdfValido()), 7, null, null));

            Assert.Empty(Directory.Exists(directorio) ? Directory.GetFiles(directorio) : Array.Empty<string>());
        }
        finally
        {
            EliminarDirectorioTemporal(directorio);
        }
    }

    [Fact]
    public async Task DescargarEvidencia_RutaFueraDelAlmacenamiento_RechazaSinAuditar()
    {
        var directorio = CrearDirectorioTemporal();
        try
        {
            var service = CrearServicio(out var repo, out _, CrearConfiguracionEvidencias(directorio));
            repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaAsync), _ => Task.FromResult<MatrizRiesgoEvidenciaDto?>(new MatrizRiesgoEvidenciaDto
            {
                EvidenciaId = 5,
                MatrizId = 12,
                Activa = true,
                RutaFisica = Path.Combine(Path.GetDirectoryName(directorio)!, "fuera.pdf")
            }));

            var result = await service.DescargarEvidenciaAsync(12, 5, 7, null, null);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.RegistrarDescargaEvidenciaAsync)));
        }
        finally
        {
            EliminarDirectorioTemporal(directorio);
        }
    }

    [Fact]
    public async Task DescargarEvidencia_ArchivoValido_RetornaContenidoYAudita()
    {
        var directorio = CrearDirectorioTemporal();
        try
        {
            Directory.CreateDirectory(directorio);
            var ruta = Path.Combine(directorio, "archivo.pdf");
            await File.WriteAllBytesAsync(ruta, PdfValido());
            var service = CrearServicio(out var repo, out _, CrearConfiguracionEvidencias(directorio));
            repo.On(nameof(IMatricesRiesgosRepository.ObtenerEvidenciaAsync), _ => Task.FromResult<MatrizRiesgoEvidenciaDto?>(new MatrizRiesgoEvidenciaDto
            {
                EvidenciaId = 5,
                MatrizId = 12,
                Activa = true,
                RutaFisica = ruta,
                NombreOriginal = "reporte.pdf",
                TipoMime = "application/pdf"
            }));
            repo.On(nameof(IMatricesRiesgosRepository.RegistrarDescargaEvidenciaAsync), _ => Task.CompletedTask);

            var result = await service.DescargarEvidenciaAsync(12, 5, 7, null, null);

            Assert.True(result.Success);
            Assert.Equal("reporte.pdf", result.Data!.NombreArchivo);
            Assert.Equal(PdfValido(), result.Data.Contenido);
            Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.RegistrarDescargaEvidenciaAsync)));
        }
        finally
        {
            EliminarDirectorioTemporal(directorio);
        }
    }

    [Fact]
    public async Task InactivarEvidencia_MotivoValido_RecortaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.InactivarEvidenciaAsync), _ => Task.FromResult(true));

        var result = await service.InactivarEvidenciaAsync(12, 5, new MatrizRiesgoInactivarRequestDto { Motivo = " Documento sustituido " }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.InactivarEvidenciaAsync)));
        Assert.Equal("Documento sustituido", call.Arguments[2]);
    }

    [Fact]
    public void EvidenciaPublica_NoSerializaRutaFisica()
    {
        var json = JsonConvert.SerializeObject(new MatrizRiesgoEvidenciaDto
        {
            EvidenciaId = 5,
            RutaFisica = @"C:\App_Data\Evidencias\archivo.pdf",
            NombreOriginal = "archivo.pdf"
        });

        Assert.DoesNotContain("rutaFisica", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nombreOriginal", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("EXCEL", "application/vnd.ms-excel")]
    [InlineData("PDF", "application/pdf")]
    public async Task ExportarReporte_FormatoValido_GeneraContenidoYRegistraAuditoria(string formato, string contentType)
    {
        var service = CrearServicio(out var repo, out _);
        var reporte = new MatricesRiesgoReporteDto();
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerReporteAsync), _ => Task.FromResult(reporte));
        repo.On(nameof(IMatricesRiesgosRepository.RegistrarExportacionReporteAsync), _ => Task.CompletedTask);
        var filtro = new MatrizRiesgoReporteFiltroDto { Estado = " aprobada ", SujetoTipo = " proveedor " };

        var result = await service.ExportarReporteAsync(filtro, formato, 7, "a@ihss.hn", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal(contentType, result.Data!.ContentType);
        Assert.NotEmpty(result.Data.Contenido);
        Assert.Equal("APROBADA", filtro.Estado);
        Assert.Equal("PROVEEDOR", filtro.SujetoTipo);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.RegistrarExportacionReporteAsync)));
        Assert.Equal(formato, call.Arguments[1]);
    }

    [Fact]
    public async Task ExportarReporte_FormatoInvalido_NoConsultaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ExportarReporteAsync(new MatrizRiesgoReporteFiltroDto(), "CSV", 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task CrearCriterio_Valido_NormalizaYRetornaCreado()
    {
        var service = CrearServicio(out var repo, out _);
        var criterio = new MatrizRiesgoCriterioDto { CriterioId = 33, Descripcion = "Rango alto" };
        repo.On(nameof(IMatricesRiesgosRepository.CrearCriterioAsync), _ => Task.FromResult(33L));
        repo.On(nameof(IMatricesRiesgosRepository.ListarCriteriosAsync), _ => Task.FromResult(new List<MatrizRiesgoCriterioDto> { criterio }));
        var dto = new MatrizRiesgoCriterioRequestDto
        {
            VariableId = 2,
            Puntaje = 4,
            ValorDesde = 10,
            ValorHasta = 20,
            Descripcion = " Rango alto "
        };

        var result = await service.CrearCriterioAsync(dto, 7, null, null);

        Assert.True(result.Success);
        Assert.Same(criterio, result.Data);
        Assert.Equal("Rango alto", dto.Descripcion);
    }

    [Fact]
    public async Task ActualizarCriterio_NoEncontrado_NoListaCatalogo()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ActualizarCriterioAsync), _ => Task.FromResult(false));

        var result = await service.ActualizarCriterioAsync(34, new MatrizRiesgoCriterioRequestDto
        {
            VariableId = 2,
            Puntaje = 4,
            Descripcion = "Rango"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.ListarCriteriosAsync)));
    }

    [Fact]
    public async Task InactivarCriterio_MotivoValido_RecortaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.InactivarCriterioAsync), _ => Task.FromResult(true));

        var result = await service.InactivarCriterioAsync(35, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = " Fuera de vigencia "
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.InactivarCriterioAsync)));
        Assert.Equal("Fuera de vigencia", call.Arguments[1]);
    }

    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub, out InterfaceStub motorStub, IConfiguration? configuration = null)
    {
        var repo = InterfaceStub.Create<IMatricesRiesgosRepository>(out repoStub);
        var motor = InterfaceStub.Create<IMatricesRiesgoService>(out motorStub);
        return new MatricesRiesgosAppService(repo, motor, configuration);
    }

    private static IConfiguration CrearConfiguracionEvidencias(string directorio) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MatricesRiesgos:Evidencias:StoragePath"] = directorio,
            ["Evidencias:ValidateFileSignature"] = "true",
            ["Evidencias:AllowedMimeTypes:.pdf:0"] = "application/pdf"
        }).Build();

    private static IFormFile CrearArchivo(string nombre, string contentType, byte[] contenido)
    {
        var stream = new MemoryStream(contenido);
        return new FormFile(stream, 0, stream.Length, "archivo", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static byte[] PdfValido() => new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 };

    private static string CrearDirectorioTemporal() => Path.Combine(Path.GetTempPath(), $"rl-matrices-{Guid.NewGuid():N}");

    private static void EliminarDirectorioTemporal(string directorio)
    {
        if (Directory.Exists(directorio))
            Directory.Delete(directorio, true);
    }

    private static MatrizRiesgoCrearRequestDto CrearMatrizValida() => new()
    {
        SujetoTipo = "PROVEEDOR",
        NombreSujeto = "Proveedor",
        OrigenDatos = "CAPTURA",
        Detalles = new List<MatrizRiesgoDetalleRequestDto>
        {
            new() { VariableId = 1, Puntaje = 3 }
        }
    };
}
