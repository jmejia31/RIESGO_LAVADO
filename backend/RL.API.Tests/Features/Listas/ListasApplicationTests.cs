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
    public async Task ObtenerSeguimientos_RangoInvertido_RechazaSinConsultar()
    {
        var service = CrearServicio(out var repo, out _);

        var result = await service.ObtenerSeguimientosAsync("0801", new DateTime(2026, 7, 15), new DateTime(2026, 7, 1));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(repo.CallsTo(nameof(IListasRepository.ObtenerSeguimientosAsync)));
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

    private static IFormFile CrearArchivo(string nombre)
    {
        var contenido = new MemoryStream(new byte[] { 1, 2, 3 });
        return new FormFile(contenido, 0, contenido.Length, "archivo", nombre);
    }
}
