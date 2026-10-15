# Estándar de tema dinámico y modales institucionales

## Fuente de identidad

Los colores institucionales de todos los modales se obtienen exclusivamente del tema dinámico del sistema. La configuración persistida `ConfigSistema.colorPrimario` alimenta la identidad primaria y `ConfigSistema.colorSecundario` alimenta el acento. `ConfiguracionService` aplica el mismo tema al cargar y guardar la configuración; no existe un color de modal independiente.

Los valores de respaldo son `#1e3a8a` y `#1d4ed8`. Un HEX inválido conserva el último color válido aplicado; al iniciar, se usa el valor institucional de respaldo. No se realiza ninguna escritura de base de datos adicional para este estándar.

## Paleta dinámica

`system-theme.util.ts` genera de forma determinista los once niveles `ihss-50` a `ihss-950` mezclando el primario configurado con blanco o negro en proporciones fijas por nivel. Los tonos `on-*` seleccionan el foreground negro o blanco con mayor contraste. Tailwind consume tokens CSS, no hex azules fijos.

Las superficies del header y footer modal usan tonos calculados de la misma familia cromática del primario. El generador ajusta luminosidad en HSL y conserva el matiz; el contraste de texto se calcula con luminancia relativa. `colorSecundario` genera su propia escala y alimenta el acento, el highlight y el foco cuando la combinación es armónica. Si primario y secundario difieren mucho, el gradiente principal permanece en la familia primaria y el secundario no introduce un salto de matiz. Las pruebas incluyen azul, rojo, verde, claro, oscuro e inválido.

## Estándar modal existente

No crear un sistema paralelo. Las clases canónicas son:

- `modal-backdrop-overlay`
- `modal-container-card`
- `modal-header-institutional`
- `modal-body-scrollable`
- `modal-footer-institutional`
- `modal-size-sm`, `modal-size-md`, `modal-size-lg`, `modal-size-xl`, `modal-size-workspace`

Un modal nuevo debe usar esas clases y no definir colores de marca ni gradientes locales. El header aplica highlight radial, gradiente primario de tres tonos y geometría CSS discreta; el footer usa un gradiente más calmado de la misma familia. Header, footer, cierre, botón primario, foco y acentos heredan el tema global. El cuerpo, backdrop y sombras conservan sus superficies neutras; error, éxito y advertencia mantienen su semántica. **Toda acción interactiva del footer usa un botón icon-only** con `app-action-icon`, una variante `modal-action-icon--*`, `aria-label` y `title`/tooltip; no muestra texto de acción permanente. El texto `sr-only` es opcional y no reemplaza esos atributos. Esto incluye cerrar, cancelar, guardar, navegar, descargar, aprobar y rechazar. Los textos informativos y metadatos del footer no son acciones y permanecen legibles.

Las variantes globales son `primary`, `secondary`, `neutral`, `danger` y `success`. Los controles miden 44×44 px, conservan un icono de 20 px, superficie y borde identificables en estado disabled, hover y foco visible. Las superficies y foregrounds consumen el tema dinámico; los templates no deben añadir utilitarios locales de color ni estilos inline de marca.

Los templates con header estándar no requieren cambios para cambiar de tema. Los shells antiguos que usaban un header distinto deben migrar una vez a la clase canónica; el resto de su estructura, bindings, eventos, tamaños y scroll se conserva. La Matriz completa no tiene override de color propio.

## Identidad azul semántica

Los tonos azules usados como identidad, foco, acción o selección migran a `ihss-*` o tokens dinámicos. Los tonos azules de mensajes informativos permanecen como semántica informativa. Las opciones de confirmación SweetAlert consumen `--ihss-primary-color`; los defaults de Configuración se importan de `system-theme.util.ts`. El validador revisa templates externos e inline de TypeScript, estilos y colores de confirmación, y falla ante utilitarios de marca `blue-*` o HEX de marca fijo fuera de los defaults centrales.

## Validación obligatoria

Quality Gates ejecuta:

```powershell
node tools/validate_institutional_modal_standard.js
node tools/validate_dynamic_system_theme.js
node tools/validate_premium_modal_design.js
node --test tools/validate_modal_icon_contrast.test.js
node tools/validate_modal_icon_contrast.js
```

También se ejecutan desde `tools/run_quality_gates.ps1`. Los validadores comprueban shell único, herencia de tokens, gradientes multicapa, ausencia de overrides locales y acciones icon-only por cada botón del footer. Los fixtures prueban texto visible, falta de aria, falta de tooltip, botón solo de texto, override de marca, `sr-only` y región compuesta. Playwright mide superficie, icono, contraste, disabled, foco y cambio de tema; las regresiones de los modales comprueban que cerrar, guardar y navegar preservan sus handlers.

## Tono y contraste del estándar premium

Los tokens `--ihss-modal-primary-start-rgb`, `--ihss-modal-primary-mid-rgb` y `--ihss-modal-primary-end-rgb` se generan desde `colorPrimario`; `--ihss-modal-secondary-*` se genera desde `colorSecundario`. `--ihss-modal-highlight-rgb` sólo usa el secundario si su matiz queda dentro del umbral armónico configurado. Los valores iniciales de respaldo viven en `:root`; las reglas del shell consumen tokens y no copian hex ni RGB de marca.

El texto del header usa foreground dinámico con contraste normal mínimo de 4.5:1; el borde y el botón de cierre mantienen contraste de interfaz visible. Los estados de error, éxito y advertencia conservan color semántico. El cuerpo de contenido permanece blanco/neutro y los tamaños, scroll, capas y handlers no cambian al cambiar tema.

La auditoría actual contó 22 modales, todos con shell institucional; no quedan shells no estándar ni overrides locales de marca. Los valores azul, rojo, verde y claro pasaron los cálculos de contraste y las pruebas de actualización en vivo. Un modal ya conforme hereda el diseño sin cambios de template.

## Shells modales compuestos

Un shell puede estar dividido entre una plantilla de página y componentes hijos. Cada región delegada debe declararse con `data-modal-shell-region` y consumir las clases canónicas; una etiqueta `<header>` arbitraria nunca acredita un header institucional.

El Form Builder declara `data-modal-composite-shell="true"` en su host dentro del diálogo. Su toolbar publica la región `header` con `modal-header-institutional`; las cuatro variantes del contenido declaran la región `body`; el statusbar publica la región `footer` con `modal-footer-institutional`. La fila de título/versión hereda el gradiente dinámico; la navegación del workspace permanece neutra. El botón de cierre usa los tokens globales sólo dentro de un header institucional marcado.

Las acciones del footer del Form Builder son icon-only. En modo editable se conservan los botones Cancelar y Guardar con sus mismos outputs; en solo lectura se muestra Cerrar y se omite Guardar, que no puede actuar. Cada botón tiene nombre accesible y tooltip; los textos de estado (solo lectura, versión y conteos) siguen visibles. El header conserva su cierre icon-only accesible.

### Contrato y validación por instancia

`tools/modal_composite_shell_contract.json` registra el selector compuesto y las plantillas que deben probar su header, body y footer. El validador comprueba cada instancia modal por separado y resuelve componentes compuestos únicamente si están marcados y registrados. No considera un `<header>` genérico ni la presencia de una clase en otra parte del archivo como evidencia.

`node --test tools/validate_institutional_modal_standard.test.js` contiene los casos de modal correcto junto a modal incorrecto, header anidado arbitrario, composite válido, composite sin registro, modal inline y regiones compuestas incompletas. Quality Gates ejecuta esas pruebas y los tres validadores modales.

La causa corregida fue que el wrapper exterior del Form Builder era estándar, pero su toolbar y statusbar anidados conservaban fondos blancos; el token de cierre blanco se aplicaba a la barra blanca. Ahora ambas superficies consumen el shell global, y los tokens de cierre se limitan a regiones institucionales certificadas.
