using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Listas;
using RL.API.Features.Listas.Application;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using RL.API.Shared.Results;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class PositivosCoexistenciaIntegridadTests
{
    [Fact]
    public async Task RegistrarSeguimiento_ParametrosContradictorios_TipoPositivo_RetornaBadRequest()
    {
        var controller = CrearController(out _, out _, out _);

        var result = await controller.RegistrarSeguimiento(
            noDocumento: "0801198512345",
            motivoIngreso: "Prueba seguimiento",
            archivos: null,
            tipoPositivoId: 2,
            idRegistroFuente: null,
            tipoPositivoIdForm: 3,
            idRegistroFuenteForm: null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        var json = System.Text.Json.JsonSerializer.Serialize(badRequest.Value);
        Assert.Contains("PARAMETROS_CONTRADICTORIOS", json);
    }

    [Fact]
    public async Task RegistrarSeguimiento_ParametrosContradictorios_IdRegistroFuente_RetornaBadRequest()
    {
        var controller = CrearController(out _, out _, out _);

        var result = await controller.RegistrarSeguimiento(
            noDocumento: "0801198512345",
            motivoIngreso: "Prueba seguimiento",
            archivos: null,
            tipoPositivoId: null,
            idRegistroFuente: 100,
            tipoPositivoIdForm: null,
            idRegistroFuenteForm: 200);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        var json = System.Text.Json.JsonSerializer.Serialize(badRequest.Value);
        Assert.Contains("PARAMETROS_CONTRADICTORIOS", json);
    }

    [Fact]
    public async Task RegistrarSeguimiento_TipoPositivoInvalido_RetornaBadRequest()
    {
        var controller = CrearController(out _, out _, out _);

        var result = await controller.RegistrarSeguimiento(
            noDocumento: "0801198512345",
            motivoIngreso: "Prueba",
            archivos: null,
            tipoPositivoId: 99);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task RegistrarSeguimiento_IdRegistroFuenteInvalido_RetornaBadRequest()
    {
        var controller = CrearController(out _, out _, out _);

        var result = await controller.RegistrarSeguimiento(
            noDocumento: "0801198512345",
            motivoIngreso: "Prueba",
            archivos: null,
            idRegistroFuente: -5);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task ObtenerPositivoPorDocumento_AmbiguedadSinTipo_RetornaConflict409()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        listasStub.On(nameof(IListasService.ObtenerPositivoPorDocumentoAsync), _ =>
            throw new AmbiguousIdentityException("Existen múltiples registros activos para el documento indicado."));

        var result = await controller.ObtenerPositivoPorDocumento("0801198512345", tipoPositivoId: null);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
        var json = System.Text.Json.JsonSerializer.Serialize(conflictResult.Value);
        Assert.Contains("AMBIGUOUS_IDENTITY", json);
    }

    [Fact]
    public async Task ObtenerPositivoPorDocumento_ConTipoEspecificado_ResuelveSujetoExacto()
    {
        var controller = CrearController(out var listasStub, out _, out _);
        var esperado = new ExistingPositivoDto
        {
            PositivoId = 55,
            TipoDocumentoId = 1,
            TipoPositivoId = 3,
            MotivoIngreso = "Empleado en listas"
        };
        listasStub.On(nameof(IListasService.ObtenerPositivoPorDocumentoAsync), _ =>
            Task.FromResult<ExistingPositivoDto?>(esperado));

        var result = await controller.ObtenerPositivoPorDocumento("0801198512345", tipoPositivoId: 3);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var call = Assert.Single(listasStub.CallsTo(nameof(IListasService.ObtenerPositivoPorDocumentoAsync)));
        Assert.Equal("0801198512345", call.Arguments[0]);
        Assert.Equal(3, call.Arguments[1]);
    }

    [Fact]
    public void SimulacionCoexistencia_EscenarioA_NaturalPrimero_EmpleadoSegundo()
    {
        // Simular almacén bajo restricción UNIQUE (TipoDocumentoId, NoDocumento)
        var tablaPositivos = new Dictionary<(int TipoDoc, string Doc), (long Id, int TipoPositivo, string DocFisico)>();
        long nextId = 1;

        // Paso 1: Registrar Persona Natural (DNI = 0801198512345, Tipo = 2, TipoDoc = 1)
        string dni = "0801198512345";
        int tipoDoc = 1;

        // Regla: Si cleanDoc no está ocupado, Natural usa cleanDoc
        string docNatural = dni;
        Assert.False(tablaPositivos.ContainsKey((tipoDoc, docNatural)));
        tablaPositivos[(tipoDoc, docNatural)] = (nextId++, 2, docNatural);

        // Paso 2: Registrar Empleado (DNI = 0801198512345, Tipo = 3, TipoDoc = 1)
        // Regla: Empleado usa $"EMP:{cleanDoc}"
        string docEmpleado = $"EMP:{dni}";
        Assert.False(tablaPositivos.ContainsKey((tipoDoc, docEmpleado))); // Coexiste sin conflicto!
        tablaPositivos[(tipoDoc, docEmpleado)] = (nextId++, 3, docEmpleado);

        // Verificación: Ambos coexisten bajo UNIQUE (TipoDoc, NoDocumento)
        Assert.Equal(2, tablaPositivos.Count);
        Assert.Equal(2, tablaPositivos[(tipoDoc, docNatural)].TipoPositivo);
        Assert.Equal(3, tablaPositivos[(tipoDoc, docEmpleado)].TipoPositivo);
        Assert.NotEqual(tablaPositivos[(tipoDoc, docNatural)].DocFisico, tablaPositivos[(tipoDoc, docEmpleado)].DocFisico);
    }

    [Fact]
    public void SimulacionCoexistencia_EscenarioB_EmpleadoPrimero_NaturalSegundo()
    {
        var tablaPositivos = new Dictionary<(int TipoDoc, string Doc), (long Id, int TipoPositivo, string DocFisico)>();
        long nextId = 1;

        string dni = "0801198512345";
        int tipoDoc = 1;

        // Paso 1: Registrar Empleado primero -> Empleado usa $"EMP:{cleanDoc}"
        string docEmpleado = $"EMP:{dni}";
        tablaPositivos[(tipoDoc, docEmpleado)] = (nextId++, 3, docEmpleado);

        // Paso 2: Registrar Natural segundo -> cleanDoc no está ocupado por TipoDoc 1
        string docNatural = dni;
        Assert.False(tablaPositivos.ContainsKey((tipoDoc, docNatural))); // Coexiste sin conflicto!
        tablaPositivos[(tipoDoc, docNatural)] = (nextId++, 2, docNatural);

        // Verificación: Ambos coexisten bajo UNIQUE (TipoDoc, NoDocumento)
        Assert.Equal(2, tablaPositivos.Count);
        Assert.Equal(3, tablaPositivos[(tipoDoc, docEmpleado)].TipoPositivo);
        Assert.Equal(2, tablaPositivos[(tipoDoc, docNatural)].TipoPositivo);
    }

    [Fact]
    public void SimulacionCoexistencia_EscenarioC_EmpleadoHistoricoSinPrefijo_NaturalSegundoUsaPrefijoDni()
    {
        var tablaPositivos = new Dictionary<(int TipoDoc, string Doc), (long Id, int TipoPositivo, string DocFisico)>();
        long nextId = 1;

        string dni = "0801198512345";
        int tipoDoc = 1;

        // Empleado histórico almacenado sin prefijo (legacy)
        tablaPositivos[(tipoDoc, dni)] = (nextId++, 3, dni);

        // Paso 2: Registrar Natural -> cleanDoc está ocupado, por lo que Natural usa $"DNI:{cleanDoc}"
        bool ocupado = tablaPositivos.ContainsKey((tipoDoc, dni));
        string docNatural = ocupado ? $"DNI:{dni}" : dni;

        Assert.True(ocupado);
        Assert.Equal($"DNI:{dni}", docNatural);
        Assert.False(tablaPositivos.ContainsKey((tipoDoc, docNatural)));

        tablaPositivos[(tipoDoc, docNatural)] = (nextId++, 2, docNatural);

        // Verificación: Ambos coexisten físicamente sin error de clave única
        Assert.Equal(2, tablaPositivos.Count);
        Assert.Equal(3, tablaPositivos[(tipoDoc, dni)].TipoPositivo);
        Assert.Equal(2, tablaPositivos[(tipoDoc, docNatural)].TipoPositivo);
    }

    private static ListasController CrearController(
        out InterfaceStub listasStub,
        out InterfaceStub evidenciasStub,
        out InterfaceStub coincidenciasStub)
    {
        var listas = InterfaceStub.Create<IListasService>(out listasStub);
        var evidencias = InterfaceStub.Create<IEvidenciasService>(out evidenciasStub);
        var coincidencias = InterfaceStub.Create<ICoincidenciasService>(out coincidenciasStub);

        var controller = new ListasController(listas, evidencias, coincidencias);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "99"),
            new(ClaimTypes.Email, "test@ihss.hn"),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }
}
