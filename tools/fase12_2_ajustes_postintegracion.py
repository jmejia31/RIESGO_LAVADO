from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8-sig')


def write(path: str, content: str) -> None:
    (ROOT / path).write_text(content, encoding='utf-8')


def replace_once(content: str, old: str, new: str, label: str) -> str:
    if old in content:
        return content.replace(old, new, 1)
    if new in content:
        return content
    raise RuntimeError(f'No se encontró el bloque requerido: {label}')


repository_path = 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
app_service_path = 'backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosAppService.cs'

repository = read(repository_path)
repository = replace_once(
    repository,
    '''        cmd.Parameters.Add(Param("modeloId", modeloId));
        cmd.Parameters.Add(Param("variableId", dto.VariableId));
        cmd.Parameters.Add(Param("escalaId", dto.EscalaId));
        cmd.Parameters.Add(Param("valorDesde", dto.ValorDesde));''',
    '''        cmd.Parameters.Add(Param("modeloId", modeloId));
        cmd.Parameters.Add(Param("variableId", dto.VariableId));
        cmd.Parameters.Add(Param("valorDesde", dto.ValorDesde));''',
    'parámetro Oracle no utilizado en solapamiento'
)
write(repository_path, repository)

app_service = read(app_service_path)
app_service = replace_once(
    app_service,
    '''        if (await _repo.CriterioTieneUsoHistoricoAsync(criterioId))
            return ServiceResult.BadRequest("El criterio está relacionado con evaluaciones históricas y no puede eliminarse físicamente. Desactívelo para conservar la trazabilidad.");

        try
        {
            var ok = await _repo.EliminarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);''',
    '''        try
        {
            if (await _repo.CriterioTieneUsoHistoricoAsync(criterioId))
                return ServiceResult.BadRequest("El criterio está relacionado con evaluaciones históricas y no puede eliminarse físicamente. Desactívelo para conservar la trazabilidad.");

            var ok = await _repo.EliminarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);''',
    'consulta histórica dentro del control de errores'
)
app_service = replace_once(
    app_service,
    '''        catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2292)
        {
            return ServiceResult.BadRequest("El criterio ya está relacionado con información histórica y no puede eliminarse físicamente. Puede desactivarlo para conservar la trazabilidad.");
        }
    }''',
    '''        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 2292)
        {
            return ServiceResult.BadRequest("El criterio ya está relacionado con información histórica y no puede eliminarse físicamente. Puede desactivarlo para conservar la trazabilidad.");
        }
    }''',
    'captura de protección histórica transaccional'
)
write(app_service_path, app_service)

print('Ajustes postintegración Fase 12.2 aplicados correctamente.')
