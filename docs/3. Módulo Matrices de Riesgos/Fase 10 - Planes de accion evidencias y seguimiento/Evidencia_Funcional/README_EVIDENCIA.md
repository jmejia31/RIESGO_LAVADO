# Índice de evidencia de Fase 10

Estado: cierre técnico aprobado el 16 de julio de 2026.

## Evidencia disponible

| Archivo | Alcance | Naturaleza |
|---|---|---|
| `fase10_validacion_api_final.json` | Flujo API de planes, evidencias, cierre condicionado e historial | Prueba funcional local |
| `fase10_validacion_auditoria.json` | Registros observados mediante el endpoint de auditoría | Evidencia funcional de trazabilidad |
| `../Evidencia_DBA/fase10_validacion_dba_readonly.sql` | Objetos, constraints, índices, integridad y auditoría agregada | Validación DBA reproducible de solo lectura |
| `../MATRIZ_TRAZABILIDAD_FASE10.md` | Relación requisito-código-prueba-evidencia | Control documental |
| `../POLITICA_RETENCION_EVIDENCIAS.md` | Custodia, acceso, respaldo, retención y disposición | Lineamiento operativo |
| `../MANIFIESTO_SHA256.txt` | Huellas de integridad de los artefactos de cierre | Control de integridad |

## Alcance y límites

- La base funcional fue validada sobre el commit `072a470e94fd14da3d3c9c8a0a9cd80052cbaeda`.
- Los JSON conservan los resultados observados en el ambiente local de prueba; no se reinterpretan como una certificación externa.
- El SQL DBA es deliberadamente de solo lectura y debe ejecutarse en el esquema institucional que se quiera certificar. Su salida debe custodiarse con fecha, ambiente y responsable.
- Las pruebas automatizadas no contienen contraseñas, tokens reales ni datos semilla destructivos.
- Los quality gates del repositorio verifican Backend, Frontend, cobertura y un E2E autenticado de Matrices sin escribir en Oracle.

## Reproducción

Desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run_quality_gates.ps1
```

En Oracle/SQL*Plus, con credenciales administradas fuera de Git:

```sql
@"docs/3. Módulo Matrices de Riesgos/Fase 10 - Planes de accion evidencias y seguimiento/Evidencia_DBA/fase10_validacion_dba_readonly.sql"
```

El resultado DBA es conforme cuando todos los objetos requeridos están `VALID`/`ENABLED`, los cuatro controles de integridad reportan cero hallazgos y existen eventos agregados coherentes con las operaciones funcionales ejecutadas.
