using Microsoft.AspNetCore.Http;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Listas.Application;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class ListasApplicationTests
{
    [Fact]
    public async Task RegistrarPositivo_DatosInvalidos_NoInvocaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);
        var dto = new RegistrarPositivoDto
        {
            TipoDocumentoId = 0,
            TipoPositivoId = 2,
            NoDocumento = "0801",
            NombreCompleto = "Persona",
            MotivoIngreso = "Revisión",
            TipoListaCautelaId = 1,
            OrigenRegistro = "DNP_LISTAS"
        };

        var result = await service.RegistrarPositivoAsync(dto, 7);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("tipo de documento", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_DatosValidos_NormalizaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        var dto = new RegistrarPositivoDto
        {
            TipoDocumentoId = 1,
            TipoPositivoId = 2,
            NoDocumento = " 0801 ",
            NombreCompleto = " Persona Uno ",
            MotivoIngreso = " Revisión periódica ",
            TipoListaCautelaId = 3,
            OrigenRegistro = " manual_cumplimiento "
        };

        var result = await service.RegistrarPositivoAsync(dto, 9);

        Assert.True(result.Success);
        Assert.Equal("0801", dto.NoDocumento);
        Assert.Equal("Persona Uno", dto.NombreCompleto);
        Assert.Equal("Revisión periódica", dto.MotivoIngreso);
        Assert.Equal("MANUAL_CUMPLIMIENTO", dto.OrigenRegistro);
        var call = Assert.Single(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
        Assert.Equal(9L, call.Arguments[1]);
    }

    [Fact]
    public async Task RegistrarPositivo_DnpConAsociacionNoVerificada_PermiteRegistroDeMotivo()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));
        var dto = CrearPositivo("DNP_LISTAS");

        var result = await service.RegistrarPositivoAsync(dto, 9);

        Assert.True(result.Success);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_DnpConIdentidadAmbigua_PermiteRegistroDeMotivo()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));
        var dto = CrearPositivo("DNP_LISTAS");

        var result = await service.RegistrarPositivoAsync(dto, 9);

        Assert.True(result.Success);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_DnpVerificado_Delega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ResolverIdentidadAsync), _ => Task.FromResult(
            new ResolucionIdentidadMonitoreo("0801", EstadoIdentidadMonitoreo.Verified, 2, 1)));
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));

        var result = await service.RegistrarPositivoAsync(CrearPositivo("DNP_LISTAS"), 9);

        Assert.True(result.Success);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_DocumentoConMultiplesPositivosActivos_RechazaSinEscribir()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(2));
        var dto = new RegistrarPositivoDto
        {
            TipoDocumentoId = 1,
            TipoPositivoId = 2,
            NoDocumento = "0801",
            NombreCompleto = "Persona Uno",
            MotivoIngreso = "Revisión",
            TipoListaCautelaId = 3,
            OrigenRegistro = "MANUAL_CUMPLIMIENTO"
        };

        var result = await service.RegistrarPositivoAsync(dto, 9);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("múltiples", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_MarcadorNoIdentificacion_RechazaSinConsultarPositivos()
    {
        var service = CrearServicio(out var repo, out _);
        var dto = new RegistrarPositivoDto
        {
            TipoDocumentoId = 1,
            TipoPositivoId = 2,
            NoDocumento = "N/A(3)",
            NombreCompleto = "Persona no atribuida",
            MotivoIngreso = "Revisión",
            TipoListaCautelaId = 3,
            OrigenRegistro = "MANUAL_CUMPLIMIENTO"
        };

        var result = await service.RegistrarPositivoAsync(dto, 9);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ContarPositivosActivosAsync)));
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_MarcadorConIdRegistroFuente_NormalizaASrcYRegistra()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));
        var dto = new RegistrarPositivoDto
        {
            TipoDocumentoId = 1,
            TipoPositivoId = 2,
            NoDocumento = "N/A(3)",
            IdRegistroFuente = 91009,
            NombreCompleto = "Persona no atribuida",
            MotivoIngreso = "Revisión técnica",
            TipoListaCautelaId = 3,
            OrigenRegistro = "MANUAL_CUMPLIMIENTO"
        };

        var result = await service.RegistrarPositivoAsync(dto, 9);

        Assert.True(result.Success);
        Assert.Equal("SRC:91009", dto.NoDocumento);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_NoPermiteReconvertirTipoUOrigenDeRegistroExistente()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto
            {
                PositivoId = 80, TipoPositivoId = 3, OrigenRegistro = "MANUAL_CUMPLIMIENTO"
            }));

        var result = await service.RegistrarPositivoAsync(CrearPositivo("MANUAL_CUMPLIMIENTO"), 9);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task RegistrarPositivo_MismoTipoConDiferenteOrigenOConOrigenPrevioNull_PermiteActualizacion()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto
            {
                PositivoId = 80, TipoPositivoId = 2, OrigenRegistro = null
            }));
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));

        var result = await service.RegistrarPositivoAsync(CrearPositivo("DNP_LISTAS"), 9);

        Assert.True(result.Success);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync)));
    }

    [Fact]
    public async Task ConsultarPositivoDnp_RetornaPositivoAunqueIdentidadNoEsteVerificada()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto
            {
                PositivoId = 80, TipoPositivoId = 2, OrigenRegistro = "DNP_LISTAS"
            }));

        var resultado = await service.ObtenerPositivoPorDocumentoAsync("0801");
        Assert.NotNull(resultado);
        Assert.Equal(80, resultado.PositivoId);
    }

    [Fact]
    public async Task ObtenerSeguimientos_RangoInvertido_RechazaSinConsultar()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ObtenerSeguimientosAsync("0801", new DateTime(2026, 7, 15), new DateTime(2026, 7, 1));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ObtenerSeguimientosAsync)));
    }

    [Fact]
    public async Task ObtenerPositivoPorDocumento_Natural_NoCaeEnFallbackDeEmpleado()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ObtenerPositivoPorDocumentoAsync), args =>
        {
            var doc = args[0]?.ToString();
            var tipo = args.Length > 1 ? (int?)args[1] : null;
            if (tipo == 2)
            {
                // Para natural devuelve null (no existe aún)
                return Task.FromResult<ExistingPositivoDto?>(null);
            }
            if (tipo == 3 || doc == "EMP:0801199012345")
            {
                // Empleado existe
                return Task.FromResult<ExistingPositivoDto?>(new ExistingPositivoDto { PositivoId = 99, TipoPositivoId = 3 });
            }
            return Task.FromResult<ExistingPositivoDto?>(null);
        });

        // Consulta de persona natural (tipoPositivoId = 2)
        var resultNatural = await service.ObtenerPositivoPorDocumentoAsync("0801199012345", 2);
        Assert.Null(resultNatural);

        // Consulta de empleado (tipoPositivoId = 3)
        var resultEmpleado = await service.ObtenerPositivoPorDocumentoAsync("0801199012345", 3);
        Assert.NotNull(resultEmpleado);
        Assert.Equal(99, resultEmpleado.PositivoId);
        Assert.Equal(3, resultEmpleado.TipoPositivoId);
    }

    [Fact]
    public async Task ObtenerSeguimientos_AmbiguedadSinTipo_LanzaAmbiguousIdentityException()
    {
        var service = CrearServicio(out var repo, out _);
        // Cuando no se especifica tipo (0), hay 2 positivos (uno natural y uno empleado con mismo DNI)
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), args =>
        {
            var tipo = (int)args[1]!;
            return Task.FromResult(tipo == 0 ? 2 : 1);
        });

        await Assert.ThrowsAsync<AmbiguousIdentityException>(async () =>
        {
            await service.ObtenerSeguimientosAsync("0801199012345", null, null, null);
        });
    }

    [Fact]
    public async Task ObtenerSeguimientos_ConTipoPositivoId_AislaYNoLanzaAmbiguedad()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), args => Task.FromResult(1));
        repo.On(nameof(IListasRepository.ObtenerSeguimientosAsync), args =>
        {
            var tipo = (int?)args[3];
            return Task.FromResult(new List<SeguimientoDto>
            {
                new() { DetalleListaId = tipo == 2 ? 101 : 202, PositivoId = tipo == 2 ? 10 : 20, MotivoIngreso = tipo == 2 ? "Nota Natural" : "Nota Empleado" }
            });
        });

        var segNatural = await service.ObtenerSeguimientosAsync("0801199012345", null, null, 2);
        Assert.True(segNatural.Success);
        Assert.NotNull(segNatural.Data);
        Assert.Single(segNatural.Data);
        Assert.Equal(101, segNatural.Data[0].DetalleListaId);
        Assert.Equal("Nota Natural", segNatural.Data[0].MotivoIngreso);

        var segEmpleado = await service.ObtenerSeguimientosAsync("0801199012345", null, null, 3);
        Assert.True(segEmpleado.Success);
        Assert.NotNull(segEmpleado.Data);
        Assert.Single(segEmpleado.Data);
        Assert.Equal(202, segEmpleado.Data[0].DetalleListaId);
        Assert.Equal("Nota Empleado", segEmpleado.Data[0].MotivoIngreso);
    }

    [Fact]
    public async Task RegistrarPositivo_ClaveFuenteSrc_PreservaReferencia()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ContarPositivosActivosAsync), _ => Task.FromResult(0));
        repo.On(nameof(IListasRepository.RegistrarPositivoAsync), _ => Task.FromResult(true));

        var dto = new RegistrarPositivoDto
        {
            TipoDocumentoId = 1,
            TipoPositivoId = 2,
            NoDocumento = "N/A(3)",
            IdRegistroFuente = 777,
            NombreCompleto = "Sin Identificacion",
            MotivoIngreso = "Investigación preliminar",
            TipoListaCautelaId = 1,
            OrigenRegistro = "DNP_LISTAS"
        };

        var result = await service.RegistrarPositivoAsync(dto, 5);
        Assert.True(result.Success);
        Assert.Equal("SRC:777", dto.NoDocumento);
        var calls = repo.CallsTo(nameof(IListasRepository.RegistrarPositivoAsync));
        Assert.Single(calls);
    }

    [Fact]
    public async Task ObtenerDetalleNatural_ConsultaAunqueLaIdentidadNoEsteVerificada()
    {
        var service = CrearServicio(out var repo, out _);
        var detalle = new List<DetalleCoincidenciaNaturalDto>
        {
            new() { NumeroIdentificacion = "N/A(3)", NombresPersona = "Registro fuente" }
        };
        repo.On(nameof(IListasRepository.ObtenerDetalleNaturalAsync), _ => Task.FromResult(detalle));

        var result = await service.ObtenerDetalleNaturalAsync("N/A(3)");

        Assert.Same(detalle, result);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.ObtenerDetalleNaturalAsync)));
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ResolverIdentidadAsync)));
    }

    [Fact]
    public async Task ObtenerDetalleEmpleado_ConsultaAunqueLaIdentidadEsteAmbigua()
    {
        var service = CrearServicio(out var repo, out _);
        var detalle = new List<DetalleCoincidenciaEmpleadoDto>
        {
            new() { Identidad = "0801", Nombre = "Empleado histórico" }
        };
        repo.On(nameof(IListasRepository.ObtenerDetalleEmpleadoAsync), _ => Task.FromResult(detalle));

        var result = await service.ObtenerDetalleEmpleadoAsync("0801");

        Assert.Same(detalle, result);
        Assert.Single(repo.CallsTo(nameof(IListasRepository.ObtenerDetalleEmpleadoAsync)));
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ResolverIdentidadAsync)));
    }

    [Fact]
    public async Task ObtenerDetalleNatural_TransportaClaveFuenteSinResolverDniCompartido()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ObtenerDetalleNaturalAsync),
            _ => Task.FromResult(new List<DetalleCoincidenciaNaturalDto>
            {
                new() { NumeroIdentificacion = "N/A(3)", NombresPersona = "Fuente" }
            }));
        var result = await service.ObtenerDetalleNaturalAsync("N/A(3)", 22, "DNP_COINCIDENCIA");
        Assert.Single(result);
        var invocacion = Assert.Single(repo.CallsTo(nameof(IListasRepository.ObtenerDetalleNaturalAsync)));
        Assert.Equal(22L, invocacion.Arguments[1]);
        Assert.Equal("DNP_COINCIDENCIA", invocacion.Arguments[2]);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ResolverIdentidadAsync)));
    }

    [Fact]
    public async Task ObtenerDetalleEmpleado_TransportaPeriodoYClaveFuente()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ObtenerDetalleEmpleadoAsync),
            _ => Task.FromResult(new List<DetalleCoincidenciaEmpleadoDto>()));
        var periodo = new DateTime(2026, 7, 1);
        await service.ObtenerDetalleEmpleadoAsync("0801", 33, "DNP_COINCIDENCIA", periodo);
        var invocacion = Assert.Single(repo.CallsTo(nameof(IListasRepository.ObtenerDetalleEmpleadoAsync)));
        Assert.Equal(33L, invocacion.Arguments[1]);
        Assert.Equal(periodo, invocacion.Arguments[3]);
    }

    [Fact]
    public async Task ExportarDetalle_RegistraAuditoriaConCantidadAntesDeResponder()
    {
        var service = CrearServicio(out var repo, out var auditoria);
        var datos = new List<Dictionary<string, object>>
        {
            new() { ["Nombre"] = "Uno" },
            new() { ["Nombre"] = "Dos" }
        };
        repo.On(nameof(IListasRepository.ObtenerDetalleListaParaExportarAsync), _ => Task.FromResult(datos));
        auditoria.On(nameof(IAuditoriaRepository.RegistrarAsync), _ => Task.CompletedTask);

        var result = await service.ObtenerDetalleListaParaExportarAsync(4, 12, "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        var call = Assert.Single(auditoria.CallsTo(nameof(IAuditoriaRepository.RegistrarAsync)));
        Assert.Equal("DNP_IHSS.LISTA_CAUTELA", call.Arguments[0]);
        Assert.Contains("CantidadRegistros\":2", Assert.IsType<string>(call.Arguments[4]));
        Assert.Equal(12L, call.Arguments[5]);
    }

    [Fact]
    public async Task ProcesarCarga_SinArchivo_RechazaSinConsultarRepositorio()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ProcesarCargaCautelaAsync(null, 2, 5);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task ProcesarCarga_ArchivoNoValido_ConservaMensajeDelRepositorio()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ValidarArchivoCautelaAsync), _ => Task.FromResult((false, "Estructura inválida")));

        var result = await service.ProcesarCargaCautelaAsync(CrearArchivo("lista.csv"), 2, 5);

        Assert.False(result.Success);
        Assert.Equal("Estructura inválida", result.Message);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ProcesarArchivoCsvOfacAsync)));
    }

    [Theory]
    [InlineData("lista.xml", "", nameof(IListasRepository.ProcesarArchivoXmlOnuAsync))]
    [InlineData("lista.xlsx", "LISTA ENGEL", nameof(IListasRepository.ProcesarArchivoExcelEngelAsync))]
    [InlineData("lista.xls", "PEPS", nameof(IListasRepository.ProcesarArchivoExcelPepsAsync))]
    [InlineData("lista.csv", "OFAC", nameof(IListasRepository.ProcesarArchivoCsvOfacAsync))]
    public async Task ProcesarCarga_SeleccionaProcesadorSegunFormato(string nombre, string descripcion, string metodoEsperado)
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.ValidarArchivoCautelaAsync), _ => Task.FromResult((true, "OK")));
        repo.On(nameof(IListasRepository.ObtenerDescripcionListaAsync), _ => Task.FromResult(descripcion));
        repo.On(nameof(IListasRepository.ProcesarArchivoXmlOnuAsync), _ => Task.FromResult((true, "XML procesado")));
        repo.On(nameof(IListasRepository.ProcesarArchivoExcelEngelAsync), _ => Task.FromResult((true, "ENGEL procesado")));
        repo.On(nameof(IListasRepository.ProcesarArchivoExcelPepsAsync), _ => Task.FromResult((true, "PEPS procesado")));
        repo.On(nameof(IListasRepository.ProcesarArchivoCsvOfacAsync), _ => Task.FromResult((true, "CSV procesado")));

        var result = await service.ProcesarCargaCautelaAsync(CrearArchivo(nombre), 8, 15);

        Assert.True(result.Success);
        Assert.Single(repo.CallsTo(metodoEsperado));
    }

    [Fact]
    public async Task CrearTipoLista_RepositorioSinId_DevuelveBadRequest()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IListasRepository.CrearTipoListaCautelaAsync), _ => Task.FromResult(0));

        var result = await service.CrearTipoListaCautelaAsync(new TipoListaCautelaDto
        {
            Descripcion = "Nueva lista",
            TipoArchivo = "CSV",
            CantidadColumnas = 4
        }, 3);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    private static ListasService CrearServicio(out InterfaceStub repoStub, out InterfaceStub auditoriaStub)
    {
        var repo = InterfaceStub.Create<IListasRepository>(out repoStub);
        var auditoria = InterfaceStub.Create<IAuditoriaRepository>(out auditoriaStub);
        return new ListasService(repo, auditoria);
    }

    private static RegistrarPositivoDto CrearPositivo(string origen) => new()
    {
        TipoDocumentoId = 1,
        TipoPositivoId = 2,
        NoDocumento = "0801",
        NombreCompleto = "Persona Uno",
        MotivoIngreso = "Revisión",
        TipoListaCautelaId = 3,
        OrigenRegistro = origen
    };

    private static IFormFile CrearArchivo(string nombre)
    {
        var contenido = new MemoryStream(new byte[] { 1, 2, 3 });
        return new FormFile(contenido, 0, contenido.Length, "archivo", nombre);
    }
}
