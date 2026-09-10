# Certificación HTTP de Monitoreo de Listas

Modo: **DOTNET_PRODUCTION**; Oracle real: **true**; SHA: `97f3577b58dd4788bff2dfacac026514593d81ad`.

Medición: cuerpo HTTP completo consumido con response.text(); 10 ejecuciones warm y 2 warmups por escenario; timeout 10000 ms.

| Tipo | Cold ms | Median ms | P90 ms | P95 ms | Max ms | Total | Resultado |
|---|---:|---:|---:|---:|---:|---:|---|
| juridicas | FAIL | 1718.75 | 9403.31 | 9403.31 | 9403.31 | 4 | FAIL |
| naturales | 3976.84 | 3998.45 | 6935.52 | 7561.85 | 7561.85 | 242 | FAIL |
| empleados | FAIL | 478.66 | 4482.17 | 4482.17 | 4482.17 | 7 | FAIL |

Resultado global: **FAIL**. Failed requests: **22**.

La clasificación de volumen es descriptiva del dataset conectado y no implica certificación de 100k registros.
