# Certificación HTTP de Monitoreo de Listas

Modo: **FULL_STACK_PRODUCTION_MODE_REAL_ORACLE**; Oracle real: **true**; SHA: `3a8782bf9482349062a65b639d7978791f8f715b`.

Medición: cuerpo HTTP completo consumido con response.text(); 10 ejecuciones warm y 2 warmups por escenario; timeout 10000 ms.

| Tipo | Cold ms | Median ms | P90 ms | P95 ms | Max ms | Total | Resultado |
|---|---:|---:|---:|---:|---:|---:|---|
| juridicas | FAIL | 4943.57 | 8582.68 | FAIL | 8582.68 | 4 | FAIL |
| naturales | FAIL | 4131.33 | 8170.45 | FAIL | 8170.45 | 242 | FAIL |
| empleados | FAIL | 188.15 | 471.58 | FAIL | 732.15 | 7 | FAIL |

Resultado global: **FAIL**. Failed requests: **40**.

La clasificación de volumen es descriptiva del dataset conectado y no implica certificación de 100k registros.
