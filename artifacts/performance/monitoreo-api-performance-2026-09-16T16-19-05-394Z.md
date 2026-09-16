# Certificación HTTP de Monitoreo de Listas

Modo: **FULL_STACK_PRODUCTION_MODE_REAL_ORACLE**; Oracle real: **true**; SHA: `3f6977bfd3bdcf26447f29ee23b0f7854b155244`.

Medición: cuerpo HTTP completo consumido con response.text(); 10 ejecuciones warm y 2 warmups por escenario; timeout 10000 ms.

| Tipo | Cold ms | Median ms | P90 ms | P95 ms | Max ms | Total | Resultado |
|---|---:|---:|---:|---:|---:|---:|---|
| juridicas | 9687.14 | 2479.01 | 7421.1 | 8204.88 | 8204.88 | 4 | FAIL |
| naturales | 719.22 | 480.27 | 2988.61 | 4327.69 | 4327.69 | 242 | PASS |
| empleados | 1048.18 | 179.06 | 510.27 | 530.03 | 530.03 | 7 | PASS |

Resultado global: **FAIL**. Failed requests: **0**.

La clasificación de volumen es descriptiva del dataset conectado y no implica certificación de 100k registros.
