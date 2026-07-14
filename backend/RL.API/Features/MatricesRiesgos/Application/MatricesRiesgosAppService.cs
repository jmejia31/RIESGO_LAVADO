using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Services;
using System.Net;
using System.Text;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class MatricesRiesgosAppService : IMatricesRiesgosAppService
{
    private static readonly HashSet<string> SujetosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "PROVEEDOR",
        "CLIENTE_PATRONO",
        "EMPLEADO",
        "AREA",
        "PROCESO",
        "CASO_POSITIVO",
        "INSTITUCIONAL"
    };

    private static readonly HashSet<string> EstadosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "BORRADOR",
        "EN_EVALUACION",
        "CALCULADA",
        "EN_REVISION",
        "OBSERVADA",
        "APROBADA",
        "CERRADA",
        "INACTIVA"
    };

    private readonly IMatricesRiesgosRepository _repo;
    private readonly IMatricesRiesgoService _motorCalculo;

    public MatricesRiesgosAppService(IMatricesRiesgosRepository repo, IMatricesRiesgoService motorCalculo)
    {
        _repo = repo;
        _motorCalculo = motorCalculo;
    }

    public async Task<ServiceResult<MetodologiaCalculoDto>> ObtenerMetodologiaVigenteAsync()
    {
        var metodologia = await _repo.ObtenerMetodologiaVigenteAsync();
        return metodologia == null
            ? ServiceResult<MetodologiaCalculoDto>.NotFound("No existe una metodología aprobada vigente para Matrices de Riesgos.")
            : ServiceResult<MetodologiaCalculoDto>.Ok(metodologia);
    }

    public async Task<ServiceResult<MatricesRiesgoDashboardDto>> ObtenerDashboardAsync()
    {
        var dashboard = await _repo.ObtenerDashboardAsync();
        return ServiceResult<MatricesRiesgoDashboardDto>.Ok(dashboard);
    }

    public async Task<ServiceResult<MatricesRiesgoReporteDto>> ObtenerReporteAsync(MatrizRiesgoReporteFiltroDto filtro)
    {
        NormalizarFiltroReporte(filtro);
        var reporte = await _repo.ObtenerReporteAsync(filtro);
        return ServiceResult<MatricesRiesgoReporteDto>.Ok(reporte);
    }

    public async Task<ServiceResult<MatrizRiesgoExportacionDto>> ExportarReporteAsync(MatrizRiesgoReporteFiltroDto filtro, string formato, long usuarioId, string? usuarioEmail, string? ip)
    {
        NormalizarFiltroReporte(filtro);
        var formatoNormalizado = string.IsNullOrWhiteSpace(formato) ? "EXCEL" : formato.Trim().ToUpperInvariant();
        if (formatoNormalizado != "EXCEL" && formatoNormalizado != "PDF")
            return ServiceResult<MatrizRiesgoExportacionDto>.BadRequest("El formato de exportación debe ser EXCEL o PDF.");

        var reporte = await _repo.ObtenerReporteAsync(filtro);
        var archivo = formatoNormalizado == "PDF"
            ? ConstruirPdfReporte(reporte)
            : ConstruirExcelReporte(reporte);

        await _repo.RegistrarExportacionReporteAsync(filtro, formatoNormalizado, usuarioId, usuarioEmail, ip);
        return ServiceResult<MatrizRiesgoExportacionDto>.Ok(archivo, "Reporte generado correctamente.");
    }

    public async Task<ServiceResult<List<MatrizRiesgoResumenDto>>> ListarAsync(MatrizRiesgoFiltroDto filtro)
    {
        var datos = await _repo.ListarMatricesAsync(filtro);
        return ServiceResult<List<MatrizRiesgoResumenDto>>.Ok(datos);
    }

    public async Task<ServiceResult<MatrizRiesgoDetalleDto>> ObtenerAsync(long matrizId)
    {
        if (matrizId <= 0)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest("El identificador de la matriz es obligatorio.");

        var matriz = await _repo.ObtenerMatrizAsync(matrizId);
        return matriz == null
            ? ServiceResult<MatrizRiesgoDetalleDto>.NotFound("No se encontró la matriz de riesgos.")
            : ServiceResult<MatrizRiesgoDetalleDto>.Ok(matriz);
    }

    public async Task<ServiceResult<MatrizRiesgoDetalleDto>> CrearAsync(MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        var error = ValidarCreacion(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(error);

        try
        {
            var matrizId = await _repo.CrearMatrizAsync(dto, usuarioId, usuarioEmail, ip);
            var matriz = await _repo.ObtenerMatrizAsync(matrizId);
            return matriz == null
                ? ServiceResult<MatrizRiesgoDetalleDto>.NotFound("La matriz fue creada, pero no pudo consultarse el detalle.")
                : ServiceResult<MatrizRiesgoDetalleDto>.Ok(matriz, "Matriz de riesgos creada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizRiesgoDetalleDto>> ActualizarAsync(long matrizId, MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest("El identificador de la matriz es obligatorio.");

        var error = ValidarCreacion(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(error);

        try
        {
            var ok = await _repo.ActualizarMatrizAsync(matrizId, dto, usuarioId, usuarioEmail, ip);
            if (!ok)
                return ServiceResult<MatrizRiesgoDetalleDto>.NotFound("No se encontró la matriz de riesgos.");

            var matriz = await _repo.ObtenerMatrizAsync(matrizId);
            return matriz == null
                ? ServiceResult<MatrizRiesgoDetalleDto>.NotFound("La matriz fue actualizada, pero no pudo consultarse el detalle.")
                : ServiceResult<MatrizRiesgoDetalleDto>.Ok(matriz, "Matriz de riesgos actualizada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoDetalleDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizCalculoResultadoDto>> CalcularAsync(long matrizId, MatrizRiesgoCalcularRequestDto dto, bool esRecalculo, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest("El identificador de la matriz es obligatorio.");

        if (esRecalculo && string.IsNullOrWhiteSpace(dto.MotivoCalculo))
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest("El motivo de recálculo es obligatorio.");

        try
        {
            var solicitud = await _repo.PrepararSolicitudCalculoAsync(matrizId, dto.TipoCalculo, dto.MotivoCalculo, esRecalculo);
            if (solicitud == null)
                return ServiceResult<MatrizCalculoResultadoDto>.NotFound("No se encontró la matriz de riesgos.");

            var resultado = _motorCalculo.Calcular(solicitud);
            if (!resultado.Success || resultado.Data == null)
                return ServiceResult<MatrizCalculoResultadoDto>.BadRequest(resultado.Message ?? "No se pudo calcular la matriz de riesgos.");

            await _repo.PersistirResultadoCalculoAsync(matrizId, resultado.Data, dto.MotivoCalculo, esRecalculo, usuarioId, usuarioEmail, ip);
            return ServiceResult<MatrizCalculoResultadoDto>.Ok(resultado.Data, esRecalculo ? "Matriz recalculada correctamente." : "Matriz calculada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> CambiarEstadoAsync(long matrizId, MatrizRiesgoCambiarEstadoRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult.BadRequest("El identificador de la matriz es obligatorio.");

        if (dto == null)
            return ServiceResult.BadRequest("La solicitud de cambio de estado es obligatoria.");

        var estado = dto.Estado?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(estado) || !EstadosPermitidos.Contains(estado))
            return ServiceResult.BadRequest("El estado solicitado no es válido para Matrices de Riesgos.");

        if (string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo del cambio de estado es obligatorio.");

        try
        {
            var ok = await _repo.CambiarEstadoAsync(matrizId, estado, dto.Motivo, usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Estado de la matriz actualizado correctamente.")
                : ServiceResult.NotFound("No se encontró la matriz de riesgos.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> EliminarMatrizAsync(long matrizId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (matrizId <= 0)
            return ServiceResult.BadRequest("El identificador de la matriz es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de eliminación de la matriz es obligatorio.");

        try
        {
            var ok = await _repo.EliminarMatrizAsync(matrizId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Matriz eliminada correctamente.")
                : ServiceResult.NotFound("No se encontró la matriz de riesgos.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<List<MatrizRiesgoHistorialDto>>> ObtenerHistorialAsync(long matrizId)
    {
        if (matrizId <= 0)
            return ServiceResult<List<MatrizRiesgoHistorialDto>>.BadRequest("El identificador de la matriz es obligatorio.");

        var historial = await _repo.ObtenerHistorialAsync(matrizId);
        return ServiceResult<List<MatrizRiesgoHistorialDto>>.Ok(historial);
    }

    public async Task<ServiceResult<List<MatrizRiesgoCriterioDto>>> ListarCriteriosAsync(bool incluirInactivos)
    {
        var criterios = await _repo.ListarCriteriosAsync(incluirInactivos);
        return ServiceResult<List<MatrizRiesgoCriterioDto>>.Ok(criterios);
    }

    public async Task<ServiceResult<MatrizRiesgoCriterioDto>> CrearCriterioAsync(MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        var error = ValidarCriterio(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(error);

        try
        {
            var criterioId = await _repo.CrearCriterioAsync(dto, usuarioId, usuarioEmail, ip);
            var criterio = (await _repo.ListarCriteriosAsync(true)).FirstOrDefault(x => x.CriterioId == criterioId);
            return criterio == null
                ? ServiceResult<MatrizRiesgoCriterioDto>.NotFound("El criterio fue creado, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoCriterioDto>.Ok(criterio, "Criterio registrado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult<MatrizRiesgoCriterioDto>> ActualizarCriterioAsync(long criterioId, MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest("El identificador del criterio es obligatorio.");

        var error = ValidarCriterio(dto);
        if (error != null)
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(error);

        try
        {
            var ok = await _repo.ActualizarCriterioAsync(criterioId, dto, usuarioId, usuarioEmail, ip);
            if (!ok)
                return ServiceResult<MatrizRiesgoCriterioDto>.NotFound("No se encontró el criterio activo.");

            var criterio = (await _repo.ListarCriteriosAsync(true)).FirstOrDefault(x => x.CriterioId == criterioId);
            return criterio == null
                ? ServiceResult<MatrizRiesgoCriterioDto>.NotFound("El criterio fue actualizado, pero no pudo consultarse.")
                : ServiceResult<MatrizRiesgoCriterioDto>.Ok(criterio, "Criterio actualizado correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<MatrizRiesgoCriterioDto>.BadRequest(ex.Message);
        }
    }

    public async Task<ServiceResult> InactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult.BadRequest("El identificador del criterio es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de inactivación del criterio es obligatorio.");

        var ok = await _repo.InactivarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
        return ok
            ? ServiceResult.Ok("Criterio inactivado correctamente.")
            : ServiceResult.NotFound("No se encontró el criterio activo.");
    }

    public async Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult.BadRequest("El identificador del criterio es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de eliminación del criterio es obligatorio.");

        try
        {
            var ok = await _repo.EliminarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Criterio eliminado correctamente.")
                : ServiceResult.NotFound("No se encontró el criterio.");
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2292)
        {
            return ServiceResult.BadRequest("El criterio ya está relacionado con información histórica y no puede eliminarse físicamente. Puede inactivarlo para conservar la trazabilidad.");
        }
    }

    private static string? ValidarCreacion(MatrizRiesgoCrearRequestDto dto)
    {
        if (dto == null)
            return "La solicitud de creación de matriz es obligatoria.";

        dto.SujetoTipo = dto.SujetoTipo?.Trim().ToUpperInvariant() ?? string.Empty;
        dto.NombreSujeto = dto.NombreSujeto?.Trim() ?? string.Empty;
        dto.OrigenDatos = string.IsNullOrWhiteSpace(dto.OrigenDatos) ? "CAPTURA" : dto.OrigenDatos.Trim().ToUpperInvariant();

        if (!SujetosPermitidos.Contains(dto.SujetoTipo))
            return "El tipo de sujeto evaluado no es válido.";

        if (string.IsNullOrWhiteSpace(dto.NombreSujeto))
            return "El nombre del sujeto evaluado es obligatorio.";

        if (dto.Detalles == null || dto.Detalles.Count == 0)
            return "Debe registrar al menos un detalle de variable para evaluar la matriz.";

        dto.Controles ??= new List<MatrizRiesgoControlRequestDto>();

        foreach (var detalle in dto.Detalles)
        {
            if (detalle.VariableId <= 0)
                return "Cada detalle debe indicar una variable válida.";

            if (detalle.Puntaje < 0)
                return "El puntaje de una variable no puede ser negativo.";
        }

        if (dto.Controles != null)
        {
            foreach (var control in dto.Controles)
            {
                if (string.IsNullOrWhiteSpace(control.Nombre))
                    return "Cada control debe tener nombre.";

                if (control.EfectividadPct < 0 || control.EfectividadPct > 100)
                    return "La efectividad del control debe estar entre 0% y 100%.";
            }
        }

        return null;
    }

    private static void NormalizarFiltroReporte(MatrizRiesgoReporteFiltroDto filtro)
    {
        if (filtro == null)
            return;

        filtro.Buscar = filtro.Buscar?.Trim();
        filtro.Estado = filtro.Estado?.Trim().ToUpperInvariant();
        filtro.SujetoTipo = filtro.SujetoTipo?.Trim().ToUpperInvariant();
        filtro.NivelResidual = filtro.NivelResidual?.Trim();
        filtro.ModeloVersion = filtro.ModeloVersion?.Trim();
        filtro.Responsable = filtro.Responsable?.Trim();
    }

    private static MatrizRiesgoExportacionDto ConstruirExcelReporte(MatricesRiesgoReporteDto reporte)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;color:#111827} h1{font-size:20px;margin:0;color:#16337a} h2{font-size:15px;margin:18px 0 8px;color:#111827} p{margin:4px 0;font-size:12px} table{border-collapse:collapse;margin:8px 0 18px;width:100%} th{background:#1f3f91;color:#fff;font-weight:bold;text-align:left} th,td{border:1px solid #cbd5e1;padding:7px;font-size:12px;vertical-align:top} .meta td:first-child,.totales td:first-child{font-weight:bold;background:#f8fafc}.num{text-align:right}.alto{color:#c2410c;font-weight:bold}.critico{color:#b91c1c;font-weight:bold}.ok{color:#047857;font-weight:bold}.muted{color:#64748b}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>SGRLA-IHSS - Reporte de Matrices de Riesgos</h1>");
        sb.AppendLine("<p class=\"muted\">Instituto Hondureño de Seguridad Social</p>");
        sb.AppendLine($"<p>Fecha de generación: {Html(reporte.FechaGeneracion.ToString("dd/MM/yyyy HH:mm"))}</p>");

        AgregarTablaHtml(sb, "Filtros aplicados", new[] { "Filtro", "Valor" }, ConstruirResumenFiltros(reporte.Filtro), "meta");
        AgregarTablaHtml(sb, "Resumen ejecutivo", new[] { "Indicador", "Valor" }, new[]
        {
            new[] { "Total matrices", reporte.Totales.TotalMatrices.ToString() },
            new[] { "Calculadas", reporte.Totales.TotalCalculadas.ToString() },
            new[] { "Cerradas", reporte.Totales.TotalCerradas.ToString() },
            new[] { "Alto / Crítico", reporte.Totales.TotalAltoCritico.ToString() },
            new[] { "Plan requerido", reporte.Totales.TotalPlanAccionRequerido.ToString() },
            new[] { "Planes vencidos", reporte.Totales.TotalPlanesVencidos.ToString() }
        }, "totales");

        AgregarTablaHtml(sb, "Distribución por estado", new[] { "Estado", "Total" }, reporte.PorEstado.Select(x => new[] { x.Nombre, x.Total.ToString() }));
        AgregarTablaHtml(sb, "Distribución por nivel residual", new[] { "Nivel", "Total" }, reporte.PorNivelResidual.Select(x => new[] { x.Nombre, x.Total.ToString() }));
        AgregarTablaHtml(sb, "Distribución por sujeto", new[] { "Sujeto", "Total" }, reporte.PorSujetoTipo.Select(x => new[] { x.Nombre, x.Total.ToString() }));
        AgregarTablaHtml(sb, "Mapa inherente persistido", new[] { "Nivel", "Total", "Promedio" },
            reporte.MapaInherente.Select(x => new[] { x.Nivel, x.Total.ToString(), x.Promedio.ToString("0.0000") }));
        AgregarTablaHtml(sb, "Mapa residual persistido", new[] { "Nivel", "Total", "Promedio" },
            reporte.MapaResidual.Select(x => new[] { x.Nivel, x.Total.ToString(), x.Promedio.ToString("0.0000") }));
        AgregarTablaHtml(sb, "Matrices filtradas", new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Inherente", "Residual", "Plan", "Fecha" },
            reporte.MatricesFiltradas.Select(x => new[]
            {
                x.MatrizId.ToString(),
                x.NombreSujeto ?? string.Empty,
                x.Documento ?? string.Empty,
                x.SujetoTipo ?? string.Empty,
                x.Estado ?? string.Empty,
                $"{x.PuntajeInherente:0.00} {x.NivelInherente}".Trim(),
                $"{x.PuntajeResidual:0.00} {x.NivelResidual}".Trim(),
                x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString("dd/MM/yyyy")
            }));
        AgregarTablaHtml(sb, "Resultados por factor", new[] { "Factor", "Matrices", "Promedio inherente", "Promedio residual", "Alto/Crítico", "Plan requerido" },
            reporte.PorFactor.Select(x => new[]
            {
                $"{x.FactorCodigo} - {x.FactorNombre}",
                x.TotalMatrices.ToString(),
                x.PromedioInherente.ToString("0.0000"),
                x.PromedioResidual.ToString("0.0000"),
                x.TotalAltoCritico.ToString(),
                x.TotalPlanAccionRequerido.ToString()
            }));
        AgregarTablaHtml(sb, "Matrices Alto / Crítico", new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Inherente", "Residual", "Plan", "Fecha" },
            reporte.MatricesCriticas.Select(x => new[]
            {
                x.MatrizId.ToString(),
                x.NombreSujeto ?? string.Empty,
                x.Documento ?? string.Empty,
                x.SujetoTipo ?? string.Empty,
                x.Estado ?? string.Empty,
                $"{x.PuntajeInherente:0.00} {x.NivelInherente}",
                $"{x.PuntajeResidual:0.00} {x.NivelResidual}",
                x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString("dd/MM/yyyy")
            }));
        AgregarTablaHtml(sb, "Planes de acción", new[] { "Estado", "Total", "Vencidos" },
            reporte.PlanesAccion.Select(x => new[] { x.Estado, x.Total.ToString(), x.Vencidos.ToString() }));

        sb.AppendLine("</body></html>");
        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.xls",
            ContentType = "application/vnd.ms-excel",
            Contenido = Encoding.UTF8.GetBytes(sb.ToString())
        };
    }

    private static MatrizRiesgoExportacionDto ConstruirPdfReporte(MatricesRiesgoReporteDto reporte)
    {
        var lineas = new List<string>
        {
            "INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL",
            "REPORTE DE MATRICES DE RIESGOS",
            $"SGRLA-IHSS | Fecha de Generación: {reporte.FechaGeneracion:dd/MM/yyyy HH:mm}",
            "",
            "1. FILTROS APLICADOS",
        };

        lineas.AddRange(ConstruirResumenFiltros(reporte.Filtro).Select(x => $" - {x[0]}: {x[1]}"));
        lineas.AddRange(new[]
        {
            "",
            "2. RESUMEN EJECUTIVO",
            $" - Total matrices: {reporte.Totales.TotalMatrices}",
            $" - Calculadas: {reporte.Totales.TotalCalculadas}",
            $" - Cerradas: {reporte.Totales.TotalCerradas}",
            $" - Alto / Crítico: {reporte.Totales.TotalAltoCritico}",
            $" - Plan requerido: {reporte.Totales.TotalPlanAccionRequerido}",
            $" - Planes vencidos: {reporte.Totales.TotalPlanesVencidos}",
            "",
            "3. DISTRIBUCIÓN POR ESTADO"
        });

        lineas.AddRange(reporte.PorEstado.Select(x => $" - {QuitarSaltos(x.Nombre)}: {x.Total}"));
        lineas.Add("");
        lineas.Add("4. DISTRIBUCIÓN POR NIVEL RESIDUAL");
        lineas.AddRange(reporte.PorNivelResidual.Select(x => $" - {QuitarSaltos(x.Nombre)}: {x.Total}"));
        lineas.Add("");
        lineas.Add("5. MAPA INHERENTE PERSISTIDO");
        lineas.AddRange(reporte.MapaInherente.Select(x => $" - {QuitarSaltos(x.Nivel)} | Total {x.Total} | Promedio {x.Promedio:0.0000}"));
        lineas.Add("");
        lineas.Add("6. MAPA RESIDUAL PERSISTIDO");
        lineas.AddRange(reporte.MapaResidual.Select(x => $" - {QuitarSaltos(x.Nivel)} | Total {x.Total} | Promedio {x.Promedio:0.0000}"));
        lineas.Add("");
        lineas.Add("7. MATRICES FILTRADAS");
        lineas.AddRange(reporte.MatricesFiltradas.Select(x => $" - {x.MatrizId} | {QuitarSaltos(x.NombreSujeto)} | {QuitarSaltos(x.Estado)} | Residual {FormatoResultado(x.PuntajeResidual, x.NivelResidual)} | Plan {(x.RequierePlanAccion ? "Sí" : "No")} | Fecha {x.FechaEvaluacion:dd/MM/yyyy}"));
        lineas.Add("");
        lineas.Add("8. RESULTADOS POR FACTOR");
        lineas.AddRange(reporte.PorFactor.Select(x => $" - {QuitarSaltos(x.FactorCodigo)} {QuitarSaltos(x.FactorNombre)} | Matrices {x.TotalMatrices} | Residual {x.PromedioResidual:0.0000}"));
        lineas.Add("");
        lineas.Add("9. MATRICES ALTO / CRÍTICO");
        lineas.AddRange(reporte.MatricesCriticas.Select(x => $" - {x.MatrizId} | {QuitarSaltos(x.NombreSujeto)} | {QuitarSaltos(x.Estado)} | Residual {x.PuntajeResidual:0.00} {QuitarSaltos(x.NivelResidual)} | Plan {(x.RequierePlanAccion ? "Sí" : "No")}"));
        lineas.Add("");
        lineas.Add("10. PLANES DE ACCIÓN");
        lineas.AddRange(reporte.PlanesAccion.Select(x => $" - {QuitarSaltos(x.Estado)} | Total {x.Total} | Vencidos {x.Vencidos}"));

        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            ContentType = "application/pdf",
            Contenido = CrearPdfSimple(lineas)
        };
    }

    private static void AgregarTablaHtml(StringBuilder sb, string titulo, string[] encabezados, IEnumerable<string[]> filas, string? clase = null)
    {
        var filasMaterializadas = filas.ToList();
        sb.AppendLine($"<h2>{Html(titulo)}</h2>");
        sb.AppendLine($"<table{(string.IsNullOrWhiteSpace(clase) ? string.Empty : $" class=\"{clase}\"")}>");
        sb.AppendLine("<tr>");
        foreach (var encabezado in encabezados)
            sb.AppendLine($"<th>{Html(encabezado)}</th>");
        sb.AppendLine("</tr>");
        if (filasMaterializadas.Count == 0)
        {
            sb.AppendLine($"<tr><td colspan=\"{encabezados.Length}\" class=\"muted\">Sin registros para mostrar.</td></tr>");
        }

        foreach (var fila in filasMaterializadas)
        {
            sb.AppendLine("<tr>");
            foreach (var celda in fila)
                sb.AppendLine($"<td>{Html(celda)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");
    }

    private static IEnumerable<string[]> ConstruirResumenFiltros(MatrizRiesgoReporteFiltroDto filtro)
    {
        var filtros = new[]
        {
            new[] { "Búsqueda general", ValorFiltro(filtro.Buscar) },
            new[] { "Estado", ValorFiltro(filtro.Estado) },
            new[] { "Tipo de sujeto", ValorFiltro(filtro.SujetoTipo) },
            new[] { "Nivel residual", ValorFiltro(filtro.NivelResidual) },
            new[] { "Versión metodología", ValorFiltro(filtro.ModeloVersion) },
            new[] { "Responsable", ValorFiltro(filtro.Responsable) },
            new[] { "Fecha inicio", filtro.FechaInicio?.ToString("dd/MM/yyyy") ?? "Todos" },
            new[] { "Fecha fin", filtro.FechaFin?.ToString("dd/MM/yyyy") ?? "Todos" }
        };
        return filtros;
    }

    private static string ValorFiltro(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Todos" : valor.Trim();

    private static string Html(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    private static string FormatoResultado(decimal? puntaje, string? nivel)
    {
        return puntaje.HasValue
            ? $"{puntaje.Value:0.00} {QuitarSaltos(nivel)}".Trim()
            : "-";
    }

    private static byte[] CrearPdfSimple(IEnumerable<string> lineas)
    {
        var paginas = lineas
            .SelectMany(linea => DividirLinea(QuitarSaltos(linea), 95))
            .Chunk(48)
            .ToList();

        if (paginas.Count == 0)
            paginas.Add(Array.Empty<string>());

        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [] /Count 0 >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"
        };

        var pageObjectIds = new List<int>();
        const int fontObjectId = 3;
        foreach (var pagina in paginas)
        {
            var pageId = objetos.Count + 1;
            var contentId = objetos.Count + 2;
            pageObjectIds.Add(pageId);
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentId} 0 R >>");
            var contenido = ConstruirContenidoPdf(pagina);
            objetos.Add($"<< /Length {Encoding.Latin1.GetByteCount(contenido)} >>\nstream\n{contenido}\nendstream");
        }

        objetos[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

        using var ms = new MemoryStream();
        var offsets = new List<long> { 0 };
        EscribirPdf(ms, "%PDF-1.4\n");
        for (var i = 0; i < objetos.Count; i++)
        {
            offsets.Add(ms.Position);
            EscribirPdf(ms, $"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        var xrefOffset = ms.Position;
        EscribirPdf(ms, $"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            EscribirPdf(ms, $"{offset:0000000000} 00000 n \n");
        EscribirPdf(ms, $"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return ms.ToArray();
    }

    private static string ConstruirContenidoPdf(IEnumerable<string> lineas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 9 Tf");
        sb.AppendLine("45 750 Td");
        sb.AppendLine("12 TL");
        foreach (var linea in lineas)
            sb.AppendLine($"({EscaparPdf(linea)}) Tj T*");
        sb.AppendLine("ET");
        return sb.ToString();
    }

    private static IEnumerable<string> DividirLinea(string linea, int maximo)
    {
        if (linea.Length <= maximo)
        {
            yield return linea;
            yield break;
        }

        for (var i = 0; i < linea.Length; i += maximo)
            yield return linea.Substring(i, Math.Min(maximo, linea.Length - i));
    }

    private static string EscaparPdf(string texto)
    {
        return texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string QuitarSaltos(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : texto.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static void EscribirPdf(Stream stream, string texto)
    {
        var bytes = Encoding.Latin1.GetBytes(texto);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string? ValidarCriterio(MatrizRiesgoCriterioRequestDto dto)
    {
        if (dto == null)
            return "La solicitud del criterio es obligatoria.";

        if (dto.VariableId <= 0)
            return "La variable asociada al criterio es obligatoria.";

        if (dto.EscalaId.HasValue && dto.EscalaId.Value <= 0)
            return "La escala asociada al criterio no es válida.";

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return "La descripción del criterio es obligatoria.";

        if (dto.Descripcion.Trim().Length > 1500)
            return "La descripción del criterio no puede superar 1500 caracteres.";

        if (dto.Puntaje < 0)
            return "El puntaje del criterio no puede ser negativo.";

        if (dto.ValorDesde.HasValue && dto.ValorHasta.HasValue && dto.ValorDesde.Value > dto.ValorHasta.Value)
            return "El valor inicial del rango no puede ser mayor que el valor final.";

        dto.Descripcion = dto.Descripcion.Trim();
        return null;
    }
}
