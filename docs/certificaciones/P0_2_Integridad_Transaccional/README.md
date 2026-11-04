# P0 n.º 2 — Integridad transaccional

Expediente documental de la [Issue #23](https://github.com/jmejia31/RIESGO_LAVADO/issues/23).

## Identidad

- `SOURCE_CODE_SHA`: `168ffa783942d11097a2ebaad5bba74f4454de9b`.
- `EVIDENCE_COMMIT_SHA`: `87a48cb0` contiene el expediente y la evidencia textual; el commit final de metadatos se publicará inmediatamente después.
- Rama: `desarrollo`.
- Alcance: integridad transaccional backend de actualización, publicación y eliminación.
- Estado: `READY_FOR_INDEPENDENT_REVIEW`.
- Quality Gates históricos: `41469328699`, `41472933613`, `41477196537`, `41480850420`, `41483797292`; reportados como success sobre el SHA certificado, no reejecutados.

## Índice

- [Dictamen independiente](DICTAMEN_INDEPENDIENTE.md)
- [Matriz Markdown](MATRIZ_CERTIFICACION.md)
- [Matriz JSON](MATRIZ_CERTIFICACION.json)
- [Índice de evidencia primaria](PRIMARY_EVIDENCE_INDEX.json)
- [Verificación de paquetes](SOURCE_PACKAGE_VERIFICATION.json)
- [Manifiesto](MANIFIESTO_EVIDENCIAS.json)
- [Evidencias SHA-256](EVIDENCIAS_SHA256.txt)
- [Observaciones](REGISTRO_OBSERVACIONES.md)
- [Validación reproducible](VALIDACION_REPRODUCIBLE.md)
- [Evidencia primaria](evidencia-primaria/README.md)

## Procedencia y seguridad

Los cinco paquetes funcionales y el expediente consolidado disponibles localmente fueron verificados contra los SHA autorizados. El paquete independiente TX-10 y la firma independiente permanecen `MISSING_LOCAL_SOURCE`; no se reconstruyeron. Los archivos publicados son transcripciones y capturas textuales sanitizadas; no contienen secretos, credenciales, tokens, cookies, cadenas de conexión, PII, dumps ni logs operativos.

`FUNCTIONAL_CODE_CHANGED=NO`. No se ejecutaron Oracle, API, frontend, E2E, nuevas campañas ni Quality Gates durante esta publicación documental. El SHA certificado se conserva únicamente como referencia histórica y no cambia por estos commits documentales.

## Alcance del dictamen

El dictamen independiente es documental, limitado al backend P0 n.º 2, con observaciones. No certifica frontend, GTIC, H08, seguridad operacional completa ni autoriza el Bloque 10. Se mantienen `TX12_FRONTEND=NOT_VERIFIED`, `H08_M03=PENDING_USER_RETEST`, `H08_MANUAL_ACCEPTANCE=PENDING_USER_EXECUTION` y `BLOCK10_AUTHORIZED=NO`.
