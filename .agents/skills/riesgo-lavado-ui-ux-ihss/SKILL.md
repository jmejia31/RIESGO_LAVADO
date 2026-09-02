---
name: riesgo-lavado-ui-ux-ihss
description: Gobierna diseño UX/UI institucional de RIESGO_LAVADO. Usar para pantallas, modales, formularios, tablas, filtros, dashboards, responsive, accesibilidad y cualquier cambio visual que deba respetar prototipos aprobados del IHSS.
---

# UX/UI institucional IHSS

## Principio rector

Un diseño o prototipo aprobado por Javier Mejía es contrato visual. No reinterpretarlo, simplificarlo ni reemplazarlo sin autorización explícita.

## Antes de editar

1. Localizar el prototipo, captura o documentación aprobada de la interfaz.
2. Compararlo con la implementación actual.
3. Identificar exactamente qué difiere: jerarquía, contenido, componentes, espaciado, tamaño, estados, acciones o comportamiento.
4. Activar `riesgo-lavado-frontend-angular` para la implementación.

## Criterios de calidad

- Jerarquía visual clara y sin información redundante.
- Acciones primarias/secundarias inequívocas.
- Formularios agrupados por contexto; evitar repetición de títulos y etiquetas.
- Tablas y filtros con densidad adecuada y sin duplicación.
- Estados vacío/carga/error/sin permiso diseñados.
- Responsive sin overflow accidental.
- Navegación por teclado, foco visible y labels accesibles.
- Modales con ciclo de foco correcto y cierre predecible.
- No usar elementos decorativos que oculten información o resten legibilidad.

## Regla anti-regresión visual

Si una zona está explícitamente congelada o aprobada, no tocarla para resolver un problema de otra zona. Verificar visualmente la interfaz afectada y las superficies vecinas antes de cerrar.
