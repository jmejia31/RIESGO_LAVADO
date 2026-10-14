# Estándar de tema dinámico y modales institucionales

## Fuente de identidad

Los colores institucionales de todos los modales se obtienen exclusivamente del tema dinámico del sistema. La configuración persistida `ConfigSistema.colorPrimario` alimenta la identidad primaria y `ConfigSistema.colorSecundario` alimenta el acento. `ConfiguracionService` aplica el mismo tema al cargar y guardar la configuración; no existe un color de modal independiente.

Los valores de respaldo son `#1e3a8a` y `#1d4ed8`. Un HEX inválido conserva el último color válido aplicado; al iniciar, se usa el valor institucional de respaldo. No se realiza ninguna escritura de base de datos adicional para este estándar.

## Paleta dinámica

`system-theme.util.ts` genera de forma determinista los once niveles `ihss-50` a `ihss-950` mezclando el primario configurado con blanco o negro en proporciones fijas por nivel. Los tonos `on-*` seleccionan el foreground negro o blanco con mayor contraste. Tailwind consume tokens CSS, no hex azules fijos.

Las superficies de header y footer modal conservan el matiz institucional y se oscurecen sólo cuando hace falta para mantener texto claro con contraste WCAG AA de al menos 4.5:1. `colorSecundario` participa en el segundo extremo del gradiente y en los tokens de foco/acento. Las pruebas incluyen azul, rojo, verde, claro, oscuro e inválido.

## Estándar modal existente

No crear un sistema paralelo. Las clases canónicas son:

- `modal-backdrop-overlay`
- `modal-container-card`
- `modal-header-institutional`
- `modal-body-scrollable`
- `modal-footer-institutional`
- `modal-size-sm`, `modal-size-md`, `modal-size-lg`, `modal-size-xl`, `modal-size-workspace`

Un modal nuevo debe usar esas clases y no definir colores de marca. Header, footer, cierre, botón primario, foco y acentos heredan el tema global. El cuerpo, backdrop y sombras conservan sus superficies neutras; error, éxito y advertencia mantienen su semántica. Los controles existentes y la jerarquía de acciones de los footers no se sustituyen.

Los templates con header estándar no requieren cambios para cambiar de tema. Los shells antiguos que usaban un header distinto deben migrar una vez a la clase canónica; el resto de su estructura, bindings, eventos, tamaños y scroll se conserva. La Matriz completa no tiene override de color propio.

## Identidad azul semántica

Los tonos azules usados como identidad, foco, acción o selección migran a `ihss-*` o tokens dinámicos. Los tonos azules de mensajes informativos permanecen como semántica informativa. Las opciones de confirmación SweetAlert consumen `--ihss-primary-color`; los defaults de Configuración se importan de `system-theme.util.ts`. El validador revisa templates externos e inline de TypeScript, estilos y colores de confirmación, y falla ante utilitarios de marca `blue-*` o HEX de marca fijo fuera de los defaults centrales.

## Validación obligatoria

Quality Gates ejecuta:

```powershell
node tools/validate_institutional_modal_standard.js
node tools/validate_dynamic_system_theme.js
```

También se ejecutan desde `tools/run_quality_gates.ps1`. Los tests del servicio verifican carga, guardado, recarga y fallback; los E2E verifican cambio de tema en un modal abierto, Matriz completa, contraste, geometría, móvil y desktop.
