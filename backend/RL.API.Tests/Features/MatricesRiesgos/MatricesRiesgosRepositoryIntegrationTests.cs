#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;
using Xunit.Abstractions;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Suite de certificación Oracle del modelo reducido de 17 tablas.
///
/// La ejecución ordinaria NO abre conexiones Oracle. Solo se habilita cuando un
/// operador autorizado configura simultáneamente:
///   RL_ORACLE_INTEGRATION_REQUIRED=true
///   ConnectionStrings__OracleDB mediante variable de entorno o User Secrets.
///
/// La suite no ejecuta DDL, no ejecuta los scripts 05 o 06 y no modifica el
/// esquema. Únicamente certifica objetos existentes y utiliza registros aislados
/// que se eliminan al terminar cada escenario confirmado.
/// </summary>
[Collection("MatricesRiesgosOracle")]
public sealed class MatricesRiesgosRepositoryIntegrationTests
{
    private const string PrefijoPrueba = "TMR17_";

    internal static readonly string[] TablasModelo17 =
    {
        "RL_MR_FAMILIAS_FORMULARIO",
        "RL_MR_VERSIONES_FORMULARIO",
        "RL_MR_CATALOGOS",
        "RL_MR_ELEMENTOS_CATALOGO",
        "RL_MR_REGLAS_CALCULO",
        "RL_MR_RIESGOS",
        "RL_MR_EVALUACIONES_RIESGO",
        "RL_MR_PROYECCIONES_EVALUACION",
        "RL_MR_FLUJOS_EVALUACION",
        "RL_MR_CONTROLES_RIESGO",
        "RL_MR_EVALUACIONES_CONTROL",
        "RL_MR_PLANES",
        "RL_MR_ACTIVIDADES",
        "RL_MR_EVIDENCIAS",
        "RL_MR_EVIDENCIAS_VINCULOS",
        "RL_MR_SENALES_ALERTA",
        "RL_MR_AUTOMONITOREO"
    };

    internal static readonly string[] SecuenciasModelo17 =
    {
        "SEQ_RL_MR_FAMILIAS",
        "SEQ_RL_MR_VERSIONES",
        "SEQ_RL_MR_CATALOGOS",
        "SEQ_RL_MR_ELEMENTOS",
        "SEQ_RL_MR_REGLAS",
        "SEQ_RL_MR_RIESGOS",
        "SEQ_RL_MR_EVALUACIONES",
        "SEQ_RL_MR_PROYECCIONES",
        "SEQ_RL_MR_FLUJOS",
        "SEQ_RL_MR_CONTROLES",
        "SEQ_RL_MR_EVAL_CONTROLES",
        "SEQ_RL_MR_PLANES",
        "SEQ_RL_MR_ACTIVIDADES",
        "SEQ_RL_MR_EVIDENCIAS",
        "SEQ_RL_MR_EVI_VINCULOS",
        "SEQ_RL_MR_SENALES",
        "SEQ_RL_MR_AUTOMONITOREO"
    };

    // El motor de cálculo persistido es parte del módulo, pero no forma parte
    // del modelo operativo reducido de 17 tablas. Estas tablas se validan como
    // un segundo conjunto explícito para no confundir configuración activa con
    // objetos históricos de importación que deben permanecer retirados.
    internal static readonly string[] TablasConfiguracionCalculo =
    {
        "RL_MR_FORMULAS",
        "RL_MR_FORMULA_USOS",
        "RL_MR_FORMULA_VERSIONES",
        "RL_MR_FUNCION_ARGUMENTOS",
        "RL_MR_FUNCIONES",
        "RL_MR_FUNCION_VERSIONES",
        "RL_MR_PARAMETROS_CALCULO",
        "RL_MR_PARAMETRO_VERSIONES"
    };

    internal static readonly string[] SecuenciasConfiguracionCalculo =
    {
        "SEQ_RL_MR_FORMULAS",
        "SEQ_RL_MR_FORMULA_USOS",
        "SEQ_RL_MR_FORMULA_VERSIONES",
        "SEQ_RL_MR_FUNCION_ARGUMENTOS",
        "SEQ_RL_MR_FUNCIONES",
        "SEQ_RL_MR_FUNCION_VERSIONES",
        "SEQ_RL_MR_PARAMETROS",
        "SEQ_RL_MR_PARAMETRO_VERSIONES"
    };

    internal static readonly string[] TablasRetiradas =
    {
        "RL_MR_EVI_APROBACION",
        "RL_MR_EVI_REVISION",
        "RL_MR_EVI_AUTOMONITOREO",
        "RL_MR_EVI_ALERTA",
        "RL_MR_EVI_ACTIVIDAD",
        "RL_MR_EVI_PLAN",
        "RL_MR_EVI_CONTROL",
        "RL_MR_EVI_EVALUACION",
        "RL_MR_EVI_RIESGO",
        "RL_MR_DETALLES_IMPORTACION",
        "RL_MR_LOTES_IMPORTACION",
        "RL_MR_TRAZAS_CALCULO",
        "RL_MR_AUDITORIA",
        "RL_MR_PERMISOS_FORMULARIO",
        "RL_MR_APROBACIONES_FORMULARIO",
        "RL_MR_CAMPOS_FORMULARIO",
        "RL_MR_RELACIONES_RIESGO",
        "RL_MR_REVISIONES_EVALUACION"
    };

    internal static readonly string[] SecuenciasRetiradas =
    {
        "SEQ_RL_MR_AUDITORIA",
        "SEQ_RL_MR_TRAZAS",
        "SEQ_RL_MR_REVISIONES"
    };

    internal static readonly string[] IndicesPrincipales =
    {
        "IDX_RL_MR_VER_VIG",
        "IDX_RL_MR_ELE_CAT",
        "IDX_RL_MR_EVA_RIE",
        "IDX_RL_MR_EVA_VER",
        "IDX_RL_MR_FLU_EVA_FEC",
        "IDX_RL_MR_PROY_BUSQ",
        "IDX_RL_MR_PROY_AREA",
        "IDX_RL_MR_PROY_DUENO",
        "IDX_RL_MR_CON_EVA",
        "IDX_RL_MR_ECO_CON",
        "IDX_RL_MR_PLA_EVA",
        "IDX_RL_MR_ACT_PLAN",
        "IDX_RL_MR_EVV_ENTIDAD",
        "IDX_RL_MR_EVV_EVIDENCIA",
        "IDX_RL_MR_ALE_EVAL",
        "IDX_RL_MR_MON_EVAL_FEC"
    };

    internal static readonly string[] RestriccionesPrincipales =
    {
        "PK_RL_MR_FAMILIAS",
        "PK_RL_MR_VERSIONES",
        "PK_RL_MR_CATALOGOS",
        "PK_RL_MR_ELEMENTOS",
        "PK_RL_MR_REGLAS",
        "PK_RL_MR_RIESGOS",
        "PK_RL_MR_EVALUACIONES",
        "PK_RL_MR_PROYECCIONES",
        "PK_RL_MR_FLUJOS",
        "PK_RL_MR_CONTROLES",
        "PK_RL_MR_EVAL_CONTROLES",
        "PK_RL_MR_PLANES",
        "PK_RL_MR_ACTIVIDADES",
        "PK_RL_MR_EVIDENCIAS",
        "PK_RL_MR_EVI_VINCULOS",
        "PK_RL_MR_SENALES",
        "PK_RL_MR_AUTOMONITOREO",
        "FK_RL_MR_EVA_RIE",
        "FK_RL_MR_EVA_VER",
        "FK_RL_MR_PROY_EVA",
        "FK_RL_MR_FLU_EVA",
        "FK_RL_MR_EVV_EVI",
        "UQ_RL_MR_PROY_EVA",
        "UQ_RL_MR_EVV_UNICO",
        "CK_RL_MR_FLU_EST",
        "CK_RL_MR_EVV_TIPO"
    };

    private readonly ITestOutputHelper _output;
    private readonly string? _connectionString;
    private readonly bool _integrationRequired;

    public MatricesRiesgosRepositoryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<MatricesRiesgosRepositoryIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = configuration.GetConnectionString("OracleDB")
            ?? configuration["ConnectionStrings:OracleDB"];

        _integrationRequired = string.Equals(
            Environment.GetEnvironmentVariable("RL_ORACLE_INTEGRATION_REQUIRED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await ValidarContratoFisicoAsync(conn);
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        DatosCiclo datos = await CrearCicloConfirmadoAsync("COMMIT");
        try
        {
            var db = new OracleDbContext(_connectionString!);
            var auditoria = new AuditoriaRepository(db, new HttpContextAccessor());
            var repository = new MatricesRiesgosRepository(db, auditoria);

            bool vinculado = await repository.VincularEvidenciaAsync(
                new VincularEvidenciaDto
                {
                    EvidenciaId = datos.EvidenciaId,
                    TipoEntidad = TipoEntidadEvidencia.Evaluacion,
                    EntidadId = datos.EvaluacionId
                },
                datos.UsuarioId,
                "127.0.0.1");

            Assert.True(vinculado);

            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", datos.FamiliaId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_VERSIONES_FORMULARIO", "VER_ID", datos.VersionId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_RIESGOS", "RIE_ID", datos.RiesgoId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_EVALUACIONES_RIESGO", "EVA_ID", datos.EvaluacionId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_PROYECCIONES_EVALUACION", "PROY_ID", datos.ProyeccionId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_FLUJOS_EVALUACION", "FLU_ID", datos.FlujoId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_EVIDENCIAS", "EVI_ID", datos.EvidenciaId));

            long vinculoId = await ObtenerVinculoIdAsync(conn, datos);
            Assert.True(vinculoId > 0);
            Assert.Equal(1, await ContarAuditoriaAsync(conn, vinculoId));
        }
        finally
        {
            await LimpiarCicloAsync(datos);
        }
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task CicloCompleto_Rollback_NoPersisteRegistrosBase()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        DatosCiclo datos = await InsertarCicloAsync(conn, transaction, "ROLLBACK_BASE");
        await transaction.RollbackAsync();

        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", datos.FamiliaId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_VERSIONES_FORMULARIO", "VER_ID", datos.VersionId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_RIESGOS", "RIE_ID", datos.RiesgoId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_EVALUACIONES_RIESGO", "EVA_ID", datos.EvaluacionId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_PROYECCIONES_EVALUACION", "PROY_ID", datos.ProyeccionId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_FLUJOS_EVALUACION", "FLU_ID", datos.FlujoId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_EVIDENCIAS", "EVI_ID", datos.EvidenciaId));
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task Bloques4y6_ColumnasNuevasPersistenYSeProyectanDesdeOracle()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        (long UsuarioId, long RolId) identidad = await CrearUsuarioIntegracionAsync();
        DatosCiclo? datos = null;
        DatosCiclo? datosOtraEvaluacion = null;
        var evidenciaExtraIds = new List<long>();
        try
        {
            datos = await CrearCicloConfirmadoAsync("BLOQUE4_BLOQUE6_DB_ROUNDTRIP", identidad.UsuarioId);
            datosOtraEvaluacion = await CrearCicloConfirmadoAsync("BLOQUE6_ALERT_ISOLATION", identidad.UsuarioId);
            var db = new OracleDbContext(_connectionString!);
            var auditoria = new AuditoriaRepository(db, new HttpContextAccessor());
            var mitigacion = new MatricesRiesgosMitigacionRepository(db, auditoria);
            var monitoreo = new MatricesRiesgosMonitoreoRepository(db, auditoria);
            var matriz = new MatricesRiesgosRepository(db, auditoria);

            var planDto = new PlanMitigacionGuardarDto
            {
                PlaEvaluacionId = datos.EvaluacionId,
                PlaDescripcion = "Plan Oracle de certificación",
                PlaAvance = 10,
                PlaPresupuesto = 12500.50m,
                PlaFechaInicio = DateTime.Today,
                PlaFechaFin = DateTime.Today.AddDays(30),
                PlaEstado = "PENDIENTE",
                PlaMonitoreoSeguimiento = "Seguimiento inicial",
                PlaResponsables = "Unidad QA",
                PlaRecursos = "Recursos de integración"
            };
            long planId = await mitigacion.CrearPlanAsync(planDto, datos.UsuarioId, "127.0.0.1");
            var planCreado = Assert.Single((await mitigacion.ObtenerBloque4Async(datos.EvaluacionId))!.Planes);
            Assert.Equal("Recursos de integración", planCreado.PlaRecursos);
            Assert.Equal("Unidad QA", planCreado.PlaResponsables);
            Assert.Equal("Seguimiento inicial", planCreado.PlaMonitoreoSeguimiento);

            planDto.PlaRecursos = "Recursos actualizados";
            planDto.PlaResponsables = "Unidad QA y Operaciones";
            planDto.PlaMonitoreoSeguimiento = "Seguimiento posterior";
            Assert.True(await mitigacion.ActualizarPlanAsync(planId, planDto, datos.UsuarioId, "127.0.0.1"));
            var planActualizado = Assert.Single((await mitigacion.ObtenerBloque4Async(datos.EvaluacionId))!.Planes);
            Assert.Equal("Recursos actualizados", planActualizado.PlaRecursos);
            Assert.Equal("Unidad QA y Operaciones", planActualizado.PlaResponsables);
            Assert.Equal("Seguimiento posterior", planActualizado.PlaMonitoreoSeguimiento);
            Assert.Equal(12500.50m, planActualizado.PlaPresupuesto);

            var controlesEsperados = new[]
            {
                new { Tipo = "PREVENTIVO", Estado = "EN_SEGUIMIENTO", Efectividad = 80m, Base = 75m },
                new { Tipo = "DETECTIVO", Estado = "REVISADO", Efectividad = 70m, Base = 65m },
                new { Tipo = "CORRECTIVO", Estado = "FINALIZADO", Efectividad = 60m, Base = 55m }
            };
            var controlPorTipo = new Dictionary<string, long>(StringComparer.Ordinal);
            for (int index = 0; index < controlesEsperados.Length; index++)
            {
                var esperado = controlesEsperados[index];
                long controlId = await mitigacion.CrearControlAsync(new ControlRiesgoGuardarDto
                {
                    ConEvaluacionId = datos.EvaluacionId,
                    ConTipo = esperado.Tipo,
                    ConDescripcion = "Control " + esperado.Tipo + " Oracle",
                    ConAutomatizacion = "MANUAL",
                    ConEstado = "ACTIVO",
                    ConEstadoMonitoreo = esperado.Estado,
                    ConEfectividadMonitoreo = esperado.Efectividad
                }, datos.UsuarioId, "127.0.0.1");
                controlPorTipo.Add(esperado.Tipo, controlId);
                await mitigacion.RegistrarEvaluacionControlAsync(controlId,
                    new EvaluacionControlGuardarDto { EcoEfectividad = esperado.Base, EcoComentario = "Efectividad base" },
                    datos.UsuarioId, "127.0.0.1");
            }

            long evidenciaPreventivaExtraId = await CrearEvidenciaExtraAsync(datos.UsuarioId, "preventiva");
            long evidenciaDetectivaId = await CrearEvidenciaExtraAsync(datos.UsuarioId, "detectiva");
            long evidenciaDetectivaExtraId = await CrearEvidenciaExtraAsync(datos.UsuarioId, "detectiva");
            long evidenciaCorrectivaId = await CrearEvidenciaExtraAsync(datos.UsuarioId, "correctiva");
            long evidenciaCorrectivaExtraId = await CrearEvidenciaExtraAsync(datos.UsuarioId, "correctiva");
            evidenciaExtraIds.AddRange(new[]
            {
                evidenciaPreventivaExtraId,
                evidenciaDetectivaId,
                evidenciaDetectivaExtraId,
                evidenciaCorrectivaId,
                evidenciaCorrectivaExtraId
            });

            var evidenciasPorTipo = new Dictionary<string, long[]>(StringComparer.Ordinal)
            {
                ["PREVENTIVO"] = new[] { datos.EvidenciaId, evidenciaPreventivaExtraId },
                ["DETECTIVO"] = new[] { evidenciaDetectivaId, evidenciaDetectivaExtraId },
                ["CORRECTIVO"] = new[] { evidenciaCorrectivaId, evidenciaCorrectivaExtraId }
            };
            foreach ((string tipo, long[] evidenciaIds) in evidenciasPorTipo)
            {
                foreach (long evidenciaId in evidenciaIds)
                {
                    Assert.True(await matriz.VincularEvidenciaAsync(new VincularEvidenciaDto
                    {
                        EvidenciaId = evidenciaId,
                        TipoEntidad = TipoEntidadEvidencia.Control,
                        EntidadId = controlPorTipo[tipo]
                    }, datos.UsuarioId, "127.0.0.1"));
                }
            }

            string sufijo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            await monitoreo.CrearAlertaAsync(new SenalAlertaGuardarDto
            {
                AleEvaluacionId = datos.EvaluacionId,
                AleCodigo = "ORACLE_" + sufijo + "_A",
                AleIndicador = "Alerta de integración A",
                AleEstado = "ACTIVO"
            }, datos.UsuarioId, "127.0.0.1");
            await monitoreo.CrearAlertaAsync(new SenalAlertaGuardarDto
            {
                AleEvaluacionId = datos.EvaluacionId,
                AleCodigo = "ORACLE_" + sufijo + "_B",
                AleIndicador = "Alerta de integración B",
                AleEstado = "INACTIVO"
            }, datos.UsuarioId, "127.0.0.1");
            await monitoreo.CrearAlertaAsync(new SenalAlertaGuardarDto
            {
                AleEvaluacionId = datosOtraEvaluacion.EvaluacionId,
                AleCodigo = "ORACLE_" + sufijo + "_OTHER_EVALUATION",
                AleIndicador = "Alerta de otra evaluación",
                AleEstado = "ACTIVO"
            }, datos.UsuarioId, "127.0.0.1");
            await monitoreo.RegistrarAutomonitoreoAsync(new AutomonitoreoGuardarDto
            {
                MonEvaluacionId = datos.EvaluacionId,
                MonEstadoRiesgo = "ALTO",
                MonEstadoContr = "EN_SEGUIMIENTO",
                MonResultado = "Evento de integración"
            }, datos.UsuarioId, "127.0.0.1");

            await AsignarCapacidadesIntegracionAsync(datos.UsuarioId, false, false);
            var sinPermisos = await monitoreo.ObtenerBloque6Async(datos.EvaluacionId, datos.UsuarioId);
            Assert.False(sinPermisos.PuedeEditarObservacionesArea);
            Assert.False(sinPermisos.PuedeEditarObservacionesUgr);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => monitoreo.ActualizarObservacionAsync(
                datos.EvaluacionId, true, "Debe denegarse", datos.UsuarioId, "127.0.0.1"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => monitoreo.ActualizarObservacionAsync(
                datos.EvaluacionId, false, "Debe denegarse", datos.UsuarioId, "127.0.0.1"));

            await AsignarCapacidadesIntegracionAsync(datos.UsuarioId, true, false);
            Assert.True(await monitoreo.ActualizarObservacionAsync(datos.EvaluacionId, true,
                "Observación Área intermedia", datos.UsuarioId, "127.0.0.1"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => monitoreo.ActualizarObservacionAsync(
                datos.EvaluacionId, false, "Intento Área hacia UGR", datos.UsuarioId, "127.0.0.1"));
            var soloArea = await monitoreo.ObtenerBloque6Async(datos.EvaluacionId, datos.UsuarioId);
            Assert.Equal("Observación Área intermedia", soloArea.ObservacionesArea);
            Assert.Null(soloArea.ObservacionesUgr);

            await AsignarCapacidadesIntegracionAsync(datos.UsuarioId, false, true);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => monitoreo.ActualizarObservacionAsync(
                datos.EvaluacionId, true, "Intento UGR hacia Área", datos.UsuarioId, "127.0.0.1"));
            Assert.True(await monitoreo.ActualizarObservacionAsync(datos.EvaluacionId, false,
                "Observación UGR intermedia", datos.UsuarioId, "127.0.0.1"));
            var soloUgr = await monitoreo.ObtenerBloque6Async(datos.EvaluacionId, datos.UsuarioId);
            Assert.Equal("Observación Área intermedia", soloUgr.ObservacionesArea);
            Assert.Equal("Observación UGR intermedia", soloUgr.ObservacionesUgr);

            await AsignarCapacidadesIntegracionAsync(datos.UsuarioId, true, true);
            Assert.True(await monitoreo.ActualizarObservacionAsync(datos.EvaluacionId, true,
                "Observación Área Oracle", datos.UsuarioId, "127.0.0.1"));
            Assert.True(await monitoreo.ActualizarObservacionAsync(datos.EvaluacionId, false,
                "Observación UGR Oracle", datos.UsuarioId, "127.0.0.1"));

            var bloque6 = await monitoreo.ObtenerBloque6Async(datos.EvaluacionId, datos.UsuarioId);
            Assert.Equal("ALTO", bloque6.EstadoRiesgo);
            Assert.Equal("Observación Área Oracle", bloque6.ObservacionesArea);
            Assert.Equal("Observación UGR Oracle", bloque6.ObservacionesUgr);
            Assert.True(bloque6.PuedeEditarObservacionesArea);
            Assert.True(bloque6.PuedeEditarObservacionesUgr);
            Assert.Equal(2, bloque6.SenalesAlerta.Count);
            Assert.DoesNotContain(bloque6.SenalesAlerta, alerta => alerta.AleIndicador == "Alerta de otra evaluación");
            Assert.Equal(3, bloque6.Controles.Count);
            foreach (var esperado in controlesEsperados)
            {
                var control = Assert.Single(bloque6.Controles.Where(item => item.Tipo == esperado.Tipo));
                Assert.Equal(esperado.Estado, control.EstadoMonitoreo);
                Assert.Equal(esperado.Efectividad, control.EfectividadMonitoreo);
                long[] evidenciaEsperada = evidenciasPorTipo[esperado.Tipo];
                Assert.Equal(2, control.Evidencias.Count);
                Assert.Equal(evidenciaEsperada.OrderBy(id => id), control.Evidencias.Select(evidencia => evidencia.Id).OrderBy(id => id));
                Assert.DoesNotContain(control.Evidencias,
                    evidencia => evidenciasPorTipo.Where(pair => pair.Key != esperado.Tipo)
                        .SelectMany(pair => pair.Value).Contains(evidencia.Id));
            }

            foreach (var esperado in controlesEsperados)
            {
                var controlLeido = await mitigacion.ObtenerControlAsync(controlPorTipo[esperado.Tipo]);
                Assert.NotNull(controlLeido);
                Assert.Equal("ACTIVO", controlLeido.ConEstado);
                Assert.Equal(esperado.Base, Assert.Single(await mitigacion.ListarEvaluacionesControlAsync(controlPorTipo[esperado.Tipo])).EcoEfectividad);
            }

            await UsarNuevaSesionAsync();
            var bloque6Releido = await monitoreo.ObtenerBloque6Async(datos.EvaluacionId, datos.UsuarioId);
            Assert.Equal("Observación Área Oracle", bloque6Releido.ObservacionesArea);
            Assert.Equal("Observación UGR Oracle", bloque6Releido.ObservacionesUgr);
            Assert.Equal("Recursos actualizados",
                Assert.Single((await mitigacion.ObtenerBloque4Async(datos.EvaluacionId))!.Planes).PlaRecursos);
        }
        finally
        {
            if (datos is not null)
            {
                await LimpiarDatosBloque4y6Async(datos, evidenciaExtraIds);
                await LimpiarCicloAsync(datos);
            }
            if (datosOtraEvaluacion is not null)
            {
                await LimpiarDatosBloque4y6Async(datosOtraEvaluacion, Array.Empty<long>());
                await LimpiarCicloAsync(datosOtraEvaluacion);
            }
            await EliminarUsuarioIntegracionAsync(identidad.UsuarioId, identidad.RolId);
        }
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task VinculoGenericoYAuditoria_FalloPosteriorAInsertarAuditoria_RevierteAmbosRegistros()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        DatosCiclo datos = await CrearCicloConfirmadoAsync("ROLLBACK_AUD");
        try
        {
            var db = new OracleDbContext(_connectionString!);
            var auditoriaReal = new AuditoriaRepository(db, new HttpContextAccessor());
            var auditoriaConFallo = new AuditoriaFallaDespuesDeInsertar(auditoriaReal);
            var repository = new MatricesRiesgosRepository(db, auditoriaConFallo);

            int auditoriasAntes = await ContarAuditoriasVinculoAsync();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.VincularEvidenciaAsync(
                    new VincularEvidenciaDto
                    {
                        EvidenciaId = datos.EvidenciaId,
                        TipoEntidad = TipoEntidadEvidencia.Evaluacion,
                        EntidadId = datos.EvaluacionId
                    },
                    datos.UsuarioId,
                    "127.0.0.1"));

            Assert.Contains("Fallo controlado posterior a la auditoría", error.Message);

            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            Assert.Equal(0, await ContarVinculosAsync(conn, datos));
            Assert.Equal(auditoriasAntes, await ContarAuditoriasVinculoAsync());
        }
        finally
        {
            await LimpiarCicloAsync(datos);
        }
    }

    private async Task<bool> ValidarEntornoEjecucionAsync()
    {
        if (!_integrationRequired)
        {
            _output.WriteLine(
                "[OMITIDA] Certificación Oracle no habilitada. " +
                "RL_ORACLE_INTEGRATION_REQUIRED debe ser true. " +
                "Este resultado no certifica físicamente el modelo de 17 tablas.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            Assert.Fail(
                "RL_ORACLE_INTEGRATION_REQUIRED=true, pero no existe una cadena Oracle segura en variables de entorno o User Secrets.");
            return false;
        }

        try
        {
            // La suite puede ejecutarse después de otra clase Oracle dentro del
            // mismo proceso. El timeout observado era una solicitud al pool,
            // no una consulta lenta ni un fallo del esquema.
            OracleConnection.ClearAllPools();
            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            await using var schemaCmd = new OracleCommand(
                "SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL",
                conn);
            string esquema = Convert.ToString(await schemaCmd.ExecuteScalarAsync()) ?? string.Empty;

            if (!string.Equals(esquema, "RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"Esquema Oracle no autorizado para certificación: {esquema}.");
                return false;
            }

            await ValidarContratoFisicoAsync(conn);
            return true;
        }
        catch (Exception ex)
        {
            Assert.Fail($"No fue posible validar de forma segura el entorno Oracle: {ex.Message}");
            return false;
        }
    }

    private static async Task ValidarContratoFisicoAsync(OracleConnection conn)
    {
        string[] tablasActuales = await ObtenerNombresAsync(
            conn,
            "SELECT TABLE_NAME FROM USER_TABLES WHERE TABLE_NAME LIKE 'RL_MR_%' ORDER BY TABLE_NAME");
        string[] tablasEsperadas = TablasModelo17
            .Concat(TablasConfiguracionCalculo)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(tablasEsperadas, tablasActuales);

        string[] secuenciasActuales = await ObtenerNombresAsync(
            conn,
            "SELECT SEQUENCE_NAME FROM USER_SEQUENCES WHERE SEQUENCE_NAME LIKE 'SEQ_RL_MR_%' ORDER BY SEQUENCE_NAME");
        string[] secuenciasEsperadas = SecuenciasModelo17
            .Concat(SecuenciasConfiguracionCalculo)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(secuenciasEsperadas, secuenciasActuales);

        Assert.Equal(1, await ContarObjetoAsync(conn, "USER_TABLES", "TABLE_NAME", "RL_USUARIOS"));
        Assert.Equal(1, await ContarObjetoAsync(conn, "USER_TABLES", "TABLE_NAME", "RL_AUDITORIA"));
        Assert.Equal(1, await ContarObjetoAsync(conn, "USER_SEQUENCES", "SEQUENCE_NAME", "SEQ_RL_AUDITORIA"));

        foreach (string tabla in TablasRetiradas)
        {
            Assert.Equal(0, await ContarObjetoAsync(conn, "USER_TABLES", "TABLE_NAME", tabla));
        }

        foreach (string secuencia in SecuenciasRetiradas)
        {
            Assert.Equal(0, await ContarObjetoAsync(conn, "USER_SEQUENCES", "SEQUENCE_NAME", secuencia));
        }

        foreach (string indice in IndicesPrincipales)
        {
            Assert.Equal(1, await ContarObjetoAsync(conn, "USER_INDEXES", "INDEX_NAME", indice));
        }

        foreach (string restriccion in RestriccionesPrincipales)
        {
            await using var cmd = new OracleCommand(@"
                SELECT COUNT(*)
                  FROM USER_CONSTRAINTS
                 WHERE CONSTRAINT_NAME = :nombre
                   AND STATUS = 'ENABLED'", conn)
            {
                BindByName = true
            };
            cmd.Parameters.Add(new OracleParameter("nombre", restriccion));
            Assert.Equal(1, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
        }
    }

    private async Task<DatosCiclo> CrearCicloConfirmadoAsync(string escenario, long? usuarioId = null)
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        try
        {
            DatosCiclo datos = await InsertarCicloAsync(conn, transaction, escenario, usuarioId);
            await transaction.CommitAsync();
            return datos;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<DatosCiclo> InsertarCicloAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string escenario,
        long? usuarioId = null)
    {
        long usuarioFixtureId = usuarioId ?? await ObtenerUsuarioValidoAsync(conn, transaction);
        long familiaId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_FAMILIAS");
        long versionId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_VERSIONES");
        long riesgoId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_RIESGOS");
        long evaluacionId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_EVALUACIONES");
        long proyeccionId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_PROYECCIONES");
        long flujoId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_FLUJOS");
        long evidenciaId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_EVIDENCIAS");
        string sufijo = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        string codigoBase = PrefijoPrueba + sufijo;

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
                  FAM_ID, FAM_CODIGO, FAM_NOMBRE, FAM_DESCRIPCION, FAM_ACTIVO
              ) VALUES (
                  :id, :codigo, :nombre, :descripcion, 1
              )",
            new OracleParameter("id", familiaId),
            new OracleParameter("codigo", codigoBase),
            new OracleParameter("nombre", "Familia certificación " + escenario),
            new OracleParameter("descripcion", "Registro aislado de certificación Oracle"));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                  VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH,
                  VER_ESTADO, VER_VIGENTE, VER_FECHA_INICIO, VER_USR_CREACION
              ) VALUES (
                  :id, :familiaId, :codigo, 1, :json, :hash,
                  'PUBLISHED', 1, SYSDATE, :usuarioId
              )",
            new OracleParameter("id", versionId),
            new OracleParameter("familiaId", familiaId),
            new OracleParameter("codigo", "V_" + sufijo),
            CrearClob("json", "{\"sections\":[]}"),
            new OracleParameter("hash", sufijo.PadRight(64, '0')),
            new OracleParameter("usuarioId", usuarioFixtureId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_RIESGOS (
                  RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION,
                  RIE_ACTIVO, RIE_USR_CREACION
              ) VALUES (
                  :id, :codigo, :nombre, :descripcion, 1, :usuarioId
              )",
            new OracleParameter("id", riesgoId),
            new OracleParameter("codigo", "R_" + sufijo),
            new OracleParameter("nombre", "Riesgo certificación " + escenario),
            new OracleParameter("descripcion", "Riesgo aislado para validar el modelo de 17 tablas"),
            new OracleParameter("usuarioId", usuarioFixtureId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                  EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_DATOS_JSON,
                  EVA_CALCULOS_JSON, EVA_USR_REGISTRO, EVA_VERSION_ROW, EVA_ACTIVO
              ) VALUES (
                  :id, :riesgoId, :versionId, :datosJson,
                  :calculosJson, :usuarioId, 1, 1
              )",
            new OracleParameter("id", evaluacionId),
            new OracleParameter("riesgoId", riesgoId),
            new OracleParameter("versionId", versionId),
            CrearClob("datosJson", "{\"area\":\"PRUEBAS\",\"dueno\":\"CERTIFICACION\"}"),
            CrearClob("calculosJson", "{\"reglaCodigo\":\"CALCULO_VRI_VRR\",\"reglaVersion\":\"1.0\",\"algoritmoId\":\"MATRICES_VRI_ADITIVO_1_9\",\"vri\":7,\"vrr\":4}"),
            new OracleParameter("usuarioId", usuarioFixtureId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_PROYECCIONES_EVALUACION (
                  PROY_ID, PROY_EVALUACION_ID, PROY_CODIGO_RIESGO,
                  PROY_AREA_PRINCIPAL, PROY_VRI, PROY_VRR,
                  PROY_NIVEL_INHERENTE, PROY_NIVEL_RESIDUAL,
                  PROY_RESPUESTA_RIESGO, PROY_ESTADO_EVALUACION,
                  PROY_DUENO_RIESGO, PROY_FECHA_EVAL
              ) VALUES (
                  :id, :evaluacionId, :codigoRiesgo,
                  'PRUEBAS', 7, 4,
                  'ALTO', 'MEDIO',
                  'MITIGAR', 'BORRADOR',
                  'CERTIFICACION', SYSDATE
              )",
            new OracleParameter("id", proyeccionId),
            new OracleParameter("evaluacionId", evaluacionId),
            new OracleParameter("codigoRiesgo", "R_" + sufijo));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_FLUJOS_EVALUACION (
                  FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO, FLU_MOTIVO, FLU_USR_ID
              ) VALUES (
                  :id, :evaluacionId, 'BORRADOR', :motivo, :usuarioId
              )",
            new OracleParameter("id", flujoId),
            new OracleParameter("evaluacionId", evaluacionId),
            new OracleParameter("motivo", "Captura inicial de certificación"),
            new OracleParameter("usuarioId", usuarioFixtureId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_EVIDENCIAS (
                  EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO,
                  EVI_HASH, EVI_RUTA, EVI_USR_CREACION
              ) VALUES (
                  :id, :nombre, 'txt', 1,
                  :hash, :ruta, :usuarioId
              )",
            new OracleParameter("id", evidenciaId),
            new OracleParameter("nombre", codigoBase + ".txt"),
            new OracleParameter("hash", (escenario + sufijo).PadRight(64, '0')[..64]),
            new OracleParameter("ruta", "/certificacion-oracle/" + codigoBase),
            new OracleParameter("usuarioId", usuarioFixtureId));

        return new DatosCiclo(
            familiaId,
            versionId,
            riesgoId,
            evaluacionId,
            proyeccionId,
            flujoId,
            evidenciaId,
            usuarioFixtureId);
    }

    private async Task LimpiarCicloAsync(DatosCiclo datos)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        try
        {
            var vinculos = new List<long>();
            await using (var buscarCmd = new OracleCommand(@"
                SELECT EVV_ID
                  FROM RL_MR_EVIDENCIAS_VINCULOS
                 WHERE EVV_EVIDENCIA_ID = :evidenciaId", conn)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                buscarCmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
                await using OracleDataReader reader = await buscarCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vinculos.Add(reader.GetInt64(0));
                }
            }

            foreach (long vinculoId in vinculos)
            {
                await EjecutarAsync(
                    conn,
                    transaction,
                    "DELETE FROM RL_AUDITORIA WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS' AND AUD_REGISTRO_ID = :id",
                    new OracleParameter("id", vinculoId.ToString()));
            }

            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_EVIDENCIAS_VINCULOS WHERE EVV_EVIDENCIA_ID = :id",
                new OracleParameter("id", datos.EvidenciaId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id",
                new OracleParameter("id", datos.EvidenciaId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID = :id",
                new OracleParameter("id", datos.RiesgoId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID = :id",
                new OracleParameter("id", datos.VersionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_ID = :id",
                new OracleParameter("id", datos.FamiliaId));

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _output.WriteLine($"[ADVERTENCIA] Limpieza Oracle incompleta: {ex.Message}");
        }
    }

    private async Task<(long UsuarioId, long RolId)> CrearUsuarioIntegracionAsync()
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();
        string sufijo = Guid.NewGuid().ToString("N")[..12];
        long rolId = await SiguienteIdentidadAsync(conn, transaction, "RL_ROLES", "ROL_ID");
        long usuarioId = await SiguienteIdentidadAsync(conn, transaction, "RL_USUARIOS", "USR_ID");
        await using (var role = new OracleCommand(
            "INSERT INTO RL_ROLES (ROL_ID, ROL_NOMBRE, ROL_DESCRIPCION, ROL_ACTIVO) VALUES (:id, :nombre, :descripcion, 1)",
            conn)
        {
            BindByName = true,
            Transaction = transaction
        })
        {
            role.Parameters.Add(new OracleParameter("nombre", "IT_B6_" + sufijo));
            role.Parameters.Add(new OracleParameter("descripcion", "Usuario temporal de integración Oracle"));
            role.Parameters.Add(new OracleParameter("id", rolId));
            await role.ExecuteNonQueryAsync();
        }

        await using (var user = new OracleCommand(@"
            INSERT INTO RL_USUARIOS (
                USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL, USR_PASSWORD_HASH,
                USR_PASSWORD_SALT, USR_ROL_ID, USR_ACTIVO, ES_USUARIO_DOMINIO
            ) VALUES (:id, 'Integración', 'Bloque 6', :email,
                      'TEST_ONLY', 'TEST_ONLY', :rolId, 1, 0)", conn)
        {
            BindByName = true,
            Transaction = transaction
        })
        {
            user.Parameters.Add(new OracleParameter("email", $"oracle-b6-{sufijo}@example.invalid"));
            user.Parameters.Add(new OracleParameter("rolId", rolId));
            user.Parameters.Add(new OracleParameter("id", usuarioId));
            await user.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return (usuarioId, rolId);
    }

    private static async Task<long> SiguienteIdentidadAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string tableName,
        string columnName)
    {
        await using var command = new OracleCommand(
            $"SELECT NVL(MAX({columnName}), 0) + 1 FROM {tableName}",
            conn)
        {
            BindByName = true,
            Transaction = transaction
        };

        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private async Task AsignarCapacidadesIntegracionAsync(long usuarioId, bool area, bool ugr)
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();
        await EjecutarAsync(conn, transaction,
            "DELETE FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID = :id",
            new OracleParameter("id", usuarioId));
        if (area)
        {
            await EjecutarAsync(conn, transaction, @"
                INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID, UCP_CAPACIDAD, UCP_ACTIVO)
                VALUES (:id, 'MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR', 1)",
                new OracleParameter("id", usuarioId));
        }
        if (ugr)
        {
            await EjecutarAsync(conn, transaction, @"
                INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID, UCP_CAPACIDAD, UCP_ACTIVO)
                VALUES (:id, 'MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR', 1)",
                new OracleParameter("id", usuarioId));
        }
        await transaction.CommitAsync();
    }

    private async Task<long> CrearEvidenciaExtraAsync(long usuarioId, string tipo)
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();
        long evidenciaId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_EVIDENCIAS");
        string sufijo = Guid.NewGuid().ToString("N");
        await EjecutarAsync(conn, transaction, @"
            INSERT INTO RL_MR_EVIDENCIAS (
                EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO,
                EVI_HASH, EVI_RUTA, EVI_USR_CREACION
            ) VALUES (
                :id, :nombre, 'txt', 1, :hash, :ruta, :usuarioId
            )",
            new OracleParameter("id", evidenciaId),
            new OracleParameter("nombre", "bloque6-" + tipo + "-" + sufijo + ".txt"),
            new OracleParameter("hash", sufijo.PadRight(64, '0')),
            new OracleParameter("ruta", "/certificacion-oracle/bloque6/" + tipo + "/" + sufijo),
            new OracleParameter("usuarioId", usuarioId));
        await transaction.CommitAsync();

        return evidenciaId;
    }

    private async Task LimpiarDatosBloque4y6Async(DatosCiclo datos, IReadOnlyCollection<long> evidenciaExtraIds)
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();
        try
        {
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_AUDITORIA WHERE AUD_USR_ID = :id",
                new OracleParameter("id", datos.UsuarioId));
            await EjecutarAsync(conn, transaction, @"
                DELETE FROM RL_MR_EVIDENCIAS_VINCULOS
                 WHERE EVV_TIPO_ENTIDAD = 'CONTROL'
                   AND EVV_ENTIDAD_ID IN (SELECT CON_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID = :id)",
                new OracleParameter("id", datos.EvaluacionId));
            foreach (long evidenciaId in evidenciaExtraIds)
            {
                await EjecutarAsync(conn, transaction, "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id",
                    new OracleParameter("id", evidenciaId));
            }
            await EjecutarAsync(conn, transaction, @"
                DELETE FROM RL_MR_EVALUACIONES_CONTROL
                 WHERE ECO_CONTROL_ID IN (SELECT CON_ID FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID = :id)",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction, @"
                DELETE FROM RL_MR_ACTIVIDADES
                 WHERE ACT_PLAN_ID IN (SELECT PLA_ID FROM RL_MR_PLANES WHERE PLA_EVALUACION_ID = :id)",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_MR_PLANES WHERE PLA_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_MR_SENALES_ALERTA WHERE ALE_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_MR_AUTOMONITOREO WHERE MON_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_USUARIO_CAPACIDADES WHERE UCP_USR_ID = :id",
                new OracleParameter("id", datos.UsuarioId));
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task EliminarUsuarioIntegracionAsync(long usuarioId, long rolId)
    {
        if (string.IsNullOrWhiteSpace(_connectionString)) return;
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();
        try
        {
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_USUARIOS WHERE USR_ID = :id",
                new OracleParameter("id", usuarioId));
            await EjecutarAsync(conn, transaction, "DELETE FROM RL_ROLES WHERE ROL_ID = :id",
                new OracleParameter("id", rolId));
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static Task UsarNuevaSesionAsync()
    {
        OracleConnection.ClearAllPools();
        return Task.CompletedTask;
    }

    private static async Task<string[]> ObtenerNombresAsync(OracleConnection conn, string sql)
    {
        var nombres = new List<string>();
        await using var cmd = new OracleCommand(sql, conn);
        await using OracleDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            nombres.Add(reader.GetString(0));
        }

        return nombres.ToArray();
    }

    private static async Task<int> ContarObjetoAsync(
        OracleConnection conn,
        string vista,
        string columna,
        string nombre)
    {
        if ((vista, columna) is not
            ("USER_TABLES", "TABLE_NAME") and not
            ("USER_SEQUENCES", "SEQUENCE_NAME") and not
            ("USER_INDEXES", "INDEX_NAME"))
        {
            throw new InvalidOperationException("Vista de metadatos no autorizada en la certificación.");
        }

        await using var cmd = new OracleCommand(
            $"SELECT COUNT(*) FROM {vista} WHERE {columna} = :nombre",
            conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("nombre", nombre));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> ContarPorIdAsync(
        OracleConnection conn,
        string tabla,
        string columna,
        long id)
    {
        var destinos = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RL_MR_FAMILIAS_FORMULARIO"] = "FAM_ID",
            ["RL_MR_VERSIONES_FORMULARIO"] = "VER_ID",
            ["RL_MR_RIESGOS"] = "RIE_ID",
            ["RL_MR_EVALUACIONES_RIESGO"] = "EVA_ID",
            ["RL_MR_PROYECCIONES_EVALUACION"] = "PROY_ID",
            ["RL_MR_FLUJOS_EVALUACION"] = "FLU_ID",
            ["RL_MR_EVIDENCIAS"] = "EVI_ID"
        };

        if (!destinos.TryGetValue(tabla, out string? columnaPermitida) ||
            !string.Equals(columnaPermitida, columna, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Destino no autorizado en la certificación Oracle.");
        }

        await using var cmd = new OracleCommand(
            $"SELECT COUNT(*) FROM {tabla} WHERE {columna} = :id",
            conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("id", id));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<long> ObtenerVinculoIdAsync(OracleConnection conn, DatosCiclo datos)
    {
        await using var cmd = new OracleCommand(@"
            SELECT EVV_ID
              FROM RL_MR_EVIDENCIAS_VINCULOS
             WHERE EVV_EVIDENCIA_ID = :evidenciaId
               AND EVV_TIPO_ENTIDAD = 'EVALUACION'
               AND EVV_ENTIDAD_ID = :evaluacionId", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
        cmd.Parameters.Add(new OracleParameter("evaluacionId", datos.EvaluacionId));
        object? resultado = await cmd.ExecuteScalarAsync();
        return resultado is null ? 0 : Convert.ToInt64(resultado);
    }

    private static async Task<int> ContarVinculosAsync(OracleConnection conn, DatosCiclo datos)
    {
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_MR_EVIDENCIAS_VINCULOS
             WHERE EVV_EVIDENCIA_ID = :evidenciaId
               AND EVV_TIPO_ENTIDAD = 'EVALUACION'
               AND EVV_ENTIDAD_ID = :evaluacionId", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
        cmd.Parameters.Add(new OracleParameter("evaluacionId", datos.EvaluacionId));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> ContarAuditoriaAsync(OracleConnection conn, long vinculoId)
    {
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_AUDITORIA
             WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS'
               AND AUD_REGISTRO_ID = :registroId
               AND AUD_ACCION = 'INSERT'", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("registroId", vinculoId.ToString()));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> ContarAuditoriasVinculoAsync()
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_AUDITORIA
             WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS'
               AND AUD_ACCION = 'VINCULAR_EVIDENCIA'", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<long> ObtenerUsuarioValidoAsync(
        OracleConnection conn,
        OracleTransaction transaction)
    {
        await using var cmd = new OracleCommand(@"
            SELECT USR_ID
              FROM RL_USUARIOS
             WHERE ROWNUM = 1", conn)
        {
            Transaction = transaction
        };
        object? resultado = await cmd.ExecuteScalarAsync();
        return resultado is null
            ? throw new InvalidOperationException("No existe un usuario válido para la certificación Oracle.")
            : Convert.ToInt64(resultado);
    }

    private static async Task<long> SiguienteSecuenciaAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string secuencia)
    {
        if (!SecuenciasModelo17.Contains(secuencia, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Secuencia no autorizada en la certificación Oracle.");
        }

        await using var cmd = new OracleCommand($"SELECT {secuencia}.NEXTVAL FROM DUAL", conn)
        {
            Transaction = transaction
        };
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private OracleConnection CrearConexion()
    {
        var builder = new OracleConnectionStringBuilder(_connectionString!)
        {
            ConnectionTimeout = 30
        };
        return new OracleConnection(builder.ConnectionString);
    }

    private static OracleParameter CrearClob(string nombre, string valor) =>
        new(nombre, OracleDbType.Clob) { Value = valor };

    private static async Task EjecutarAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string sql,
        params OracleParameter[] parameters)
    {
        await using var cmd = new OracleCommand(sql, conn)
        {
            BindByName = true,
            Transaction = transaction
        };
        cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed record DatosCiclo(
        long FamiliaId,
        long VersionId,
        long RiesgoId,
        long EvaluacionId,
        long ProyeccionId,
        long FlujoId,
        long EvidenciaId,
        long UsuarioId);

    private sealed class AuditoriaFallaDespuesDeInsertar : IAuditoriaRepository
    {
        private readonly IAuditoriaRepository _inner;

        public AuditoriaFallaDespuesDeInsertar(IAuditoriaRepository inner)
        {
            _inner = inner;
        }

        public Task RegistrarAsync(
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo) =>
            _inner.RegistrarAsync(
                tabla,
                registroId,
                accion,
                datosAnt,
                datosNvo,
                usrId,
                email,
                ip,
                modulo);

        public async Task RegistrarAsync(
            OracleConnection connection,
            OracleTransaction? transaction,
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo)
        {
            await _inner.RegistrarAsync(
                connection,
                transaction,
                tabla,
                registroId,
                accion,
                datosAnt,
                datosNvo,
                usrId,
                email,
                ip,
                modulo);

            throw new InvalidOperationException("Fallo controlado posterior a la auditoría.");
        }

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(
            int pagina,
            int limite,
            string? buscar,
            string? accion,
            string? modulo,
            string? tabla,
            DateTime? fechaInicio,
            DateTime? fechaFin) =>
            _inner.ObtenerBitacoraPaginadaAsync(
                pagina,
                limite,
                buscar,
                accion,
                modulo,
                tabla,
                fechaInicio,
                fechaFin);
    }
}
