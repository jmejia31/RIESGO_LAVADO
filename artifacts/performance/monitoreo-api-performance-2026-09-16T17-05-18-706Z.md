# Certificación HTTP de Monitoreo de Listas

Modo: **FULL_STACK_PRODUCTION_MODE_REAL_ORACLE**; Oracle real: **true**; SHA: `927dd0bd25a4981442992c329d61cd6c18b3542b`.

Medición: cuerpo HTTP completo consumido con response.text(); 10 ejecuciones warm y 2 warmups por escenario; timeout 10000 ms.

| Tipo | Cold ms | Median ms | P90 ms | P95 ms | Max ms | Total | Resultado |
|---|---:|---:|---:|---:|---:|---:|---|
| juridicas | FAIL | 3790.77 | 9698.41 | FAIL | 9698.41 | 4 | FAIL |
| naturales | 4855.17 | 5224.63 | 5588.27 | FAIL | 5588.27 | 242 | FAIL |
| empleados | 1997.27 | 185.46 | 490.65 | 534.36 | 534.36 | 7 | PASS |

Resultado global: **FAIL**. Failed requests: **29**.

La clasificación de volumen es descriptiva del dataset conectado y no implica certificación de 100k registros.
