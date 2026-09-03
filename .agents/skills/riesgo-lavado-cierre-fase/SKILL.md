---
name: riesgo-lavado-cierre-fase
description: Cierra formalmente fases, subfases e interfaces de RIESGO_LAVADO. Usar cuando se solicite terminar, certificar, aprobar técnicamente o declarar cerrada una fase/UI.
---

# Cierre de fase/UI

## Condición de cierre

Una fase solo puede declararse técnicamente cerrada cuando su alcance está implementado, verificado y publicado en `desarrollo`, sin gate requerido pendiente salvo dependencia externa expresamente aceptada por Javier Mejía.

## Checklist

1. Confirmar criterios funcionales y visuales del alcance.
2. Revisar diff contra el punto inicial.
3. Confirmar que no quedaron referencias heredadas, TODOs accidentales, código muerto introducido o archivos temporales.
4. Ejecutar `riesgo-lavado-quality-gates`.
5. Verificar SHA final publicado en `origin/desarrollo`.
6. Actualizar bitácora y estado colaborativo.
7. Registrar qué quedó cerrado y qué sigue.

## Estados permitidos

- `CERRADA`: implementación y gates requeridos completos.
- `NO CERRADA`: existe defecto, prueba/gate rojo o falta evidencia requerida.
- `BLOQUEADA EXTERNAMENTE`: implementación lista, pero existe dependencia fuera del repositorio claramente demostrada.

No convertir `BLOQUEADA EXTERNAMENTE` en `CERRADA` sin autorización expresa cuando el gate sea obligatorio.

## Salida de certificación

Informar de forma compacta:

- fase/UI;
- estado;
- SHA certificado;
- archivos principales;
- tests/build/gates;
- bloqueos externos si existen;
- siguiente punto de trabajo.
