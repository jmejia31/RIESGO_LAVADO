# P0-2 - Integridad transaccional

Expediente documental de la [Issue #23](https://github.com/jmejia31/RIESGO_LAVADO/issues/23).

## Identidad

- `SOURCE_CODE_SHA`: `168ffa783942d11097a2ebaad5bba74f4454de9b`.
- `EVIDENCE_COMMIT_SHA`: `87a48cb0` contiene el expediente y la evidencia textual.
- `DOCUMENTATION_HEAD_SHA`: `53e3502c81f640464f57f9e958a9ca1dff17e743`.
- Rama: `desarrollo`.
- Alcance: integridad transaccional backend de actualizacion, publicacion y eliminacion.
- Estado: `READY_FOR_INDEPENDENT_REVIEW`.
- Quality Gates historicos: `41469328699`, `41472933613`, `41477196537`, `41480850420`, `41483797292`; reportados como success sobre el SHA certificado, no reejecutados.

## Enlaces remotos

- `GITHUB_EVIDENCE_INDEX_URL`: https://github.com/jmejia31/RIESGO_LAVADO/blob/87a48cb0/docs/certificaciones/INDEX.json
- `GITHUB_SIGNOFF_URL`: https://github.com/jmejia31/RIESGO_LAVADO/issues/23#issuecomment-6633540601
- `GITHUB_PRIMARY_EVIDENCE_URL`: https://github.com/jmejia31/RIESGO_LAVADO/tree/87a48cb0/docs/certificaciones/P0_2_Integridad_Transaccional/evidencia-primaria
- `GITHUB_REMOTE_VERIFIED`: `YES`.

## Indice

- [Dictamen independiente](DICTAMEN_INDEPENDIENTE.md)
- [Matriz Markdown](MATRIZ_CERTIFICACION.md)
- [Matriz JSON](MATRIZ_CERTIFICACION.json)
- [Indice de evidencia primaria](PRIMARY_EVIDENCE_INDEX.json)
- [Verificacion de paquetes](SOURCE_PACKAGE_VERIFICATION.json)
- [Manifiesto](MANIFIESTO_EVIDENCIAS.json)
- [Evidencias SHA-256](EVIDENCIAS_SHA256.txt)
- [Observaciones](REGISTRO_OBSERVACIONES.md)
- [Validacion reproducible](VALIDACION_REPRODUCIBLE.md)
- [Evidencia primaria](evidencia-primaria/README.md)

## Procedencia y seguridad

Los cinco paquetes funcionales y el expediente consolidado disponibles localmente fueron verificados contra los SHA autorizados. El paquete independiente TX-10 y la firma independiente permanecen `MISSING_LOCAL_SOURCE`; no se reconstruyeron. Los archivos publicados son transcripciones y capturas textuales sanitizadas; no contienen secretos, credenciales, tokens, cookies, cadenas de conexion, PII, dumps ni logs operativos.

`FUNCTIONAL_CODE_CHANGED=NO`. No se ejecutaron Oracle, API, frontend, E2E, nuevas campanas ni Quality Gates durante esta publicacion documental. El SHA certificado se conserva unicamente como referencia historica y no cambia por estos commits documentales.

## Alcance del dictamen

El dictamen independiente es documental, limitado al backend P0-2, con observaciones. No certifica frontend, GTIC, H08, seguridad operacional completa ni autoriza el Bloque 10. Se mantienen `TX12_FRONTEND=NOT_VERIFIED`, `H08_M03=PENDING_USER_RETEST`, `H08_MANUAL_ACCEPTANCE=PENDING_USER_EXECUTION` y `BLOCK10_AUTHORIZED=NO`.
